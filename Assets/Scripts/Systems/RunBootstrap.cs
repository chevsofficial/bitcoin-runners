using UnityEngine;
using UnityEngine.SceneManagement;

public class RunBootstrap : MonoBehaviour
{
    void Start()
    {
        AnalyticsManager.I?.RunStart();

        if (RunSession.I && RunSession.I.hasPendingContinue)
        {
            GameManager.I.ResetRun();
            GameManager.I.OverrideSpeed(GameManager.I.cfg.startSpeed);
            GameManager.I.AddCoin(0);
            GameManager.I.SetDistance(RunSession.I.continueDistance);

            // Find the runner (new API on 2023.1+, old API otherwise)
#if UNITY_2023_1_OR_NEWER
            var runner = FindFirstObjectByType<RunnerController>();
#else
            var runner = FindObjectOfType<RunnerController>();
#endif
            if (runner)
            {
                var p = runner.transform.position;
                runner.transform.position = new Vector3(p.x, p.y, RunSession.I.continueDistance);

                var pu = runner.GetComponent<PowerupSystem>();
                pu?.Activate(PowerType.Shield, 2f);
            }

            RunSession.I.hasPendingContinue = false;
        }
    }
}
