using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class ResultsController : MonoBehaviour
{
    public TMPro.TextMeshProUGUI scoreTxt, coinsTxt, bestTxt;
    public Button replayBtn, homeBtn, continueBtn, x2Btn, removeAdsBtn;

    bool _continuedThisRun;
    bool _x2Consumed;

    void OnEnable()
    {
        // Run after the panel becomes visible; wait one frame for singletons.
        StartCoroutine(RefreshNextFrame());
    }

    System.Collections.IEnumerator RefreshNextFrame()
    {
        // wait one frame to let singletons Awake
        yield return null;

        var gm = GameManager.I;
        if (gm == null)
        {
            Debug.LogError("[Results] GameManager still not found after a frame. Make sure it exists or is DontDestroyOnLoad.");
            yield break;
        }

        int distance = Mathf.FloorToInt(gm.Distance);
        int coins = gm.Coins;
        int score = ScoreSystem.CalcScore();

        if (scoreTxt) scoreTxt.text = score.ToString();
        if (coinsTxt) coinsTxt.text = coins.ToString();

        if (score > Prefs.BestScore) Prefs.BestScore = score;
        if (bestTxt) bestTxt.text = Prefs.BestScore.ToString();
        SaveSystem.Data.bestScore = Prefs.BestScore; SaveSystem.Save();


        // Continue eligibility (≥ checkpoint stride, e.g. 150m)
        float lastCp = RunSession.LastCheckpoint(gm.Distance);
        bool canContinue = lastCp >= RunSession.CheckpointStride;

        if (continueBtn)
        {
            continueBtn.gameObject.SetActive(true);
            continueBtn.interactable = canContinue;
        }

        if (x2Btn) x2Btn.interactable = !_x2Consumed;

        // Disable Remove Ads if already owned
        if (removeAdsBtn)
            removeAdsBtn.interactable = !(IAPManager.I?.HasRemoveAds ?? Prefs.RemoveAds);

        AdsManager.I?.OnRunEnded();
        _ = AdsManager.I?.TryShowInterstitial("results_interstitial");

        AnalyticsManager.I?.RunEnd(distance, coins, score, _continuedThisRun);
    }

    // ---------- Button handlers (required for the OnClick dropdown) ----------

    public void OnReplay()
    {
        SceneManager.LoadScene("Run");
    }

    public void OnHome()
    {
        SceneManager.LoadScene("Menu");
    }

    public void OnContinue()
    {
        float lastCp = RunSession.LastCheckpoint(GameManager.I.Distance);
        if (lastCp < RunSession.CheckpointStride) return;

        AdsManager.I?.ShowRewarded("continue_checkpoint", () =>
        {
            RunSession.I.hasPendingContinue = true;
            RunSession.I.continueDistance = lastCp;
            _continuedThisRun = true;
            SceneManager.LoadScene("Run");
        });
    }

    public void OnX2()
    {
        if (_x2Consumed) return;

        AdsManager.I?.ShowRewarded("x2_sats", () =>
        {
            GameManager.I.AddCoin(GameManager.I.Coins); // double
            _x2Consumed = true;
            if (x2Btn) x2Btn.interactable = false;
            if (scoreTxt) scoreTxt.text = ScoreSystem.CalcScore().ToString();
        });
    }

    public void OnBuyRemoveAds()
    {
        IAPManager.I?.BuyRemoveAds();
        if (removeAdsBtn) removeAdsBtn.interactable = false;
    }
}
