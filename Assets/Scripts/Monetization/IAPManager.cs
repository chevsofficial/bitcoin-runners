using UnityEngine;
using Debug = UnityEngine.Debug;

public class IAPManager : SingletonServiceBehaviour<IAPManager>
{
    public static IAPManager I => ServiceLocator.TryGet(out IAPManager service) ? service : null;
    public bool HasRemoveAds => Prefs.RemoveAds;
    [Tooltip("Product identifier for the remove-ads entitlement in the live store.")]
    public string removeAdsProductId = "remove_ads";

    [Header("Stub Configuration")]
    [Tooltip("Optional configuration asset that tunes the behaviour of the editor monetization stubs.")]
    [SerializeField] StubMonetizationSettings stubSettings;

    IIAPProvider _provider;

    public override void Initialize()
    {
        _provider = IAPProviderFactory.Create();
        _provider.OnPurchaseSucceeded += HandlePurchaseSucceeded;
        _provider.OnPurchaseFailed += HandlePurchaseFailed;
        _provider.OnRestoreCompleted += HandleRestoreCompleted;
        _provider.Initialize(this, removeAdsProductId);
#if !UNITY_IAP_ENABLED
        if (_provider is StubIapProvider stub)
        {
            stub.ApplySettings(stubSettings != null ? stubSettings : StubMonetizationSettings.LoadDefault());
        }
#endif
    }

    public override void Shutdown()
    {
        if (_provider != null)
        {
            _provider.OnPurchaseSucceeded -= HandlePurchaseSucceeded;
            _provider.OnPurchaseFailed -= HandlePurchaseFailed;
            _provider.OnRestoreCompleted -= HandleRestoreCompleted;
        }
    }

    public void BuyRemoveAds()
    {
        if (_provider == null || !_provider.IsInitialized)
        {
            Debug.LogWarning("[IAP] Purchase requested before provider initialization.");
            return;
        }

        _provider.Purchase(removeAdsProductId);
    }

    public void RestorePurchases()
    {
        if (_provider == null || !_provider.IsInitialized)
        {
            Debug.LogWarning("[IAP] Restore requested before provider initialization.");
            return;
        }

        _provider.RestorePurchases();
    }

    void HandlePurchaseSucceeded(string productId)
    {
        bool removeAds = string.Equals(productId, removeAdsProductId, System.StringComparison.Ordinal);
        if (removeAds)
        {
            Prefs.RemoveAds = true;
        }

        AnalyticsManager.I?.Purchase(productId, true);
        Debug.Log($"[IAP] Purchase succeeded for '{productId}'.");
    }

    void HandlePurchaseFailed(string productId, string reason)
    {
        AnalyticsManager.I?.Purchase(productId, false);
        Debug.LogWarning($"[IAP] Purchase failed for '{productId}': {reason}");
    }

    void HandleRestoreCompleted()
    {
        Debug.Log("[IAP] Restore complete.");
    }
}
