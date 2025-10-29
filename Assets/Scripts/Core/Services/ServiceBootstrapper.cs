using System;
using UnityEngine;

/// <summary>
/// Simple bootstrapper that spins up the core services from a dedicated scene.
/// </summary>
public class ServiceBootstrapper : MonoBehaviour
{
    [Tooltip("Prefabs that each contain one or more service components.")]
    public GameObject[] servicePrefabs;

    static bool _bootstrapped;

    static readonly Type[] _requiredServices =
    {
        typeof(GameManager),
        typeof(InputManager),
        typeof(RunSession),
        typeof(AudioManager),
        typeof(AdsManager),
        typeof(IAPManager),
        typeof(AnalyticsManager)
    };

    void Awake()
    {
        if (_bootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        _bootstrapped = true;
        DontDestroyOnLoad(gameObject);

        SpawnPrefabs();
        ValidateRequiredServices();
    }

    void SpawnPrefabs()
    {
        if (servicePrefabs == null) return;

        foreach (var prefab in servicePrefabs)
        {
            if (!prefab) continue;

            var instance = Instantiate(prefab, transform);
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
        if (!_bootstrapped) return;

        _bootstrapped = false;
        ServiceLocator.ShutdownAll();
    }
}
