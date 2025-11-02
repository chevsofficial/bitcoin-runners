using System;
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
    public bool IsInitialized { get; private set; }
    public event Action<string> OnPurchaseSucceeded;
    public event Action<string, string> OnPurchaseFailed;
    public event Action OnRestoreCompleted;

    public void Initialize(MonoBehaviour host, params string[] productIds)
    {
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

        Debug.Log($"[IAP] (Stub) Purchase succeeded for product '{productId}'.");
        OnPurchaseSucceeded?.Invoke(productId);
    }

    public void RestorePurchases()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[IAP] Restore requested before initialization.");
            return;
        }

        Debug.Log("[IAP] (Stub) Restore complete.");
        OnRestoreCompleted?.Invoke();
    }
}
