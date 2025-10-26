using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class HitRouter : MonoBehaviour
{
    [Header("Scene Refs (assign in Inspector or auto-find)")]
    [SerializeField] RunnerController runner;            // optional; auto-found
    [SerializeField] CharacterController controller;     // optional; auto-found

    [Header("Timing")]
    [SerializeField] int hitStopFrames = 8;              // 6–10 feels good
    [SerializeField] float resultsDelayRealtime = 0.05f; // brief unscaled delay after hit-stop

    bool _isDead;

    void Awake()
    {
        if (runner == null) runner = GetComponent<RunnerController>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    /// <summary>Call this when a fatal hit is detected.</summary>
    public void TryKill()
    {
        if (_isDead) return;
        _isDead = true;

        AudioManager.I?.PlayHit();
        Haptics.Heavy();
        GameEvents.Hit();
        HitStop.I?.DoHitStopFrames(hitStopFrames);

        // Stop movement & collisions to avoid re-entrant hits
        if (runner != null) runner.enabled = false;
        if (controller != null)
        {
            controller.detectCollisions = false;
            controller.enabled = false;
        }

        StartCoroutine(ShowResultsSequence());
    }

    IEnumerator ShowResultsSequence()
    {
        // let hit-stop finish (unscaled)
        yield return new WaitForSecondsRealtime(resultsDelayRealtime);

        // ensure UI isn’t stuck paused
        if (Time.timeScale < 0.99f) Time.timeScale = 1f;

        // delegate to GameManager — it shows the ResultsController panel & sets sorting
        if (GameManager.I != null)
        {
            GameManager.I.KillPlayer();
        }
        else
        {
            Debug.LogError("HitRouter: GameManager.I is null; cannot show results.");
        }
    }
}
