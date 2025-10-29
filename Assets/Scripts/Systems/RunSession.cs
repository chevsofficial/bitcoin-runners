using UnityEngine;

public class RunSession : SingletonServiceBehaviour<RunSession>
{
    public static RunSession I => ServiceLocator.TryGet(out RunSession service) ? service : null;
    public bool hasPendingContinue;
    public float continueDistance;
    public bool x2GrantedThisResults;

    public override void Initialize()
    {
        Clear();
    }

    public override void Shutdown()
    {
        Clear();
    }

    public static float CheckpointStride = 150f;
    public static float LastCheckpoint(float distance)
    {
        return Mathf.Floor(distance / CheckpointStride) * CheckpointStride;
    }

    public void Clear() { hasPendingContinue = false; x2GrantedThisResults = false; continueDistance = 0f; }
}
