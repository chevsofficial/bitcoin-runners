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
            GameManager.I?.KillPlayer();
#else
        if (Input.GetKeyDown(KeyCode.K))
            GameManager.I?.KillPlayer();
#endif
    }
}
