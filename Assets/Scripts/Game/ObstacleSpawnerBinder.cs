using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(100)]
public sealed class ObstacleSpawnerBinder : MonoBehaviour
{
    void Start()
    {
        var spawner = GetComponent<IObstacleSpawner>();
        if (spawner == null) return;
        StartCoroutine(ResolveAndBind(spawner));
    }

    IEnumerator ResolveAndBind(IObstacleSpawner spawner)
    {
        ObstacleDirector director = null;
        const int maxFrames = 60; // ~1 second at 60fps
        for (int i = 0; i < maxFrames && director == null; i++)
        {
#if UNITY_2023_1_OR_NEWER
            director = Object.FindFirstObjectByType<ObstacleDirector>(FindObjectsInactive.Include);
#else
            director = Object.FindObjectOfType<ObstacleDirector>(true);
#endif
            if (director == null) yield return null; // wait a frame for ServiceBootstrapper to finish
        }

        if (director != null)
        {
            director.SetSpawner(spawner);
        }
        else
        {
            Debug.LogError("[ObstacleSpawnerBinder] ObstacleDirector not found after waiting ~1s. Is it spawned from Boot?");
        }
    }
}
