using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class IAPManager : MonoBehaviour
{
    public static IAPManager I;
    public bool HasRemoveAds => Prefs.RemoveAds;
    public string removeAdsProductId = "remove_ads"; // placeholder

    void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

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