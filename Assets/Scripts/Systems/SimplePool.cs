using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine;

/// <summary> Lives on pooled instances; points back to the pool that owns it. </summary>
public class PooledRef : MonoBehaviour
{
    public SimplePool owner;
}

public class SimplePool : MonoBehaviour
{
    public GameObject prefab;
    public int initial = 10;

    private readonly Queue<GameObject> q = new();

    void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError($"[SimplePool] Prefab not set on {name}");
            return;
        }

        for (int i = 0; i < initial; i++)
        {
            var go = Instantiate(prefab, transform);
            AttachOwner(go);
            go.SetActive(false);
            q.Enqueue(go);
        }
    }

    public GameObject Get()
    {
        GameObject go = q.Count > 0 ? q.Dequeue() : Instantiate(prefab, transform);
        AttachOwner(go);
        go.SetActive(true);
        return go;
    }

    // Instance recycle (return this object to THIS pool)
    public void Recycle(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        q.Enqueue(go);
    }

    /// <summary>
    /// Static helper: recycle any pooled object back to its owning pool.
    /// Use this when you don't have a direct reference to the pool.
    /// </summary>
    public static void RecycleAny(GameObject go)
    {
        if (go == null) return;

        var pr = go.GetComponent<PooledRef>();
        if (pr == null || pr.owner == null)
        {
            // Not a pooled instance; just destroy quietly.
            UnityEngine.Object.Destroy(go);
            return;
        }

        pr.owner.Recycle(go);
    }

    private void AttachOwner(GameObject go)
    {
        var pr = go.GetComponent<PooledRef>();
        if (pr == null) pr = go.AddComponent<PooledRef>();
        pr.owner = this;
    }

    // Utility: get or instantiate a one-shot FX that isn't prepooled
    public static GameObject GetOrInstantiate(GameObject prefab)
    {
        if (!prefab) return null;
        // If the prefab itself has a PooledRef with an owner, get from that pool
        var pr = prefab.GetComponent<PooledRef>();
        if (pr && pr.owner) return pr.owner.Get();
        // else, just instantiate transiently
        return UnityEngine.Object.Instantiate(prefab);
    }

    // Utility: recycle/destroy after delay for transient FX
    public static void RecycleAfter(GameObject go, float delay)
    {
        if (!go) return;

        var runner = go.GetComponent<MonoBehaviour>() ?? (MonoBehaviour)SharedDelayHost;
        runner.StartCoroutine(_RecycleLater(go, delay));
    }

    static System.Collections.IEnumerator _RecycleLater(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        RecycleAny(go);
    }

    private static PoolDelayHost SharedDelayHost
    {
        get
        {
            if (!sharedDelayHost)
            {
                var hostGo = new GameObject("[PoolDelayHost]")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                UnityEngine.Object.DontDestroyOnLoad(hostGo);
                sharedDelayHost = hostGo.AddComponent<PoolDelayHost>();
            }

            return sharedDelayHost;
        }
    }

    private static PoolDelayHost sharedDelayHost;

    private class PoolDelayHost : MonoBehaviour {}
}
