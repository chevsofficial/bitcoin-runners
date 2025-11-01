// Assets/Scripts/Debug/ResultsDebug.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ResultsDebug : MonoBehaviour
{
    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            TriggerResults();
#else
        if (Input.GetKeyDown(KeyCode.K))
            TriggerResults();
#endif
    }

    void TriggerResults()
    {
        if (RunStateMachine.I != null)
        {
            RunStateMachine.I.HandleRunnerDeath();
        }
        else
        {
            GameManager.I?.KillPlayer();
        }
    }
}
