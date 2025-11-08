// Assets/Scripts/Systems/RunStateMachine.cs
using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-95)] // after GameManager but before ResultsPanelBinder
public class RunStateMachine : SingletonServiceBehaviour<RunStateMachine>
{
    public static RunStateMachine I => ServiceLocator.TryGet(out RunStateMachine service) ? service : null;

    public enum RunState
    {
        None,
        Bootstrapping,
        Running,
        Dead,
        Results
    }

    public RunState CurrentState { get; private set; } = RunState.None;

    public event Action<RunState, RunState> StateChanged;
    public event Action<RunState> StateEntered;

    public override void Initialize()
    {
        CurrentState = RunState.None;
    }

    public override void Shutdown()
    {
        StateChanged = null;
        StateEntered = null;
        CurrentState = RunState.None;
    }

    public void BeginRun(RunnerController runner, bool continuing, float continueDistance, float continueElapsed)
    {
        TransitionTo(RunState.Bootstrapping);

        var gm = GameManager.I;
        gm?.ResetRun();

        if (continuing)
        {
            if (gm != null)
            {
                float startSpeed = gm.cfg ? gm.cfg.startSpeed : gm.Speed;
                gm.OverrideSpeed(startSpeed);
                gm.RestoreRunProgress(continueDistance, continueElapsed);
            }

            if (runner != null)
            {
                Vector3 p = runner.transform.position;
                runner.transform.position = new Vector3(p.x, p.y, continueDistance);

                var pu = runner.GetComponent<PowerupSystem>();
                pu?.Activate(PowerType.Shield, 2f);
            }

            if (gm != null)
            {
                StartCoroutine(ResumeAutomaticSpeedNextFrame());
            }
        }

        if (RunSession.I != null)
        {
            if (!continuing && RunSession.I.x2GrantedThisResults)
            {
                RunSession.I.x2GrantedThisResults = false;
            }

            RunSession.I.hasPendingContinue = false;
            RunSession.I.continueDistance = continuing ? continueDistance : 0f;
            RunSession.I.continueElapsed = continuing ? continueElapsed : 0f;
            RunSession.I.PersistState();
        }

        TransitionTo(RunState.Running);
    }

    IEnumerator ResumeAutomaticSpeedNextFrame()
    {
        yield return null;
        GameManager.I?.ReleaseSpeedOverride();
    }

    public void HandleRunnerDeath()
    {
        if (CurrentState == RunState.Dead || CurrentState == RunState.Results)
            return;

        GameManager.I?.KillPlayer();

        if (Time.timeScale < 0.99f)
            Time.timeScale = 1f;

        TransitionTo(RunState.Dead);
        TransitionTo(RunState.Results);
    }

    void TransitionTo(RunState next)
    {
        if (CurrentState == next) return;

        var prev = CurrentState;
        CurrentState = next;
        StateChanged?.Invoke(prev, next);
        StateEntered?.Invoke(next);
    }
}
