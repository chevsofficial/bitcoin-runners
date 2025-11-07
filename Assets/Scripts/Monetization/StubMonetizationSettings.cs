using UnityEngine;

/// <summary>
/// Shared configuration for the monetization stub implementations.
/// Attach the generated asset to <see cref="AdsManager"/> and <see cref="IAPManager"/>
/// to mirror the behaviour of live SDKs while the project still relies on
/// placeholder providers.
/// </summary>
[CreateAssetMenu(menuName = "Monetization/Stub Monetization Settings", fileName = "StubMonetizationSettings")]
public sealed class StubMonetizationSettings : ScriptableObject
{
    [Header("Ads Simulation")]
    [Tooltip("Require ATT authorization before ads are considered available.")]
    public bool requireAttAuthorization;

    [Tooltip("Current simulated ATT authorization state. Ignored when requireAttAuthorization is false.")]
    public bool attAuthorizationGranted = true;

    [Tooltip("Return false from TryShowInterstitial/TryShowRewarded when availability is disabled.")]
    public bool interstitialsReady = true;

    [Tooltip("Return false from TryShowRewarded when availability is disabled.")]
    public bool rewardedVideosReady = true;

    [Tooltip("When true the stub will report a failed show attempt for interstitial placements.")]
    public bool forceInterstitialFailure;

    [Tooltip("When true the stub will report a failed show attempt for rewarded placements.")]
    public bool forceRewardFailure;

    [Tooltip("Text used when a rewarded placement reports failure.")]
    public string forcedRewardFailureReason = "Simulated reward failure";

    [Tooltip("Delay (in seconds) before the stub reports that an ad started showing.")]
    public float simulatedShowDelay = 0.25f;

    [Tooltip("Delay (in seconds) between an ad impression and reward/cancellation callbacks.")]
    public float simulatedRewardDelay = 0.75f;

    [Tooltip("Automatically trigger the reward callback once a rewarded ad finishes.")]
    public bool autoGrantReward = true;

    [Tooltip("Automatically trigger the rewarded close callback after the reward phase.")]
    public bool autoCloseReward = true;

    [Header("IAP Simulation")]
    [Tooltip("When true purchase attempts will report a simulated failure.")]
    public bool forcePurchaseFailure;

    [Tooltip("Reason string emitted when forcePurchaseFailure is enabled.")]
    public string forcedPurchaseFailureReason = "Simulated purchase failure";

    [Tooltip("When true restore requests will report a simulated failure.")]
    public bool forceRestoreFailure;

    [Tooltip("Reason string emitted when forceRestoreFailure is enabled.")]
    public string forcedRestoreFailureReason = "Simulated restore failure";

    [Tooltip("Delay (in seconds) before purchase or restore callbacks are raised.")]
    public float simulatedPurchaseDelay = 0.25f;

    /// <summary>
    /// Loads the shared settings from Resources/Monetization/StubMonetizationSettings if one exists.
    /// Falls back to a transient instance so gameplay code can still read defaults in editor/test builds.
    /// </summary>
    public static StubMonetizationSettings LoadDefault()
    {
        var settings = Resources.Load<StubMonetizationSettings>("Monetization/StubMonetizationSettings");
        if (settings == null)
        {
            settings = CreateInstance<StubMonetizationSettings>();
        }

        return settings;
    }
}
