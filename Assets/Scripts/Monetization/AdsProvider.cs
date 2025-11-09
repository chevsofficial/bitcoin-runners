using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Defines the contract for connecting an ad mediation SDK to the game.
/// Implementations can wrap IronSource/LevelPlay, Unity Ads, or any other
/// production-ready provider.  The default implementation in this repository
/// is an editor-friendly stub so the game keeps working without the SDK
/// present, but the hooks required by the live services are all exposed.
/// </summary>
public interface IAdsProvider
{
    bool IsInitialized { get; }
    event Action<string, string> OnImpression; // (adType, placement)
    event Action<string> OnRewardEarned;       // placement
    event Action<string> OnRewardClosed;       // placement
    event Action<string, string> OnShowFailed; // (adType, reason)

    void Initialize(MonoBehaviour host);
    void SetConsent(AdsConsentPayload payload);
    bool TryShowInterstitial(string placement);
    bool TryShowRewarded(string placement);
}

/// <summary>
/// Data container that lets us pass the complete consent state to mediation SDKs.
/// Some SDKs expect multiple toggles (GDPR/CCPA/age gate) alongside a high-level
/// consent value; bundling them keeps the APIs tidy.
/// </summary>
public readonly struct AdsConsentPayload
{
    public readonly AdsConsent Consent;
    public readonly bool IsAgeRestrictedUser;
    public readonly bool ShouldRestrictDataProcessing;

    public AdsConsentPayload(AdsConsent consent, bool isAgeRestrictedUser, bool shouldRestrictDataProcessing)
    {
        Consent = consent;
        IsAgeRestrictedUser = isAgeRestrictedUser;
        ShouldRestrictDataProcessing = shouldRestrictDataProcessing;
    }

    public override string ToString()
    {
        return $"Consent={Consent}, AgeRestricted={IsAgeRestrictedUser}, DoNotSell={ShouldRestrictDataProcessing}";
    }
}

/// <summary>
/// Factory that chooses the correct provider based on compilation symbols.
/// When integrating an SDK, define a symbol (for example LEVELPLAY_ENABLED)
/// and return an implementation that wraps the native SDK APIs.
/// </summary>
public static class AdsProviderFactory
{
    public static IAdsProvider Create()
    {
#if LEVELPLAY_ENABLED
        return new LevelPlayAdsProvider();
#elif IRONSOURCE_ENABLED
        return new IronSourceAdsProvider();
#else
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        throw new InvalidOperationException("Stub ads provider selected for a release build. Enable a production-ready mediation SDK.");
#else
        return new StubAdsProvider();
#endif
#endif
    }
}

#if LEVELPLAY_ENABLED
// Example placeholder implementation.  Fill this out once the LevelPlay/IronSource
// SDK is imported.  Having the class present keeps compile errors away when the
// scripting define symbol is toggled on in production builds.
internal sealed class LevelPlayAdsProvider : BaseIronSourceProvider { }
#endif

#if IRONSOURCE_ENABLED
// Same idea as above – inherit from a shared wrapper that encapsulates
// IronSource callbacks.  See BaseIronSourceProvider for the actual wiring.
internal sealed class IronSourceAdsProvider : BaseIronSourceProvider { }

internal abstract class BaseIronSourceProvider : IAdsProvider
{
    public bool IsInitialized { get; private set; }
    public event Action<string, string> OnImpression;
    public event Action<string> OnRewardEarned;
    public event Action<string> OnRewardClosed;
    public event Action<string, string> OnShowFailed;

    public void Initialize(MonoBehaviour host)
    {
        if (IsInitialized) return;
        // Real implementation goes here – the structure is provided so the
        // production SDK can be dropped in without touching gameplay code.
        //IronSource.Agent.validateIntegration();
        //IronSource.Agent.init(appKey);
        IsInitialized = true;
    }

    public void SetConsent(AdsConsentPayload payload)
    {
        //IronSource.Agent.setConsent(payload.Consent == AdsConsent.Personalized);
        //IronSource.Agent.setMetaData("is_child_directed", payload.IsAgeRestrictedUser ? "true" : "false");
        //IronSource.Agent.setMetaData("do_not_sell", payload.ShouldRestrictDataProcessing ? "true" : "false");
    }

    public bool TryShowInterstitial(string placement)
    {
        //if (!IronSource.Agent.isInterstitialReady()) return false;
        //IronSource.Agent.showInterstitial(placement);
        return true;
    }

    public bool TryShowRewarded(string placement)
    {
        //if (!IronSource.Agent.isRewardedVideoAvailable()) return false;
        //IronSource.Agent.showRewardedVideo(placement);
        return true;
    }

    // Call these from the IronSource callbacks (e.g. OnImpressionSuccessEvent)
    protected void RaiseImpression(string adType, string placement) => OnImpression?.Invoke(adType, placement);
    protected void RaiseReward(string placement) => OnRewardEarned?.Invoke(placement);
    protected void RaiseRewardClosed(string placement) => OnRewardClosed?.Invoke(placement);
    protected void RaiseShowFailed(string adType, string reason) => OnShowFailed?.Invoke(adType, reason);
}
#endif

/// <summary>
/// Editor/CI friendly provider.  It mimics the API surface of a live SDK while
/// keeping the project functional when third-party packages are absent.
/// </summary>
internal sealed class StubAdsProvider : IAdsProvider
{
    MonoBehaviour _host;
    StubMonetizationSettings _settings;

    public bool IsInitialized { get; private set; }
    public event Action<string, string> OnImpression;
    public event Action<string> OnRewardEarned;
    public event Action<string> OnRewardClosed;
    public event Action<string, string> OnShowFailed;

    public void Initialize(MonoBehaviour host)
    {
        _host = host;
        IsInitialized = true;
        Debug.Log("[Ads] Stub provider initialized. Import a mediation SDK and enable its scripting define symbol for production builds.");
    }

    public void SetConsent(AdsConsentPayload payload)
    {
        Debug.Log($"[Ads] Stub provider received consent update: {payload}");
    }

    public bool TryShowInterstitial(string placement)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[Ads] Interstitial requested before initialization.");
            return false;
        }

        if (_settings == null)
        {
            Debug.Log($"[Ads] (Stub) Interstitial shown at placement '{placement}'.");
            OnImpression?.Invoke("interstitial", placement);
            return true;
        }

        if (_settings.requireAttAuthorization && !_settings.attAuthorizationGranted)
        {
            Debug.LogWarning("[Ads] (Stub) Interstitial blocked until ATT authorization is granted.");
            OnShowFailed?.Invoke("interstitial", "ATT authorization missing");
            return false;
        }

        if (!_settings.interstitialsReady)
        {
            Debug.LogWarning("[Ads] (Stub) Interstitial requested while availability flag is disabled.");
            return false;
        }

        _host.StartCoroutine(SimulateInterstitialFlow(placement));
        return true;
    }

    public bool TryShowRewarded(string placement)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[Ads] Rewarded requested before initialization.");
            return false;
        }

        if (_settings == null)
        {
            _host.StartCoroutine(SimulateRewardedFlow(placement));
            return true;
        }

        if (_settings.requireAttAuthorization && !_settings.attAuthorizationGranted)
        {
            Debug.LogWarning("[Ads] (Stub) Rewarded blocked until ATT authorization is granted.");
            OnShowFailed?.Invoke("rewarded", "ATT authorization missing");
            return false;
        }

        if (!_settings.rewardedVideosReady)
        {
            Debug.LogWarning("[Ads] (Stub) Rewarded requested while availability flag is disabled.");
            return false;
        }

        _host.StartCoroutine(SimulateRewardedFlow(placement));
        return true;
    }

    public void ApplySettings(StubMonetizationSettings settings)
    {
        _settings = settings;
        if (_settings == null)
        {
            Debug.Log("[Ads] Stub settings cleared – using default behaviour.");
        }
        else
        {
            Debug.Log("[Ads] Stub settings applied – use the asset to simulate live SDK flows.");
        }
    }

    IEnumerator SimulateInterstitialFlow(string placement)
    {
        float delay = Mathf.Max(0f, _settings?.simulatedShowDelay ?? 0f);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (_settings != null && _settings.forceInterstitialFailure)
        {
            Debug.LogWarning("[Ads] (Stub) Interstitial show failed (forced by configuration).");
            OnShowFailed?.Invoke("interstitial", "Simulated failure");
            yield break;
        }

        Debug.Log($"[Ads] (Stub) Interstitial shown at placement '{placement}'.");
        OnImpression?.Invoke("interstitial", placement);
    }

    IEnumerator SimulateRewardedFlow(string placement)
    {
        float showDelay = Mathf.Max(0f, _settings?.simulatedShowDelay ?? 0f);
        if (showDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(showDelay);
        }

        if (_settings != null && _settings.forceRewardFailure)
        {
            Debug.LogWarning("[Ads] (Stub) Rewarded show failed (forced by configuration).");
            OnShowFailed?.Invoke("rewarded", _settings.forcedRewardFailureReason);
            if (_settings.autoCloseReward)
            {
                OnRewardClosed?.Invoke(placement);
            }
            yield break;
        }

        OnImpression?.Invoke("rewarded", placement);

        float rewardDelay = Mathf.Max(0f, _settings?.simulatedRewardDelay ?? 0f);
        if (rewardDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(rewardDelay);
        }

        Debug.Log($"[Ads] (Stub) Rewarded flow finished for placement '{placement}'.");
        if (_settings == null || _settings.autoGrantReward)
        {
            OnRewardEarned?.Invoke(placement);
        }

        if (_settings == null || _settings.autoCloseReward)
        {
            OnRewardClosed?.Invoke(placement);
        }
    }
}
