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
        var ps = fx.GetComponent<ParticleSystem>();
        if (ps)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.gravityModifier = 0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            var bursts = new ParticleSystem.Burst[1];
            bursts[0] = new ParticleSystem.Burst(0f, 4, 6);
            emission.SetBursts(bursts);

            ps.Clear(true);
            ps.Play(true);
        }
        SimplePool.RecycleAfter(fx, 0.6f);
    }
}
