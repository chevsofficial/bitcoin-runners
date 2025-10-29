using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight service locator that keeps the singleton-style managers honest.
/// </summary>
public static class ServiceLocator
{
    static readonly Dictionary<Type, IGameService> _services = new();

    public static bool TryRegister<T>(T service) where T : class, IGameService
    {
        var type = typeof(T);
        if (_services.TryGetValue(type, out var existing))
        {
            if (!ReferenceEquals(existing, service))
            {
                Debug.LogWarning($"ServiceLocator: attempting to register duplicate service of type {type.Name}. The existing instance will stay active.");
            }
            return false;
        }

        _services.Add(type, service);
        return true;
    }

    public static bool TryDeregister<T>(T service) where T : class, IGameService
    {
        var type = typeof(T);
        if (_services.TryGetValue(type, out var existing) && ReferenceEquals(existing, service))
        {
            _services.Remove(type);
            return true;
        }
        return false;
    }

    public static bool TryGet<T>(out T service) where T : class, IGameService
    {
        if (_services.TryGetValue(typeof(T), out var existing))
        {
            service = (T)existing;
            return true;
        }

        service = null;
        return false;
    }

    public static T Get<T>() where T : class, IGameService
    {
        if (TryGet<T>(out var service))
        {
            return service;
        }
        throw new InvalidOperationException($"ServiceLocator: service of type {typeof(T).Name} has not been registered.");
    }

    public static bool Contains(Type type)
    {
        return _services.ContainsKey(type);
    }

    public static void ShutdownAll()
    {
        if (_services.Count == 0) return;

        // Copy to avoid modification while iterating in case Shutdown destroys objects.
        var values = new List<IGameService>(_services.Values);
        _services.Clear();
        foreach (var service in values)
        {
            try
            {
                service?.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
