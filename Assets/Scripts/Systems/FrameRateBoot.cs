using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class FrameRateBoot : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.vSyncCount = 0; // editor/standalone only; mobile ignores
        UnityEngine.Application.targetFrameRate = 60;
    }
}
