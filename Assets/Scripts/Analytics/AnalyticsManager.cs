using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
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

    readonly Queue<AnalyticsEvent> _pendingEvents = new();
    Coroutine _dispatchRoutine;
    string _sessionId;
    bool _isShuttingDown;

    bool ShouldSendEvents => !string.IsNullOrEmpty(endpointUrl) && (!Application.isEditor || sendInEditor);

    public override void Initialize()
    {
        _sessionId = Guid.NewGuid().ToString("N");
        _dispatchRoutine = StartCoroutine(DispatchLoop());
    }

    public override void Shutdown()
    {
        _isShuttingDown = true;
        if (_dispatchRoutine != null)
        {
            StopCoroutine(_dispatchRoutine);
            _dispatchRoutine = null;
        }

        _pendingEvents.Clear();
    }

    public void Log(string name, Dictionary<string, object> parameters = null)
    {
        var evt = new AnalyticsEvent(name, parameters, _sessionId);
        _pendingEvents.Enqueue(evt);

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

    IEnumerator DispatchLoop()
    {
        var wait = new WaitForSecondsRealtime(0.25f);

        while (!_isShuttingDown)
        {
            if (_pendingEvents.Count == 0 || !ShouldSendEvents)
            {
                yield return wait;
                continue;
            }

            var evt = _pendingEvents.Dequeue();
            yield return SendEvent(evt);
        }
    }

    IEnumerator SendEvent(AnalyticsEvent evt)
    {
        string payload = MiniJson.ToJson(evt.ToDictionary());
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);

        using UnityWebRequest request = new UnityWebRequest(endpointUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Analytics] Failed to send '{evt.Name}': {request.error}");
            _pendingEvents.Enqueue(evt); // retry later
            yield return new WaitForSecondsRealtime(retryDelaySeconds);
        }
    }
}

readonly struct AnalyticsEvent
{
    public string Name { get; }
    public Dictionary<string, object> Parameters { get; }
    public long TimestampUtcMs { get; }
    public string SessionId { get; }

    public AnalyticsEvent(string name, Dictionary<string, object> parameters, string sessionId)
    {
        Name = name;
        Parameters = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>();
        TimestampUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
