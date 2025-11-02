using UnityEngine;
using UnityEngine.SceneManagement;

public class RunBootstrap : MonoBehaviour
{
    void Start()
    {
        AnalyticsManager.I?.RunStart();

        bool continuing = RunSession.I && RunSession.I.hasPendingContinue;
        float continueDistance = continuing ? RunSession.I.continueDistance : 0f;

        // Find the runner (new API on 2023.1+, old API otherwise)
#if UNITY_2023_1_OR_NEWER
        var runner = FindFirstObjectByType<RunnerController>();
#else
        var runner = FindObjectOfType<RunnerController>();
#endif

        if (RunStateMachine.I != null)
        {
            RunStateMachine.I.BeginRun(runner, continuing, continueDistance);
        }
        else if (continuing)
        {
            // Fallback to legacy behaviour if the run state machine is unavailable.
            GameManager.I.ResetRun();
            GameManager.I.OverrideSpeed(GameManager.I.cfg.startSpeed);
            GameManager.I.SetDistance(continueDistance);

            if (runner)
            {
                var p = runner.transform.position;
                runner.transform.position = new Vector3(p.x, p.y, continueDistance);

                var pu = runner.GetComponent<PowerupSystem>();
                pu?.Activate(PowerType.Shield, 2f);
            }

            if (RunSession.I != null)
            {
                RunSession.I.hasPendingContinue = false;
                RunSession.I.continueDistance = 0f;
                RunSession.I.PersistState();
            }
        }
    }
}
