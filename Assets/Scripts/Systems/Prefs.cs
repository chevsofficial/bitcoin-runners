using UnityEngine;

// You can keep this enum here or in its own AdsConsent.cs file.
public enum AdsConsent
{
    Unknown = 0,
    Personalized = 1,
    NonPersonalized = 2,
    NoAds = 3
}

public static class Prefs
{
    const string kRemoveAds = "remove_ads";
    const string kBestScore = "best_score";
    const string kConsent = "ads_consent";
    const string kGdprShown = "gdpr_shown";
    const string kAgeRestricted = "ads_age_restricted";
    const string kDoNotSell = "ads_do_not_sell";

    public static bool RemoveAds
    {
        get => PlayerPrefs.GetInt(kRemoveAds, 0) == 1;
        set { PlayerPrefs.SetInt(kRemoveAds, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static int BestScore
    {
        get => PlayerPrefs.GetInt(kBestScore, 0);
        set
        {
            // Only store if it's higher than the current best
            int current = PlayerPrefs.GetInt(kBestScore, 0);
            if (value > current)
            {
                PlayerPrefs.SetInt(kBestScore, value);
                PlayerPrefs.Save();
            }
        }
    }

    // === New for consent/GDPR ===

    public static AdsConsent Consent
    {
        get => (AdsConsent)PlayerPrefs.GetInt(kConsent, (int)AdsConsent.Unknown);
        set { PlayerPrefs.SetInt(kConsent, (int)value); PlayerPrefs.Save(); }
    }

    public static bool GDPRShown
    {
        get => PlayerPrefs.GetInt(kGdprShown, 0) == 1;
        set { PlayerPrefs.SetInt(kGdprShown, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool AgeRestrictedAdsUser
    {
        get => PlayerPrefs.GetInt(kAgeRestricted, 0) == 1;
        set { PlayerPrefs.SetInt(kAgeRestricted, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool DoNotSellAdsData
    {
        get => PlayerPrefs.GetInt(kDoNotSell, 0) == 1;
        set { PlayerPrefs.SetInt(kDoNotSell, value ? 1 : 0); PlayerPrefs.Save(); }
    }
}
