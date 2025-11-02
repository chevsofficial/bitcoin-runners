using UnityEngine;

[DefaultExecutionOrder(-50)] // run early so director sees the spawner before gameplay
public sealed class ObstacleSpawnerBinder : MonoBehaviour
{
    void Awake()
    {
        var spawner = GetComponent<IObstacleSpawner>();
        if (spawner == null) return;

                // Unity 2023.1+: use the new API. Older versions: fall back to the deprecated call.
        
#if UNITY_2023_1_OR_NEWER
        var director = Object.FindFirstObjectByType<ObstacleDirector>(FindObjectsInactive.Include);
#else
        var director = Object.FindObjectOfType<ObstacleDirector>(true);
#endif
        if (director != null)
        {
            director.SetSpawner(spawner);
        }
        else
        {
            Debug.LogError("[ObstacleSpawnerBinder] ObstacleDirector not found in scene/persistent objects.");
        }
    }
}
