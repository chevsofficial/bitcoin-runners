// Assets/Scripts/UI/ResultsPanelBinder.cs
using UnityEngine;

[DefaultExecutionOrder(-90)] // after GameManager (-100)
public class ResultsPanelBinder : MonoBehaviour
{
    void Awake()
    {
        if (RunStateMachine.I != null)
            RunStateMachine.I.RegisterResultsPanel(gameObject);

        // keep panel hidden on scene load; the run state machine will reveal it on death
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
