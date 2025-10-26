using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "BR/Balance/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Speed (m/s) over time (seconds)")]
    public AnimationCurve speedOverTime = AnimationCurve.Linear(0, 6f, 180f, 18f);
    public float speedCap = 20f;

    [Header("Obstacle Density (0..1) over time (seconds)")]
    public AnimationCurve obstacleDensityOverTime = AnimationCurve.Linear(0, 0.20f, 180f, 0.75f);

    [Header("Optional Tapering near cap")]
    [Tooltip("Blend-in factor as speed nears cap, to avoid harsh ceiling hits.")]
    public float taperWindow = 3f;

    public float SpeedAt(float t)
    {
        float raw = speedOverTime.Evaluate(Mathf.Max(0, t));
        if (speedCap <= 0) return Mathf.Max(0, raw);
        // Soft-taper near cap (smaller but frequent ramps feel)
        if (taperWindow > 0f && raw > speedCap - taperWindow)
        {
            float a = Mathf.InverseLerp(speedCap - taperWindow, speedCap, raw);
            raw = Mathf.Lerp(raw, Mathf.Min(raw, speedCap), a);
        }
        return Mathf.Min(speedCap, raw);
    }

    public float DensityAt(float t)
    {
        return Mathf.Clamp01(obstacleDensityOverTime.Evaluate(Mathf.Max(0, t)));
    }
}
