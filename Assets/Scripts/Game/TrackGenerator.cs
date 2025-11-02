// Assets/Scripts/Game/TrackGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using BR.Config;

public class TrackGenerator : MonoBehaviour, IObstacleSpawner
{
    public GameConstants cfg;
    public Transform runner;

    // Obstacle/coin pools
    public SimplePool coinPool, barrierPool, lowBarPool, gapPool;

    // NEW: powerup pools
    public SimplePool puMagnetPool, puShieldPool, puDashPool;

    [Header("Density Control")]
    [SerializeField] float spawnInterval = 1.2f;
    [SerializeField] int maxConcurrent = 4;
    [SerializeField, Range(0f, 1f)] float minEmptyLaneChance = 0.12f;

    float spawnedZ;
    float _spawnTimer;
    readonly List<GameObject> live = new();
    readonly HashSet<PooledRef> activeObstacles = new();
    readonly Dictionary<SimplePool, ObstacleInfo> obstacleMetadata = new();

    readonly struct ObstacleInfo
    {
        public readonly ObstacleHandle.ObstacleType type;
        public readonly string tag;

        public ObstacleInfo(ObstacleHandle.ObstacleType type, string tag)
        {
            this.type = type;
            this.tag = tag;
        }
    }

    void Awake()
    {
        obstacleMetadata.Clear();
        RegisterObstaclePool(barrierPool, ObstacleHandle.ObstacleType.Barrier, "ObstacleBarrier");
        RegisterObstaclePool(lowBarPool, ObstacleHandle.ObstacleType.LowBar, "ObstacleLowBar");
        RegisterObstaclePool(gapPool, ObstacleHandle.ObstacleType.Gap, "ObstacleGap");
    }

    void Start()
    {
        float startDistance = GameManager.I ? GameManager.I.Distance : 0f;
        float runnerZ = runner ? runner.position.z : startDistance;
        spawnedZ = Mathf.Max(startDistance, runnerZ);

        // Pre-spawn a few groups so the track isn't empty at run start
        for (int i = 0; i < 4; i++)
        {
            SpawnSegment(true);
        }
        _spawnTimer = 0f;
    }

    void Update()
    {
        if (!GameManager.I.Alive) return;

        _spawnTimer += Time.deltaTime;
        float interval = Mathf.Max(0.05f, spawnInterval);

        while (_spawnTimer >= interval)
        {
            bool spawned = SpawnSegment();
            _spawnTimer -= interval;

            // If we couldn't spawn because we're at capacity, avoid tight loops
            if (!spawned && maxConcurrent > 0 && activeObstacles.Count >= maxConcurrent)
            {
                _spawnTimer = 0f;
                break;
            }
        }

        // recycle objects behind runner
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var go = live[i];
            if (!go || !go.activeSelf)
            {
                UnregisterObstacle(go);
                live.RemoveAt(i);
                continue;
            }

            if (runner && go.transform.position.z < runner.position.z - 12f)
            {
                live.RemoveAt(i);
                bool wasObstacle = IsObstacle(go);
                if (wasObstacle)
                {
                    UnregisterObstacle(go);
                    FXSpawner.BurstDebris(go.transform.position);
                }
                SimplePool.RecycleAny(go);
            }
        }
    }

    // --- IObstacleSpawner implementation ---
    public void SetSpawnInterval(float seconds)
    {
        // Clamp to a tiny minimum to avoid zero/negative intervals
        spawnInterval = Mathf.Max(0.01f, seconds);
    }

    public void SetMaxConcurrent(int n)
    {
        maxConcurrent = Mathf.Max(0, n);
    }

    public void SetMinEmptyLaneChance(float p)
    {
        minEmptyLaneChance = Mathf.Clamp01(p);
    }

    bool SpawnSegment(bool force = false)
    {
        if (maxConcurrent > 0 && activeObstacles.Count >= maxConcurrent)
        {
            if (!force) return false;
        }
        if (!cfg) return false;

        float speed = GameManager.I ? Mathf.Max(0.1f, GameManager.I.Speed) : cfg.startSpeed;
        float interval = Mathf.Max(0.05f, spawnInterval);
        float spacing = Mathf.Max(4f, speed * interval);
        float runnerZ = runner ? runner.position.z : 0f;
        float distance = GameManager.I ? GameManager.I.Distance : 0f;
        float minSpacing = Mathf.Lerp(cfg.minObstacleSpacingEarly, cfg.minObstacleSpacingLate, Mathf.Clamp01(distance / 300f));
        spacing = Mathf.Max(spacing, minSpacing);

        if (spawnedZ < runnerZ)
        {
            spawnedZ = runnerZ;
        }

        float lead = Mathf.Max(18f, speed * interval * 2f);
        float baseZ = Mathf.Max(spawnedZ + spacing, runnerZ + lead);
        spawnedZ = baseZ;

        int laneCount = LaneCoords.Count;
        if (laneCount == 0)
        {
            return false;
        }
        int safeLane = UnityEngine.Random.Range(0, laneCount);
        float safeLaneX = LaneCoords.Get(safeLane);

        bool allowObstacles = maxConcurrent != 0;
        if (allowObstacles)
        {
            // difficulty ramp: which obstacle type to use on blocked lanes?
            bool advanced = distance >= 200f;
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (lane == safeLane) continue;
                if (maxConcurrent > 0 && activeObstacles.Count >= maxConcurrent) break;
                if (UnityEngine.Random.value < minEmptyLaneChance) continue;

                float laneX = LaneCoords.Get(lane);
                float z = baseZ + 6f;

                if (!advanced)
                {
                    TrySpawnObstacle(barrierPool, laneX, z);
                }
                else
                {
                    int pick = UnityEngine.Random.Range(0, 3); // 0 barrier, 1 lowbar, 2 gap
                    SimplePool chosen = pick == 0 ? barrierPool : pick == 1 ? lowBarPool : gapPool;
                    if (!TrySpawnObstacle(chosen, laneX, z))
                    {
                        // fallback to a basic barrier if the chosen pool is empty or misconfigured
                        TrySpawnObstacle(barrierPool, laneX, z);
                    }
                }
            }
        }

        // coin line on safe lane
        for (int k = 0; k < 10; k++)
        {
            var c = coinPool.Get();
            c.transform.position = new Vector3(safeLaneX, 1f, baseZ + 2f + k * 1.6f);
            live.Add(c);
        }

        // --- NEW: occasional powerup on safe lane (12% chance), only after 100m ---
        if (distance > 100f && UnityEngine.Random.value < 0.12f)
        {
            GameObject pgo = null;
            int p = UnityEngine.Random.Range(0, 3); // 0=Magnet, 1=Shield, 2=Dash

            if (p == 0 && puMagnetPool != null) pgo = puMagnetPool.Get();
            else if (p == 1 && puShieldPool != null) pgo = puShieldPool.Get();
            else if (puDashPool != null) pgo = puDashPool.Get();

            if (pgo != null)
            {
                pgo.transform.position = new Vector3(safeLaneX, 1.0f, baseZ + 10f);
                live.Add(pgo);
            }
        }

        return true;
    }

    void RegisterObstaclePool(SimplePool pool, ObstacleHandle.ObstacleType type, string tag)
    {
        if (!pool) return;
        obstacleMetadata[pool] = new ObstacleInfo(type, tag);
    }

    bool TrySpawnObstacle(SimplePool pool, float laneX, float z)
    {
        if (!pool)
        {
            Debug.LogWarning("[TrackGenerator] Obstacle pool reference missing; skipping spawn.");
            return false;
        }

        if (!obstacleMetadata.TryGetValue(pool, out var info))
        {
            Debug.LogWarning($"[TrackGenerator] No obstacle metadata registered for pool '{pool.name}'.");
            return false;
        }

        var go = pool.Get();
        if (!go)
        {
            Debug.LogWarning($"[TrackGenerator] Pool '{pool.name}' failed to provide an obstacle instance.");
            return false;
        }

        var handle = go.GetComponent<ObstacleHandle>();
        if (!handle) handle = go.AddComponent<ObstacleHandle>();
        handle.Configure(info.type, info.tag);

        go.transform.position = new Vector3(laneX, 0f, z);
        live.Add(go);

        if (handle.BlocksLane && go.TryGetComponent(out PooledRef pooled))
        {
            activeObstacles.Add(pooled);
        }

        return true;
    }

    void UnregisterObstacle(GameObject go)
    {
        if (!go) return;
        if (!go.TryGetComponent(out ObstacleHandle handle)) return;
        if (!handle.BlocksLane) return;
        if (go.TryGetComponent(out PooledRef pooled))
        {
            activeObstacles.Remove(pooled);
        }
    }

    static bool IsObstacle(GameObject go)
    {
        if (!go) return false;
        return go.TryGetComponent(out ObstacleHandle handle) && handle.BlocksLane;
    }
}
