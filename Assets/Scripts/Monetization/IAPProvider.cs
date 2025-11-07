using System;
using System.Collections;
using UnityEngine;

public interface IIAPProvider
{
    bool IsInitialized { get; }
    event Action<string> OnPurchaseSucceeded; // productId
    event Action<string, string> OnPurchaseFailed; // productId, reason
    event Action OnRestoreCompleted;

    void Initialize(MonoBehaviour host, params string[] productIds);
    void Purchase(string productId);
    void RestorePurchases();
}

public static class IAPProviderFactory
{
    public static IIAPProvider Create()
    {
#if UNITY_IAP_ENABLED
        return new UnityIapProvider();
#else
        return new StubIapProvider();
#endif
    }
}

#if UNITY_IAP_ENABLED
// Placeholder that allows you to hook up Unity IAP without touching gameplay
// code.  Fill out the implementation after importing the Unity IAP package and
// enabling the UNITY_IAP_ENABLED scripting define symbol.
internal sealed class UnityIapProvider : IIAPProvider
{
    public bool IsInitialized { get; private set; }
    public event Action<string> OnPurchaseSucceeded;
    public event Action<string, string> OnPurchaseFailed;
    public event Action OnRestoreCompleted;

    public void Initialize(MonoBehaviour host, params string[] productIds)
    {
        if (IsInitialized) return;
        // Initialization code goes here – configure builder, add products, etc.
        IsInitialized = true;
    }

    public void Purchase(string productId)
    {
        if (!IsInitialized)
        {
            OnPurchaseFailed?.Invoke(productId, "IAP provider not initialized");
            return;
        }
        // Start purchase flow (Unity Purchasing's InitiatePurchase, etc.)
    }

    public void RestorePurchases()
    {
        if (!IsInitialized)
        {
            OnPurchaseFailed?.Invoke(string.Empty, "IAP provider not initialized");
            return;
        }
        // Trigger restoration (Unity Purchasing's RestoreTransactions, etc.)
    }
}
#endif

internal sealed class StubIapProvider : IIAPProvider
{
    MonoBehaviour _host;
    StubMonetizationSettings _settings;

    public bool IsInitialized { get; private set; }
    public event Action<string> OnPurchaseSucceeded;
    public event Action<string, string> OnPurchaseFailed;
    public event Action OnRestoreCompleted;

    public void Initialize(MonoBehaviour host, params string[] productIds)
    {
        _host = host;
        IsInitialized = true;
        Debug.Log("[IAP] Stub provider initialized. Import Unity IAP (or another SDK) and enable UNITY_IAP_ENABLED for production builds.");
    }

    public void Purchase(string productId)
    {
        if (!IsInitialized)
        {
            OnPurchaseFailed?.Invoke(productId, "IAP provider not initialized");
            return;
        }

        if (_settings != null && _settings.forcePurchaseFailure)
        {
            Debug.LogWarning($"[IAP] (Stub) Purchase forced to fail for '{productId}'.");
            _host.StartCoroutine(RaisePurchaseFailed(productId, _settings.forcedPurchaseFailureReason));
            return;
        }

        _host.StartCoroutine(RaisePurchaseSucceeded(productId));
    }

    public void RestorePurchases()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[IAP] Restore requested before initialization.");
            return;
        }

        if (_settings != null && _settings.forceRestoreFailure)
        {
            Debug.LogWarning("[IAP] (Stub) Restore forced to fail.");
            _host.StartCoroutine(RaiseRestoreFailed(_settings.forcedRestoreFailureReason));
            return;
        }

        _host.StartCoroutine(RaiseRestoreCompleted());
    }

    public void ApplySettings(StubMonetizationSettings settings)
    {
        _settings = settings;
        if (_settings == null)
        {
            Debug.Log("[IAP] Stub settings cleared – using default behaviour.");
        }
        else
        {
            Debug.Log("[IAP] Stub settings applied – use the asset to simulate live purchasing flows.");
        }
    }

    IEnumerator RaisePurchaseSucceeded(string productId)
    {
        float delay = Mathf.Max(0f, _settings?.simulatedPurchaseDelay ?? 0f);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        Debug.Log($"[IAP] (Stub) Purchase succeeded for product '{productId}'.");
        OnPurchaseSucceeded?.Invoke(productId);
    }

    IEnumerator RaisePurchaseFailed(string productId, string reason)
    {
        float delay = Mathf.Max(0f, _settings?.simulatedPurchaseDelay ?? 0f);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        OnPurchaseFailed?.Invoke(productId, reason);
    }

    IEnumerator RaiseRestoreCompleted()
    {
        float delay = Mathf.Max(0f, _settings?.simulatedPurchaseDelay ?? 0f);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        Debug.Log("[IAP] (Stub) Restore complete.");
        OnRestoreCompleted?.Invoke();
    }

    IEnumerator RaiseRestoreFailed(string reason)
    {
        float delay = Mathf.Max(0f, _settings?.simulatedPurchaseDelay ?? 0f);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        OnPurchaseFailed?.Invoke(string.Empty, reason);
    }
}
