// Assets/Scripts/FX/FXSpawner.cs
using UnityEngine;

public static class FXSpawner
{
    public static GameObject DebrisPrefab;

    public static void BurstDebris(Vector3 pos)
    {
        if (!DebrisPrefab) return;

        var fx = SimplePool.GetOrInstantiate(DebrisPrefab);
        fx.transform.position = pos + Vector3.up * 0.5f;

        // Hard reset all ParticleSystems (root + children) before (re)playing
        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        float maxTime = 0f;

        foreach (var ps in systems)
        {
            // Stop + clear to avoid "setting duration while playing" warnings
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Track how long we should keep the object alive
            var main = ps.main;
            var lifetimeMax = main.startLifetime.constantMax; // handles "Two Constants" too
            float sysTime = main.duration + lifetimeMax;
            if (sysTime > maxTime) maxTime = sysTime;
        }

        // Now (re)play all particle systems
        foreach (var ps in systems)
        {
            ps.Play(true);
        }

        // Recycle after all sub-systems are done (little padding)
        SimplePool.RecycleAfter(fx, maxTime + 0.05f);
    }
}
