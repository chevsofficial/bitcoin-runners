using UnityEngine;

[DefaultExecutionOrder(-50)] // run early so director sees the spawner before gameplay
public sealed class ObstacleSpawnerBinder : MonoBehaviour
{
    void Awake()
    {
        var spawner = GetComponent<IObstacleSpawner>();
        if (spawner == null) return;

        var director = FindObjectOfType<ObstacleDirector>(true);
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
