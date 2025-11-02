using UnityEngine;

public class RunSession : SingletonServiceBehaviour<RunSession>
{
    public static RunSession I => ServiceLocator.TryGet(out RunSession service) ? service : null;
    public bool hasPendingContinue;
    public float continueDistance;
    public bool x2GrantedThisResults;

    public override void Initialize()
    {
        LoadPersistedState();
    }

    public override void Shutdown()
    {
        PersistState();
    }

    public static float CheckpointStride = 150f;
    public static float LastCheckpoint(float distance)
    {
        return Mathf.Floor(distance / CheckpointStride) * CheckpointStride;
    }

    void LoadPersistedState()
    {
        hasPendingContinue = SaveSystem.Data.runHasPendingContinue;
        continueDistance = SaveSystem.Data.runContinueDistance;
        x2GrantedThisResults = SaveSystem.Data.runX2Consumed;
    }

    public void PersistState()
    {
        SaveSystem.Data.runHasPendingContinue = hasPendingContinue;
        SaveSystem.Data.runContinueDistance = continueDistance;
        SaveSystem.Data.runX2Consumed = x2GrantedThisResults;
        SaveSystem.Save();
    }

    public void Clear()
    {
        hasPendingContinue = false;
        x2GrantedThisResults = false;
        continueDistance = 0f;
        PersistState();
    }
}
