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
    [System.Serializable]
    public struct PoolDiagnostics
    {
        public int available;
        public int borrowed;
        public int totalCreated;
        public int totalBorrowed;
        public int totalRecycled;
        public int exhaustionEvents;
    }

    public GameObject prefab;
    public int initial = 10;

    [Header("Diagnostics")]
    [SerializeField, Tooltip("Log a warning when the pool has to allocate because it ran dry.")]
    bool warnOnExhaustion = true;
    [SerializeField, Tooltip("Minimum seconds between exhaustion warnings to avoid log spam.")]
    float exhaustionWarningCooldown = 5f;
    [SerializeField, Tooltip("Runtime counters surfaced for QA/diagnostics.")]
    PoolDiagnostics diagnostics;

    readonly Queue<GameObject> q = new();

    float lastExhaustionWarnTime = -999f;

    public PoolDiagnostics Diagnostics => diagnostics;

    void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError($"[SimplePool] Prefab not set on {name}");
            return;
        }

        Prefill(initial);
    }

    public void Prefill(int desired)
    {
        if (prefab == null)
        {
            Debug.LogError($"[SimplePool] Prefab not set on {name}");
            return;
        }

        desired = Mathf.Max(0, desired);

        while (q.Count < desired)
        {
            var go = Instantiate(prefab, transform);
            AttachOwner(go);
            go.SetActive(false);
            q.Enqueue(go);
            diagnostics.totalCreated++;
        }

        diagnostics.available = q.Count;
    }

    public GameObject Get()
    {
        if (prefab == null)
        {
            Debug.LogError($"[SimplePool] Prefab not set on {name}");
            return null;
        }

        GameObject go;
        if (q.Count > 0)
        {
            go = q.Dequeue();
            diagnostics.available = q.Count;
        }
        else
        {
            diagnostics.exhaustionEvents++;
            int borrowedAfter = diagnostics.borrowed + 1;
            MaybeWarnExhausted(borrowedAfter);
            go = Instantiate(prefab, transform);
            diagnostics.totalCreated++;
        }

        diagnostics.borrowed++;
        diagnostics.totalBorrowed++;

        AttachOwner(go);
        go.SetActive(true);
        return go;
    }

    // Instance recycle (return this object to THIS pool)
    public void Recycle(GameObject go)
    {
        if (go == null) return;

        var pr = go.GetComponent<PooledRef>();
        if (pr && pr.owner != this)
        {
            // Hand the object back to its actual owner if we somehow received a foreign instance.
            pr.owner?.Recycle(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        q.Enqueue(go);

        diagnostics.available = q.Count;
        diagnostics.totalRecycled++;
        if (diagnostics.borrowed > 0) diagnostics.borrowed--;
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
            if (ShouldValidateLifecycle)
            {
                Debug.LogWarning($"[SimplePool] Attempted to recycle '{go.name}' but it has no owning pool; destroying.", go);
            }
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

    private class PoolDelayHost : MonoBehaviour
    {
        void OnDestroy()
        {
            if (sharedDelayHost == this)
            {
                sharedDelayHost = null;
            }
        }
    }

    static bool ShouldValidateLifecycle
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }

    void MaybeWarnExhausted(int borrowedAfter)
    {
        if (!warnOnExhaustion || !ShouldValidateLifecycle) return;

        float now = Time.unscaledTime;
        if (now - lastExhaustionWarnTime < Mathf.Max(0.1f, exhaustionWarningCooldown)) return;

        lastExhaustionWarnTime = now;
        Debug.LogWarning($"[SimplePool] Pool '{name}' exhausted; instantiating an extra instance. Consider increasing the initial size (active instances after request: {borrowedAfter}).", this);
    }
}
