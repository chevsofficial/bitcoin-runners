using System.Collections;
using UnityEngine;

public class RunBootstrap : MonoBehaviour
{
    void Start()
    {
        AnalyticsManager.I?.RunStart();

        var runSession = RunSession.I;
        var runStateMachine = RunStateMachine.I;
        var gameManager = GameManager.I;

        bool continuing = runSession != null && runSession.hasPendingContinue;
        float continueDistance = continuing ? runSession.continueDistance : 0f;
        float continueElapsed = continuing ? runSession.continueElapsed : 0f;
        int continueCoins = continuing ? runSession.continueCoins : 0;

        // Find the runner (new API on 2023.1+, old API otherwise)
#if UNITY_2023_1_OR_NEWER
        var runner = FindFirstObjectByType<RunnerController>();
#else
        var runner = FindObjectOfType<RunnerController>();
#endif

        if (runStateMachine != null)
        {
            runStateMachine.BeginRun(runner, continuing, continueDistance, continueElapsed);
        }
        else if (continuing)
        {
            Debug.LogError("RunBootstrap: RunStateMachine service is not available; using legacy continue flow.");

            // Fallback to legacy behaviour if the run state machine is unavailable.
            if (gameManager == null)
            {
                Debug.LogError("RunBootstrap: GameManager service is required for legacy continue handling but is unavailable.");
                return;
            }

            gameManager.ResetRun();
            gameManager.OverrideSpeed(gameManager.cfg.startSpeed);
            gameManager.RestoreRunProgress(continueDistance, continueElapsed);
            gameManager.SetCoins(continueCoins);

            if (runner)
            {
                var p = runner.transform.position;
                runner.transform.position = new Vector3(p.x, p.y, continueDistance);

                var pu = runner.GetComponent<PowerupSystem>();
                pu?.Activate(PowerType.Shield, 2f);
            }

            StartCoroutine(ReleaseSpeedOverrideNextFrame(gameManager));

            if (runSession != null)
            {
                runSession.hasPendingContinue = false;
                runSession.continueDistance = 0f;
                runSession.continueElapsed = 0f;
                runSession.continueCoins = 0;
                runSession.PersistState();
            }
            else
            {
                Debug.LogError("RunBootstrap: Unable to persist continue state because RunSession service is unavailable.");
            }
        }
        else
        {
            Debug.LogError("RunBootstrap: RunStateMachine service is not available; cannot start run.");
        }
    }

    IEnumerator ReleaseSpeedOverrideNextFrame(GameManager gameManager)
    {
        yield return null;
        if (gameManager != null)
        {
            gameManager.ReleaseSpeedOverride();
        }
        else
        {
            Debug.LogError("RunBootstrap: Unable to release speed override because GameManager service is unavailable.");
        }
    }
}
