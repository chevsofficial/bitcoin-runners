// Assets/Scripts/UI/ResultsPanelBinder.cs
using UnityEngine;

[DefaultExecutionOrder(-90)] // after GameManager (-100)
public class ResultsPanelBinder : MonoBehaviour
{
    void Awake()
    {
        if (GameManager.I != null)
            GameManager.I.resultsPanel = gameObject;

        // keep panel hidden on scene load; GM will show it on death
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
