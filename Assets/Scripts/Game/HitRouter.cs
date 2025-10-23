using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug; // <-- resolves Debug ambiguity

public class HitRouter : MonoBehaviour
{
    [Header("Scene Refs (assign in Inspector or auto-find)")]
    [SerializeField] RunnerController runner;              // optional; auto-found
    [SerializeField] CharacterController controller;       // optional; auto-found
    [SerializeField] ResultsController resultsController;  // your ResultsController in the scene (can be inactive)

    [Header("Timing")]
    [SerializeField] int hitStopFrames = 8;                // 6–10 feels good
    [SerializeField] float resultsDelayRealtime = 0.05f;   // brief unscaled delay after hit-stop

    bool _isDead;

    void Awake()
    {
        if (runner == null) runner = GetComponent<RunnerController>();
        if (controller == null) controller = GetComponent<CharacterController>();

        // Find ResultsController even if inactive
        if (resultsController == null)
            resultsController = FindFirstObjectByType<ResultsController>(FindObjectsInactive.Include);
    }

    /// <summary>Call this when a fatal hit is detected.</summary>
    public void TryKill()
    {
        if (_isDead) return;
        _isDead = true;

        GameEvents.Hit();
        HitStop.I?.DoHitStopFrames(hitStopFrames);

        // Stop movement & collisions to avoid re-entrant hits
        if (runner != null) runner.enabled = false;
        if (controller != null)
        {
            controller.detectCollisions = false;
            controller.enabled = false;
        }

        // If you have a public method, use that instead:
        // GameManager.I?.OnPlayerDied(); // <-- only if it exists

        StartCoroutine(ShowResultsSequence());
    }

    IEnumerator ShowResultsSequence()
    {
        yield return new WaitForSecondsRealtime(resultsDelayRealtime);

        if (Time.timeScale < 0.99f) Time.timeScale = 1f;

        if (resultsController == null)
            resultsController = FindFirstObjectByType<ResultsController>(FindObjectsInactive.Include);

        if (resultsController == null)
        {
            Debug.LogError("HitRouter: ResultsController not found. Place one in the scene and (optionally) assign it in the Inspector.");
            yield break;
        }

        if (!resultsController.gameObject.activeSelf)
            resultsController.gameObject.SetActive(true); // OnEnable() inside ResultsController will refresh UI/ads
    }
}
