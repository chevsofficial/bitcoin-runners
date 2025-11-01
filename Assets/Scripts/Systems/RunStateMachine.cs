// Assets/Scripts/Systems/RunStateMachine.cs
using System;
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

    [Header("Scene Hooks")]
    [Tooltip("Optional override if the results panel cannot register itself in time.")]
    [SerializeField] GameObject resultsPanelOverride;

    public RunState CurrentState { get; private set; } = RunState.None;

    public event Action<RunState, RunState> StateChanged;
    public event Action<RunState> StateEntered;

    GameObject _resultsPanel;
    CanvasGroup _resultsCanvasGroup;
    Canvas _resultsCanvas;

    public override void Initialize()
    {
        _resultsPanel = resultsPanelOverride;
        CachePanelComponents();
        CurrentState = RunState.None;
        ConfigureResultsPanel(false);
    }

    public override void Shutdown()
    {
        StateChanged = null;
        StateEntered = null;
        _resultsPanel = null;
        _resultsCanvas = null;
        _resultsCanvasGroup = null;
        CurrentState = RunState.None;
    }

    public void RegisterResultsPanel(GameObject panel)
    {
        if (!panel) return;

        _resultsPanel = panel;
        CachePanelComponents();

        bool shouldShow = CurrentState == RunState.Results || CurrentState == RunState.Dead;
        ConfigureResultsPanel(shouldShow);
    }

    public void BeginRun(RunnerController runner, bool continuing, float continueDistance)
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
                gm.SetDistance(continueDistance);
            }

            if (runner != null)
            {
                Vector3 p = runner.transform.position;
                runner.transform.position = new Vector3(p.x, p.y, continueDistance);

                var pu = runner.GetComponent<PowerupSystem>();
                pu?.Activate(PowerType.Shield, 2f);
            }
        }

        if (RunSession.I != null)
        {
            RunSession.I.hasPendingContinue = false;
            RunSession.I.continueDistance = continuing ? continueDistance : 0f;
        }

        ConfigureResultsPanel(false);
        TransitionTo(RunState.Running);
    }

    public void HandleRunnerDeath()
    {
        if (CurrentState == RunState.Dead || CurrentState == RunState.Results)
            return;

        GameManager.I?.KillPlayer();

        if (Time.timeScale < 0.99f)
            Time.timeScale = 1f;

        TransitionTo(RunState.Dead);
        ConfigureResultsPanel(true);
        TransitionTo(RunState.Results);
    }

    void ConfigureResultsPanel(bool visible)
    {
        if (!ResolveResultsPanel()) return;

        if (visible)
        {
            Transform t = _resultsPanel.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }

            _resultsPanel.transform.SetAsLastSibling();

            if (_resultsCanvasGroup == null)
                _resultsCanvasGroup = _resultsPanel.GetComponent<CanvasGroup>();
            if (_resultsCanvasGroup == null)
                _resultsCanvasGroup = _resultsPanel.AddComponent<CanvasGroup>();

            _resultsCanvasGroup.alpha = 1f;
            _resultsCanvasGroup.interactable = true;
            _resultsCanvasGroup.blocksRaycasts = true;

            if (_resultsCanvas == null)
                _resultsCanvas = _resultsPanel.GetComponent<Canvas>();
            if (_resultsCanvas == null)
                _resultsCanvas = _resultsPanel.AddComponent<Canvas>();

            _resultsCanvas.overrideSorting = true;
            _resultsCanvas.sortingOrder = 1000;
        }
        else
        {
            if (_resultsCanvasGroup != null)
            {
                _resultsCanvasGroup.alpha = 0f;
                _resultsCanvasGroup.interactable = false;
                _resultsCanvasGroup.blocksRaycasts = false;
            }

            if (_resultsPanel.activeSelf)
                _resultsPanel.SetActive(false);
        }
    }

    bool ResolveResultsPanel()
    {
        if (_resultsPanel)
            return true;

        if (resultsPanelOverride)
        {
            _resultsPanel = resultsPanelOverride;
            CachePanelComponents();
            return _resultsPanel != null;
        }

#if UNITY_2021_3_OR_NEWER
        var rc = UnityEngine.Object.FindFirstObjectByType<ResultsController>(FindObjectsInactive.Include);
#else
        var rc = UnityEngine.Object.FindObjectOfType<ResultsController>(true);
#endif
        if (rc)
        {
            RegisterResultsPanel(rc.gameObject);
        }

        return _resultsPanel != null;
    }

    void CachePanelComponents()
    {
        if (!_resultsPanel)
        {
            _resultsCanvasGroup = null;
            _resultsCanvas = null;
            return;
        }

        _resultsCanvasGroup = _resultsPanel.GetComponent<CanvasGroup>();
        _resultsCanvas = _resultsPanel.GetComponent<Canvas>();
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
