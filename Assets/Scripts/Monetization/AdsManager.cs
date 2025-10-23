using UnityEngine;
using System;
using Debug = UnityEngine.Debug;

public class AdsManager : MonoBehaviour
{
    public static AdsManager I;
    int deathRunsSinceInterstitial = 0;
    float lastInterstitialTime = -999f;
    public int interstitialGateRuns = 2;
    public float interstitialCooldownSec = 120f;

    void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

    public void OnRunEnded()
    {
        deathRunsSinceInterstitial++;
    }

    public bool TryShowInterstitial(string placement = "end_run")
    {
        if (IAPManager.I != null && IAPManager.I.HasRemoveAds) return false;
        if (deathRunsSinceInterstitial < interstitialGateRuns) return false;
        if (Time.unscaledTime - lastInterstitialTime < interstitialCooldownSec) return false;

        deathRunsSinceInterstitial = 0;
        lastInterstitialTime = Time.unscaledTime;

        Debug.Log("[Ads] Interstitial shown (stub)");
        AnalyticsManager.I?.AdImpression("interstitial", placement);
        return true;
    }

    public void ShowRewarded(string placement, Action onReward)
    {
        Debug.Log($"[Ads] Rewarded {placement} (stub showing now)");
        AnalyticsManager.I?.AdImpression("rewarded", placement);
        onReward?.Invoke(); // grant immediately in stub
        AnalyticsManager.I?.AdReward(placement);
    }
}
