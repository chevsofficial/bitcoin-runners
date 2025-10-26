using UnityEngine;

/// <summary>
/// Translates DifficultyProfile's density curve into spawn parameters,
/// independent from player speed.
/// </summary>
public class ObstacleDirector : MonoBehaviour
{
    [Header("Refs")]
    public DifficultyProfile difficulty;
    public GameManager gm;         // assign GameManager.I in Awake if null
    public MonoBehaviour spawner;  // your existing spawner component (exposes setters below)

    [Header("Tuning")]
    [Tooltip("Spawn interval range, seconds. Low end used at max density.")]
    public Vector2 spawnIntervalRange = new Vector2(0.4f, 1.8f);
    [Tooltip("Max simultaneous obstacles range. High end used at max density.")]
    public Vector2 concurrentRange = new Vector2(2, 6);
    [Tooltip("Optional probability of leaving lanes empty to reduce unfair spikes.")]
    [Range(0f,1f)] public float minEmptyLaneChance = 0.10f;

    float _t;
    int _lastConcurrent = -1;
    float _lastInterval = -1f;

    void Awake()
    {
        if (!gm) gm = GameManager.I;
        if (!difficulty && gm) difficulty = gm.difficulty;
    }

    void Update()
    {
        if (gm == null || difficulty == null || spawner == null) return;
        _t += Time.deltaTime;

        // Density in [0..1]
        float d = difficulty.DensityAt(Time.time - GmStartTimeApprox());

        // Map density to knobs (invert for interval: higher density => shorter interval)
        float targetInterval = Mathf.Lerp(spawnIntervalRange.y, spawnIntervalRange.x, d);
        int targetConcurrent = Mathf.RoundToInt(Mathf.Lerp(concurrentRange.x, concurrentRange.y, d));

        // Only push changes when they actually change to avoid churn
        if (!Mathf.Approximately(targetInterval, _lastInterval))
        {
            TryCall(spawner, "SetSpawnInterval", targetInterval);
            _lastInterval = targetInterval;
        }
        if (targetConcurrent != _lastConcurrent)
        {
            TryCall(spawner, "SetMaxConcurrent", targetConcurrent);
            _lastConcurrent = targetConcurrent;
        }
        TryCall(spawner, "SetMinEmptyLaneChance", minEmptyLaneChance);
    }

    float GmStartTimeApprox()
    {
        // We don’t have explicit start time exposed; infer using Distance/Speed when alive.
        // If Speed is 0 or dead, just use Time.time for a monotonic input into the curve.
        if (gm.Alive && gm.Speed > 0.01f) return Time.time - (gm.Distance / gm.Speed);
        return 0f;
    }

    static void TryCall(MonoBehaviour target, string method, object arg)
    {
        var mi = target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mi != null) mi.Invoke(target, new[] { arg });
    }
}
