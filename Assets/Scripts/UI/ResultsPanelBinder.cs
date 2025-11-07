// Assets/Scripts/UI/ResultsPanelBinder.cs
using UnityEngine;

[DefaultExecutionOrder(-90)] // after GameManager (-100)
public class ResultsPanelBinder : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] bool overrideSorting = true;
    [SerializeField] int sortingOrder = 1000;

    CanvasGroup _canvasGroup;
    Canvas _canvas;
    ResultsController _controller;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.overrideSorting = overrideSorting;
        if (overrideSorting)
        {
            _canvas.sortingOrder = sortingOrder;
        }

        _controller = GetComponent<ResultsController>();

        var stateMachine = RunStateMachine.I;
        if (stateMachine != null)
        {
            stateMachine.StateEntered += OnStateEntered;
            stateMachine.StateChanged += OnStateChanged;
        }

        Show(false);
    }

    void OnDestroy()
    {
        var stateMachine = RunStateMachine.I;
        if (stateMachine != null)
        {
            stateMachine.StateEntered -= OnStateEntered;
            stateMachine.StateChanged -= OnStateChanged;
        }
    }

    void OnStateChanged(RunStateMachine.RunState previous, RunStateMachine.RunState next)
    {
        if (previous == RunStateMachine.RunState.Results && next != RunStateMachine.RunState.Results)
        {
            if (_controller != null)
            {
                _controller.EndResultsFlow();
            }
            Show(false);
        }
    }

    void OnStateEntered(RunStateMachine.RunState state)
    {
        if (state != RunStateMachine.RunState.Results)
            return;

        ActivateHierarchy();
        Show(true);

        if (_controller != null)
        {
            _controller.BeginResultsFlow();
        }
    }

    void ActivateHierarchy()
    {
        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        transform.SetAsLastSibling();
    }

    void Show(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;

        if (!visible && gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
