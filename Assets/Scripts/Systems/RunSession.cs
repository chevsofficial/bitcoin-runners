using UnityEngine;

public class RunSession : MonoBehaviour
{
    public static RunSession I;
    public bool hasPendingContinue;
    public float continueDistance;
    public bool x2GrantedThisResults;

    void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

    public static float CheckpointStride = 150f;
    public static float LastCheckpoint(float distance)
    {
        return Mathf.Floor(distance / CheckpointStride) * CheckpointStride;
    }

    public void Clear() { hasPendingContinue = false; x2GrantedThisResults = false; continueDistance = 0f; }
}
