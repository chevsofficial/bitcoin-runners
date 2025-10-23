using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class MenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button playBtn;
    public Button privacyBtn;
    public Button removeAdsBtn;

    [Header("GDPR Panel")]
    public GameObject gdprPanel;
    public Button personalizedBtn;
    public Button nonPersonalizedBtn;
    public Button noAdsBtn;

    [Header("Config")]
    [Tooltip("Your hosted privacy policy URL.")]
    public string privacyURL = "https://yourdomain.com/privacy";

    void Awake()
    {
        // Wire buttons safely (if assigned)
        if (playBtn) playBtn.onClick.AddListener(OnPlay);
        if (privacyBtn) privacyBtn.onClick.AddListener(OpenPrivacy);
        if (removeAdsBtn)
        {
            removeAdsBtn.onClick.AddListener(OnBuyRemoveAds);
            // Disable if already purchased
            removeAdsBtn.interactable = !Prefs.RemoveAds;
        }

        // GDPR choices
        if (personalizedBtn) personalizedBtn.onClick.AddListener(() => ChooseConsent(AdsConsent.Personalized));
        if (nonPersonalizedBtn) nonPersonalizedBtn.onClick.AddListener(() => ChooseConsent(AdsConsent.NonPersonalized));
        if (noAdsBtn) noAdsBtn.onClick.AddListener(() => ChooseConsent(AdsConsent.NoAds));
    }

    void Start()
    {
        // MVP behavior: show GDPR on first run (or show always if you prefer)
        bool shouldShow = !Prefs.GDPRShown || Prefs.Consent == AdsConsent.Unknown;

        if (gdprPanel) gdprPanel.SetActive(shouldShow);
        // Disable Play until a choice? Optional:
        if (playBtn && gdprPanel && shouldShow) playBtn.interactable = true; // allow play regardless in MVP
    }

    // ---- Buttons ----

    public void OnPlay()
    {
        SceneManager.LoadScene("Run");
    }

    public void OpenPrivacy()
    {
        UnityEngine.Application.OpenURL(privacyURL);
    }

    public void OnBuyRemoveAds()
    {
        IAPManager.I?.BuyRemoveAds();
        if (removeAdsBtn) removeAdsBtn.interactable = false;
    }

    // ---- GDPR ----

    void ChooseConsent(AdsConsent choice)
    {
        Prefs.Consent = choice;
        Prefs.GDPRShown = true;

        // Optional: inform your AdsManager (if you add support later)
        // AdsManager.I?.SetPersonalization(choice == AdsConsent.Personalized);
        // If NoAds chosen, respect it in your AdsManager by never showing ads:
        // if (choice == AdsConsent.NoAds) Prefs.RemoveAds = true;

        if (gdprPanel) gdprPanel.SetActive(false);
        Debug.Log("[Consent] Choice: " + choice);
    }
}
