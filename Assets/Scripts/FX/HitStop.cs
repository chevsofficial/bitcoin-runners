using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    // Singleton utility (attach anywhere in scene once)
    public static HitStop I { get; private set; }

    [Range(0f, 0.25f)] public float minScale = 0.1f; // 0.0–0.15 recommended
    public AnimationCurve easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void DoHitStopFrames(int frames = 8)
    {
        StartCoroutine(HitStopCo(frames));
    }

    IEnumerator HitStopCo(int frames)
    {
        // freeze for N frames (unscaled time)
        float prevScale = Time.timeScale;
        Time.timeScale = minScale;
        float target = prevScale;
        // wait N frames while paused-ish
        for (int i = 0; i < Mathf.Max(1, frames); i++)
            yield return new WaitForEndOfFrame();

        // ease back over ~N frames worth of time (unscaled)
        float dur = frames / 60f; // ~6–10 frames ≈ 0.1–0.17s @60fps
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            Time.timeScale = Mathf.Lerp(minScale, target, easeOut.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        Time.timeScale = target;
    }
}
