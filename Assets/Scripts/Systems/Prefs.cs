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
        get
        {
            SaveSystem.Load();
            return SaveSystem.Data.removeAds;
        }
        set
        {
            SaveSystem.Load();
            if (SaveSystem.Data.removeAds == value) return;
            SaveSystem.Data.removeAds = value;
            SaveSystem.Save();
        }
    }

    public static int BestScore
    {
        get
        {
            SaveSystem.Load();
            return SaveSystem.Data.bestScore;
        }
        set
        {
            SaveSystem.Load();
            if (value > SaveSystem.Data.bestScore)
            {
                SaveSystem.Data.bestScore = value;
                SaveSystem.Save();
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
