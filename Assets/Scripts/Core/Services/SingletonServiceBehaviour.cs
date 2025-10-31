using UnityEngine;

/// <summary>
/// Convenience base class for persistent singleton-style services.
/// Handles registration, initialization, and teardown automatically.
/// </summary>
public abstract class SingletonServiceBehaviour<T> : MonoBehaviour, IGameService where T : MonoBehaviour, IGameService
{
    bool _initialized;

    protected virtual void Awake()
    {
        if (!ServiceLocator.TryRegister(this as T))
        {
            // Duplicate instance detected. The existing one stays alive.
            Destroy(gameObject);
            return;
        }

        // Ensure this object is a root before marking as persistent
        transform.SetParent(null, worldPositionStays: true);
        DontDestroyOnLoad(gameObject);
        Initialize();
        _initialized = true;
    }

    protected virtual void OnDestroy()
    {
        if (!_initialized) return;

        if (ServiceLocator.TryDeregister(this as T))
        {
            Shutdown();
        }
    }

    public abstract void Initialize();
    public abstract void Shutdown();
}
