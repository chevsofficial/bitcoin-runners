using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class AnalyticsManager : SingletonServiceBehaviour<AnalyticsManager>
{
    public static AnalyticsManager I => ServiceLocator.TryGet(out AnalyticsManager service) ? service : null;

    [Header("Transport")]
    [Tooltip("HTTPS endpoint that receives analytics POST requests.")]
    [SerializeField] string endpointUrl = string.Empty;
    [Tooltip("Send events while running inside the Unity Editor. Disable to avoid polluting production pipelines during development.")]
    [SerializeField] bool sendInEditor = false;
    [Tooltip("Writes every analytics event to the Unity console when enabled.")]
    [SerializeField] bool echoToConsole = true;
    [Tooltip("Seconds to wait before retrying a failed HTTP send.")]
    [SerializeField] float retryDelaySeconds = 5f;
    [Tooltip("Maximum number of analytics events retained in memory. Oldest events are dropped when the cap is reached. 0 disables the cap.")]
    [SerializeField] int maxQueueSize = 1024;
    [Tooltip("File name used to persist unsent analytics events to Application.persistentDataPath.")]
    [SerializeField] string persistenceFileName = "analytics_queue.dat";

    [Header("Monitoring")]
    [SerializeField] AnalyticsTransportSnapshotEvent onMetricsUpdated;

    readonly Queue<AnalyticsEvent> _pendingEvents = new();
    readonly object _syncRoot = new();
    AutoResetEvent _queueSignal;
    CancellationTokenSource _dispatchCancellation;
    Task _dispatchTask;
    string _sessionId;
    bool _isShuttingDown;
    int _eventsSent;
    int _eventsFailed;
    int _eventsDropped;
    double _lastLatencyMs;

    bool ShouldSendEvents => !string.IsNullOrEmpty(endpointUrl) && (!Application.isEditor || sendInEditor);
    string PersistencePath => Path.Combine(Application.persistentDataPath, persistenceFileName);
    static readonly HttpClient HttpClient = new();

    public override void Initialize()
    {
        _sessionId = Guid.NewGuid().ToString("N");
        LoadPersistedQueue();
        _queueSignal = new AutoResetEvent(false);
        _dispatchCancellation = new CancellationTokenSource();
        _dispatchTask = Task.Run(() => DispatchLoop(_dispatchCancellation.Token));
        RaiseMetricsChanged();
    }

    public override void Shutdown()
    {
        _isShuttingDown = true;
        if (_dispatchCancellation != null && !_dispatchCancellation.IsCancellationRequested)
        {
            _dispatchCancellation.Cancel();
        }

        _queueSignal?.Set();

        try
        {
            _dispatchTask?.Wait(1000);
        }
        catch (AggregateException) { }

        lock (_syncRoot)
        {
            SaveQueueToDiskUnsafe();
            _pendingEvents.Clear();
        }

        _queueSignal?.Dispose();
        _queueSignal = null;
        _dispatchCancellation?.Dispose();
        _dispatchCancellation = null;
        _dispatchTask = null;
    }

    public void Log(string name, Dictionary<string, object> parameters = null)
    {
        var evt = new AnalyticsEvent(name, parameters, _sessionId);

        lock (_syncRoot)
        {
            if (maxQueueSize > 0 && _pendingEvents.Count >= maxQueueSize)
            {
                _pendingEvents.Dequeue();
                Interlocked.Increment(ref _eventsDropped);
            }

            _pendingEvents.Enqueue(evt);
            SaveQueueToDiskUnsafe();
        }

        _queueSignal?.Set();
        RaiseMetricsChanged();

        if (echoToConsole)
        {
            Debug.Log($"[ANALYTICS] {evt.Name} {MiniJson.ToJson(evt.ToDictionary())}");
        }
    }

    public void RunStart() => Log("run_start");

    public void RunEnd(int distance, int coins, int score, bool continued) =>
        Log("run_end", new() { { "distance", distance }, { "coins", coins }, { "score", score }, { "continued", continued } });

    public void AdImpression(string type, string placement) =>
        Log("ad_impression", new() { { "type", type }, { "placement", placement } });

    public void AdReward(string placement) =>
        Log("ad_reward", new() { { "placement", placement } });

    public void Purchase(string productId, bool success) =>
        Log("purchase", new() { { "productId", productId }, { "success", success } });

    public void PowerupStart(string type, float duration) =>
        Log("powerup_start", new() { { "type", type }, { "duration", duration } });

    void DispatchLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_isShuttingDown)
        {
            if (!ShouldSendEvents)
            {
                if (token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    break;
                }
                continue;
            }

            AnalyticsEvent? nextEvent = null;

            lock (_syncRoot)
            {
                if (_pendingEvents.Count > 0)
                {
                    nextEvent = _pendingEvents.Peek();
                }
            }

            if (!nextEvent.HasValue)
            {
                if (token.WaitHandle.WaitOne(TimeSpan.FromSeconds(0.5)))
                {
                    break;
                }

                _queueSignal?.WaitOne(TimeSpan.FromSeconds(0.5));
                continue;
            }

            bool success = TrySendEvent(nextEvent.Value, token, out double latencyMs);

            if (token.IsCancellationRequested)
            {
                break;
            }

            if (success)
            {
                lock (_syncRoot)
                {
                    if (_pendingEvents.Count > 0)
                    {
                        _pendingEvents.Dequeue();
                    }
                    Interlocked.Increment(ref _eventsSent);
                    Volatile.Write(ref _lastLatencyMs, latencyMs);
                    SaveQueueToDiskUnsafe();
                }

                RaiseMetricsChanged();
            }
            else
            {
                Interlocked.Increment(ref _eventsFailed);
                RaiseMetricsChanged();

                if (token.WaitHandle.WaitOne(TimeSpan.FromSeconds(retryDelaySeconds)))
                {
                    break;
                }
            }
        }
    }

    bool TrySendEvent(AnalyticsEvent evt, CancellationToken token, out double latencyMs)
    {
        latencyMs = 0;

        if (string.IsNullOrEmpty(endpointUrl))
        {
            return true;
        }

        string payload = MiniJson.ToJson(evt.ToDictionary());
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = HttpClient.PostAsync(endpointUrl, content, token).GetAwaiter().GetResult();
            stopwatch.Stop();
            latencyMs = stopwatch.Elapsed.TotalMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[Analytics] Failed to send '{evt.Name}': {(int)response.StatusCode} {response.ReasonPhrase}");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics] Failed to send '{evt.Name}': {ex.Message}");
            return false;
        }
    }

    void LoadPersistedQueue()
    {
        string path = PersistencePath;

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: false);

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string name = reader.ReadString();
                string sessionId = reader.ReadString();
                long timestamp = reader.ReadInt64();
                int paramCount = reader.ReadInt32();
                Dictionary<string, object> parameters = new();

                for (int p = 0; p < paramCount; p++)
                {
                    string key = reader.ReadString();
                    AnalyticsParameterType type = (AnalyticsParameterType)reader.ReadByte();
                    object value = type switch
                    {
                        AnalyticsParameterType.Null => null,
                        AnalyticsParameterType.Bool => reader.ReadBoolean(),
                        AnalyticsParameterType.Long => reader.ReadInt64(),
                        AnalyticsParameterType.Double => reader.ReadDouble(),
                        AnalyticsParameterType.String => reader.ReadString(),
                        _ => reader.ReadString()
                    };

                    if (value is long longValue && longValue <= int.MaxValue && longValue >= int.MinValue)
                    {
                        value = (int)longValue;
                    }

                    parameters[key] = value;
                }

                var evt = new AnalyticsEvent(name, parameters, sessionId, timestamp);
                _pendingEvents.Enqueue(evt);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics] Failed to load persisted analytics queue: {ex.Message}");
            TryDeleteFile(path);
        }
    }

    void SaveQueueToDiskUnsafe()
    {
        string path = PersistencePath;

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (_pendingEvents.Count == 0)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            using FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: false);

            writer.Write(_pendingEvents.Count);
            foreach (AnalyticsEvent evt in _pendingEvents)
            {
                writer.Write(evt.Name ?? string.Empty);
                writer.Write(evt.SessionId ?? string.Empty);
                writer.Write(evt.TimestampUtcMs);
                WriteParameters(writer, evt.Parameters);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics] Failed to persist analytics queue: {ex.Message}");
        }
    }

    static void WriteParameters(BinaryWriter writer, Dictionary<string, object> parameters)
    {
        writer.Write(parameters?.Count ?? 0);

        if (parameters == null)
        {
            return;
        }

        foreach (var kvp in parameters)
        {
            writer.Write(kvp.Key ?? string.Empty);
            AnalyticsParameterType type = GetParameterType(kvp.Value, out object normalized);
            writer.Write((byte)type);

            switch (type)
            {
                case AnalyticsParameterType.Null:
                    break;
                case AnalyticsParameterType.Bool:
                    writer.Write((bool)normalized);
                    break;
                case AnalyticsParameterType.Long:
                    writer.Write((long)normalized);
                    break;
                case AnalyticsParameterType.Double:
                    writer.Write((double)normalized);
                    break;
                case AnalyticsParameterType.String:
                    writer.Write((string)normalized);
                    break;
            }
        }
    }

    static AnalyticsParameterType GetParameterType(object value, out object normalized)
    {
        switch (value)
        {
            case null:
                normalized = null;
                return AnalyticsParameterType.Null;
            case bool b:
                normalized = b;
                return AnalyticsParameterType.Bool;
            case int i:
                normalized = (long)i;
                return AnalyticsParameterType.Long;
            case long l:
                normalized = l;
                return AnalyticsParameterType.Long;
            case float f:
                normalized = (double)f;
                return AnalyticsParameterType.Double;
            case double d:
                normalized = d;
                return AnalyticsParameterType.Double;
            case string s:
                normalized = s;
                return AnalyticsParameterType.String;
            default:
                normalized = value.ToString();
                return AnalyticsParameterType.String;
        }
    }

    void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics] Failed to delete analytics persistence file '{path}': {ex.Message}");
        }
    }

    void RaiseMetricsChanged()
    {
        onMetricsUpdated?.Invoke(GetSnapshot());
    }

    public AnalyticsTransportSnapshot GetSnapshot()
    {
        int queued;

        lock (_syncRoot)
        {
            queued = _pendingEvents.Count;
        }

        int dropped = Volatile.Read(ref _eventsDropped);
        int sent = Volatile.Read(ref _eventsSent);
        int failed = Volatile.Read(ref _eventsFailed);
        double latency = Volatile.Read(ref _lastLatencyMs);

        return new AnalyticsTransportSnapshot(queued, dropped, sent, failed, latency);
    }
}

readonly struct AnalyticsEvent
{
    public string Name { get; }
    public Dictionary<string, object> Parameters { get; }
    public long TimestampUtcMs { get; }
    public string SessionId { get; }

    public AnalyticsEvent(string name, Dictionary<string, object> parameters, string sessionId, long? timestampUtcMs = null)
    {
        Name = name;
        Parameters = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>();
        TimestampUtcMs = timestampUtcMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SessionId = sessionId;
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new()
        {
            { "name", Name },
            { "timestampMs", TimestampUtcMs },
            { "sessionId", SessionId },
            { "parameters", Parameters }
        };
    }
}

[Serializable]
public struct AnalyticsTransportSnapshot
{
    public int queued;
    public int dropped;
    public int sent;
    public int failed;
    public double lastLatencyMs;

    public AnalyticsTransportSnapshot(int queued, int dropped, int sent, int failed, double lastLatencyMs)
    {
        this.queued = queued;
        this.dropped = dropped;
        this.sent = sent;
        this.failed = failed;
        this.lastLatencyMs = lastLatencyMs;
    }
}

[Serializable]
public class AnalyticsTransportSnapshotEvent : UnityEvent<AnalyticsTransportSnapshot> { }

enum AnalyticsParameterType : byte
{
    Null = 0,
    Bool = 1,
    Long = 2,
    Double = 3,
    String = 4
}

/// <summary>
/// Lightweight JSON serializer that supports nested dictionaries/lists and
/// performs proper string escaping so payloads are safe for transport.
/// </summary>
public static class MiniJson
{
    public static string ToJson(Dictionary<string, object> dict)
    {
        System.Text.StringBuilder sb = new();
        WriteValue(dict, sb);
        return sb.ToString();
    }

    static void WriteValue(object value, System.Text.StringBuilder sb)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case string s:
                WriteEscapedString(s, sb);
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case int or long or float or double or decimal or uint or ulong or short or ushort or byte or sbyte:
                sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case IFormattable formattable:
                sb.Append(formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case Dictionary<string, object> map:
                sb.Append('{');
                bool first = true;
                foreach (var kvp in map)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteEscapedString(kvp.Key, sb);
                    sb.Append(':');
                    WriteValue(kvp.Value, sb);
                }
                sb.Append('}');
                break;
            case System.Collections.IDictionary rawMap:
                sb.Append('{');
                bool firstEntry = true;
                foreach (System.Collections.DictionaryEntry entry in rawMap)
                {
                    if (!firstEntry) sb.Append(',');
                    firstEntry = false;
                    WriteEscapedString(entry.Key.ToString(), sb);
                    sb.Append(':');
                    WriteValue(entry.Value, sb);
                }
                sb.Append('}');
                break;
            case System.Collections.IEnumerable sequence:
                sb.Append('[');
                bool firstItem = true;
                foreach (var element in sequence)
                {
                    if (!firstItem) sb.Append(',');
                    firstItem = false;
                    WriteValue(element, sb);
                }
                sb.Append(']');
                break;
            default:
                WriteEscapedString(value.ToString(), sb);
                break;
        }
    }

    static void WriteEscapedString(string value, System.Text.StringBuilder sb)
    {
        sb.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
    }
}
