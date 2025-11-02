using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AdsManager : SingletonServiceBehaviour<AdsManager>
{
    public static AdsManager I => ServiceLocator.TryGet(out AdsManager service) ? service : null;

    [Header("Frequency Control")]
    [SerializeField] int interstitialGateRuns = 2;
    [SerializeField] float interstitialCooldownSec = 120f;

    int _deathRunsSinceInterstitial;
    float _lastInterstitialTime = -999f;
    bool _awaitingInterstitialConfirmation;

    IAdsProvider _provider;
    Action _pendingRewardCallback;

    public AdsConsentPayload CurrentConsent { get; private set; }

    public override void Initialize()
    {
        _deathRunsSinceInterstitial = 0;
        _lastInterstitialTime = -999f;

        _provider = AdsProviderFactory.Create();
        _provider.OnImpression += HandleImpression;
        _provider.OnRewardEarned += HandleRewardEarned;
        _provider.OnShowFailed += HandleShowFailed;
        _provider.Initialize(this);

        // Push persisted consent state down to the SDK.
        ApplyConsent(new AdsConsentPayload(Prefs.Consent, Prefs.AgeRestrictedAdsUser, Prefs.DoNotSellAdsData));
    }

    public override void Shutdown()
    {
        if (_provider != null)
        {
            _provider.OnImpression -= HandleImpression;
            _provider.OnRewardEarned -= HandleRewardEarned;
            _provider.OnShowFailed -= HandleShowFailed;
        }
    }

    public void OnRunEnded()
    {
        _deathRunsSinceInterstitial++;
    }

    public bool TryShowInterstitial(string placement = "end_run")
    {
        if (_provider == null || !_provider.IsInitialized)
        {
            Debug.LogWarning("[Ads] Interstitial requested before provider initialization.");
            return false;
        }

        if (IAPManager.I != null && IAPManager.I.HasRemoveAds) return false;
        if (_deathRunsSinceInterstitial < interstitialGateRuns) return false;
        if (Time.unscaledTime - _lastInterstitialTime < interstitialCooldownSec) return false;

        bool started = _provider.TryShowInterstitial(placement);
        if (started)
        {
            _awaitingInterstitialConfirmation = true;
        }
        else
        {
            Debug.LogWarning("[Ads] Provider rejected interstitial show request (not ready).");
        }

        return started;
    }

    public void ShowRewarded(string placement, Action onReward)
    {
        if (_provider == null || !_provider.IsInitialized)
        {
            Debug.LogWarning("[Ads] Rewarded requested before provider initialization.");
            return;
        }

        if (_pendingRewardCallback != null)
        {
            Debug.LogWarning("[Ads] Rewarded video requested while another reward is still pending.");
            return;
        }

        _pendingRewardCallback = onReward;
        if (!_provider.TryShowRewarded(placement))
        {
            Debug.LogWarning("[Ads] Provider rejected rewarded show request (not ready).");
            _pendingRewardCallback = null;
        }
    }

    public void ApplyConsent(AdsConsent consent, bool isAgeRestrictedUser, bool shouldRestrictDataProcessing)
    {
        Prefs.Consent = consent;
        Prefs.AgeRestrictedAdsUser = isAgeRestrictedUser;
        Prefs.DoNotSellAdsData = shouldRestrictDataProcessing;
        ApplyConsent(new AdsConsentPayload(consent, isAgeRestrictedUser, shouldRestrictDataProcessing));
    }

    void ApplyConsent(AdsConsentPayload payload)
    {
        CurrentConsent = payload;
        _provider?.SetConsent(payload);
    }

    void HandleImpression(string adType, string placement)
    {
        if (adType == "interstitial")
        {
            _deathRunsSinceInterstitial = 0;
            _lastInterstitialTime = Time.unscaledTime;
            _awaitingInterstitialConfirmation = false;
        }

        AnalyticsManager.I?.AdImpression(adType, placement);
    }

    void HandleRewardEarned(string placement)
    {
        if (_pendingRewardCallback == null)
        {
            Debug.LogWarning("[Ads] Reward callback received without a pending reward request.");
        }
        else
        {
            try
            {
                _pendingRewardCallback.Invoke();
            }
            finally
            {
                _pendingRewardCallback = null;
            }
        }

        AnalyticsManager.I?.AdReward(placement);
    }

    void HandleShowFailed(string adType, string reason)
    {
        Debug.LogWarning($"[Ads] {adType} failed to show: {reason}");

        if (adType == "rewarded")
        {
            _pendingRewardCallback = null;
        }
        else if (adType == "interstitial" && _awaitingInterstitialConfirmation)
        {
            _awaitingInterstitialConfirmation = false;
        }
    }
}
