// Assets/Scripts/Game/TrackGenerator.cs
using UnityEngine;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour
{
    public GameConstants cfg;
    public Transform runner;

    // Obstacle/coin pools
    public SimplePool coinPool, barrierPool, lowBarPool, gapPool;

    // NEW: powerup pools
    public SimplePool puMagnetPool, puShieldPool, puDashPool;

    float spawnedZ;
    List<GameObject> live = new();

    void Start()
    {
        spawnedZ = Mathf.Floor(GameManager.I.Distance / cfg.segmentLen) * cfg.segmentLen;
        // Pre-spawn ahead of starting distance
        while (spawnedZ < GameManager.I.Distance + 60f) { SpawnSegment(); }
    }

    void Update()
    {
        if (!GameManager.I.Alive) return;

        // keep segments spawned ahead
        while (spawnedZ < GameManager.I.Distance + 60f)
        {
            SpawnSegment();
        }

        // recycle objects behind runner
        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (live[i].activeSelf && live[i].transform.position.z < runner.position.z - 12f)
            {
                var go = live[i];
                live.RemoveAt(i);
                // spawn quick debris for obstacles only
                if (go.CompareTag("ObstacleBarrier") || go.CompareTag("ObstacleLowBar") || go.CompareTag("ObstacleGap"))
                {
                    FXSpawner.BurstDebris(go.transform.position);
                }
                SimplePool.RecycleAny(go);
            }
        }
    }

    void SpawnSegment()
    {
        float baseZ = spawnedZ + cfg.segmentLen;
        spawnedZ = baseZ;

        int safeLane = UnityEngine.Random.Range(0, 3);

        // difficulty ramp: which obstacle type to use on blocked lanes?
        bool advanced = GameManager.I.Distance >= 200f;
        for (int lane = 0; lane < 3; lane++)
        {
            if (lane == safeLane) continue;

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
            obs.transform.position = new Vector3((lane - 1) * cfg.laneWidth, 0f, baseZ + 6f);
            live.Add(obs);
        }

        // coin line on safe lane
        for (int k = 0; k < 10; k++)
        {
            var c = coinPool.Get();
            c.transform.position = new Vector3((safeLane - 1) * cfg.laneWidth, 1f, baseZ + 2f + k * 1.6f);
            live.Add(c);
        }

        // --- NEW: occasional powerup on safe lane (12% chance), only after 100m ---
        if (GameManager.I.Distance > 100f && UnityEngine.Random.value < 0.12f)
        {
            GameObject pgo = null;
            int p = UnityEngine.Random.Range(0, 3); // 0=Magnet, 1=Shield, 2=Dash

            if (p == 0 && puMagnetPool != null) pgo = puMagnetPool.Get();
            else if (p == 1 && puShieldPool != null) pgo = puShieldPool.Get();
            else if (puDashPool != null) pgo = puDashPool.Get();

            if (pgo != null)
            {
                pgo.transform.position = new Vector3((safeLane - 1) * cfg.laneWidth, 1.0f, baseZ + 10f);
                live.Add(pgo);
            }
        }
    }
}
