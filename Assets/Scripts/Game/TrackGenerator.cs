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
    int _activeObstacles;
    readonly List<GameObject> live = new();

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
            if (!spawned && maxConcurrent > 0 && _activeObstacles >= maxConcurrent)
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
                if (IsObstacle(go))
                {
                    _activeObstacles = Mathf.Max(0, _activeObstacles - 1);
                }
                live.RemoveAt(i);
                continue;
            }

            if (runner && go.transform.position.z < runner.position.z - 12f)
            {
                live.RemoveAt(i);
                if (IsObstacle(go))
                {
                    _activeObstacles = Mathf.Max(0, _activeObstacles - 1);
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
        if (maxConcurrent > 0 && _activeObstacles >= maxConcurrent)
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
                if (maxConcurrent > 0 && _activeObstacles >= maxConcurrent) break;
                if (UnityEngine.Random.value < minEmptyLaneChance) continue;

                GameObject obs;
                if (!advanced)
                {
                    obs = barrierPool.Get();
                    obs.tag = "ObstacleBarrier";
                }
                else
                {
                    int pick = UnityEngine.Random.Range(0, 3); // 0 barrier, 1 lowbar, 2 gap
                    if (pick == 0)
                    {
                        obs = barrierPool.Get(); obs.tag = "ObstacleBarrier";
                    }
                    else if (pick == 1)
                    {
                        obs = lowBarPool.Get(); obs.tag = "ObstacleLowBar";
                    }
                    else
                    {
                        obs = gapPool.Get(); obs.tag = "ObstacleGap";
                    }
                }

                float laneX = LaneCoords.Get(lane);
                obs.transform.position = new Vector3(laneX, 0f, baseZ + 6f);
                live.Add(obs);
                _activeObstacles++;
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

    static bool IsObstacle(GameObject go)
    {
        if (!go) return false;
        return go.CompareTag("ObstacleBarrier") || go.CompareTag("ObstacleLowBar") || go.CompareTag("ObstacleGap");
    }
}
