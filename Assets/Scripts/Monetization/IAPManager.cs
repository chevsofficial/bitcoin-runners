using UnityEngine;
using Debug = UnityEngine.Debug;

public class IAPManager : SingletonServiceBehaviour<IAPManager>
{
    public static IAPManager I => ServiceLocator.TryGet(out IAPManager service) ? service : null;
    public bool HasRemoveAds => Prefs.RemoveAds;
    public string removeAdsProductId = "remove_ads"; // placeholder

    public override void Initialize()
    {
        // Nothing asynchronous yet, but this is where store SDK init would live.
    }

    public override void Shutdown()
    {
        // Placeholder for eventual store SDK disposal.
    }

    public void BuyRemoveAds()
    {
        Prefs.RemoveAds = true;
        SaveSystem.Data.removeAds = true; SaveSystem.Save();
        AnalyticsManager.I?.Purchase(removeAdsProductId, true);
        Debug.Log("[IAP] Remove Ads purchased (stub).");
    }

    public void RestorePurchases()
    {
        Debug.Log("[IAP] RestorePurchases called (stub).");
    }
}
