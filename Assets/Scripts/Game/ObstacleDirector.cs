using UnityEngine;

/// <summary>
/// Translates DifficultyProfile's density curve into spawn parameters,
/// independent from player speed.
/// </summary>
public class ObstacleDirector : SingletonServiceBehaviour<ObstacleDirector>
{
    [Header("Refs")]
    public DifficultyProfile difficulty;
    public GameManager gm;         // assign GameManager.I in Initialize if null
    [Tooltip("Component that implements IObstacleSpawner")] public MonoBehaviour spawner;  // assign TrackGenerator or similar

    [Header("Tuning")]
    [Tooltip("Spawn interval range, seconds. Low end used at max density.")]
    public Vector2 spawnIntervalRange = new Vector2(0.5f, 1.6f);
    [Tooltip("Max simultaneous obstacles range. High end used at max density.")]
    public Vector2 concurrentRange = new Vector2(2, 5);
    [Tooltip("Optional probability of leaving lanes empty to reduce unfair spikes.")]
    [Range(0f,1f)] public float minEmptyLaneChance = 0.12f;

    float _t;
    int _lastConcurrent = -1;
    float _lastInterval = -1f;
    IObstacleSpawner _spawner;

    public override void Initialize()
    {
        if (!gm) gm = GameManager.I;
        if (!difficulty && gm) difficulty = gm.difficulty;
        CacheSpawnerInterface();
    }

    public override void Shutdown()
    {
        _spawner = null;
    }

    void OnValidate()
    {
        CacheSpawnerInterface();
    }

    void Update()
    {
        if (gm == null || difficulty == null || _spawner == null) return;
        _t += Time.deltaTime;

        // Density in [0..1]
        float d = difficulty.DensityAt(Time.time - GmStartTimeApprox());

        // Map density to knobs (invert for interval: higher density => shorter interval)
        float targetInterval = Mathf.Lerp(spawnIntervalRange.y, spawnIntervalRange.x, d);
        int targetConcurrent = Mathf.RoundToInt(Mathf.Lerp(concurrentRange.x, concurrentRange.y, d));

        // Only push changes when they actually change to avoid churn
        if (!Mathf.Approximately(targetInterval, _lastInterval))
        {
            _spawner.SetSpawnInterval(targetInterval);
            _lastInterval = targetInterval;
        }
        if (targetConcurrent != _lastConcurrent)
        {
            _spawner.SetMaxConcurrent(targetConcurrent);
            _lastConcurrent = targetConcurrent;
        }
        _spawner.SetMinEmptyLaneChance(minEmptyLaneChance);
    }

    float GmStartTimeApprox()
    {
        // We don’t have explicit start time exposed; infer using Distance/Speed when alive.
        // If Speed is 0 or dead, just use Time.time for a monotonic input into the curve.
        if (gm.Alive && gm.Speed > 0.01f) return Time.time - (gm.Distance / gm.Speed);
        return 0f;
    }

    void CacheSpawnerInterface()
    {
        _spawner = null;
        if (!spawner) return;

        _spawner = spawner as IObstacleSpawner;
        if (_spawner == null)
        {
            Debug.LogError($"{name} spawner reference must implement {nameof(IObstacleSpawner)}", this);
        }
    }

    // Allows runtime scenes to register their spawner (e.g., via ObstacleSpawnerBinder)
    public void SetSpawner(IObstacleSpawner s)
    {
        _spawner = s;
    }
}
