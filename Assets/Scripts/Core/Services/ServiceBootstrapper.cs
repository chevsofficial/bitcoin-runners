using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple bootstrapper that spins up the core services from a dedicated scene.
/// </summary>
public class ServiceBootstrapper : MonoBehaviour
{
    [Tooltip("Prefabs that each contain one or more service components.")]
    public GameObject[] servicePrefabs;

    [Tooltip("Scene to load after services are up (e.g., Menu or Run).")]
    public string firstScene = "Run"; // or "Menu"

    static ServiceBootstrapper _instance;
    static bool _bootstrapped;

    static readonly Type[] _requiredServices =
    {
        typeof(GameManager),
        typeof(InputManager),
        typeof(RunSession),
        typeof(AudioManager),
        typeof(AdsManager),
        typeof(IAPManager),
        typeof(AnalyticsManager),
        typeof(ObstacleDirector)
    };

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (_bootstrapped)
        {
            return;
        }

        _bootstrapped = true;
        DontDestroyOnLoad(gameObject);

        SpawnPrefabs();
        ValidateRequiredServices();
        var active = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(firstScene) && active != firstScene)
        {
            SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
        }

    }

    void SpawnPrefabs()
    {
        if (servicePrefabs == null) return;

        foreach (var prefab in servicePrefabs)
        {
            if (!prefab) continue;

            // Spawn at scene root so DontDestroyOnLoad is valid
            var instance = Instantiate(prefab);
            instance.name = prefab.name;

        }
    }

    void ValidateRequiredServices()
    {
        foreach (var type in _requiredServices)
        {
            if (!ServiceLocator.Contains(type))
            {
                Debug.LogError($"ServiceBootstrapper: required service of type {type.Name} was not registered. Check the bootstrap scene configuration.");
            }
        }
    }

    void OnDestroy()
    {
        if (!_bootstrapped || _instance != this) return;

        _bootstrapped = false;
        _instance = null;
        ServiceLocator.ShutdownAll();
    }
}
