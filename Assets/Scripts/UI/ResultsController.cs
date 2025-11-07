using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ResultsController : MonoBehaviour
{
    [Header("Texts")]
    public TMPro.TextMeshProUGUI scoreTxt;
    public TMPro.TextMeshProUGUI coinsTxt;
    public TMPro.TextMeshProUGUI bestTxt;

    [Header("Buttons")]
    public Button replayBtn;
    public Button homeBtn;
    public Button continueBtn;
    public Button x2Btn;
    public Button removeAdsBtn;

    [Header("Reveal Groups")]
    public GameObject scoreRow;
    public GameObject bestRow;
    public GameObject buttonsGroup;

    [Header("Reveal Timing")]
    public float revealStep = 0.15f;
    public float tallyTime = 0.8f;

    [Header("Audio")]
    public AudioSource tickSource;

    bool _continuedThisRun;
    bool _x2Consumed;
    Coroutine _resultsRoutine;

    public void BeginResultsFlow()
    {
        var session = RunSession.I;
        _x2Consumed = session != null && session.x2GrantedThisResults;

        if (_resultsRoutine != null)
        {
            StopCoroutine(_resultsRoutine);
        }

        _resultsRoutine = StartCoroutine(RunResultsSequence());
    }

    public void EndResultsFlow()
    {
        StopAllCoroutines();
        _resultsRoutine = null;
    }

    IEnumerator RunResultsSequence()
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

        if (score > Prefs.BestScore) Prefs.BestScore = score;

        if (scoreRow) scoreRow.SetActive(false);
        if (bestRow) bestRow.SetActive(false);
        if (buttonsGroup) buttonsGroup.SetActive(false);

        if (coinsTxt) coinsTxt.text = "0";
        if (scoreTxt) scoreTxt.text = string.Empty;
        if (bestTxt) bestTxt.text = string.Empty;

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
        {
            removeAdsBtn.interactable = !(IAPManager.I?.HasRemoveAds ?? Prefs.RemoveAds);
        }

        AdsManager.I?.OnRunEnded();
        _ = AdsManager.I?.TryShowInterstitial("results_interstitial");

        AnalyticsManager.I?.RunEnd(distance, coins, score, _continuedThisRun);

        yield return StartCoroutine(RevealFlow(coins, score, Prefs.BestScore));

        _resultsRoutine = null;
    }

    IEnumerator RevealFlow(int coins, int score, int bestScore)
    {
        if (scoreRow) scoreRow.SetActive(true);
        yield return StartCoroutine(CoinTally(coins, tallyTime));
        if (scoreTxt) scoreTxt.text = score.ToString();

        yield return new WaitForSecondsRealtime(revealStep);

        if (bestRow) bestRow.SetActive(true);
        if (bestTxt) bestTxt.text = bestScore.ToString();

        yield return new WaitForSecondsRealtime(revealStep);

        if (buttonsGroup) buttonsGroup.SetActive(true);
    }

    IEnumerator CoinTally(int target, float time)
    {
        int start = 0;
        float t = 0f;
        float tickInterval = target > 0 ? 1f / Mathf.Max(8, target) : 0f;
        float nextTick = 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            int val = Mathf.RoundToInt(Mathf.Lerp(start, target, a));
            if (coinsTxt) coinsTxt.text = val.ToString();

            if (tickInterval > 0f && t >= nextTick)
            {
                nextTick += tickInterval;
                if (tickSource) tickSource.Play();
            }

            yield return null;
        }

        if (coinsTxt) coinsTxt.text = target.ToString();
    }

    // ---------- Button handlers (required for the OnClick dropdown) ----------

    public void OnReplay()
    {
        RunSession.I?.Clear();
        SceneManager.LoadScene("Run");
    }

    public void OnHome()
    {
        RunSession.I?.Clear();
        SceneManager.LoadScene("Menu");
    }

    public void OnContinue()
    {
        float lastCp = RunSession.LastCheckpoint(GameManager.I.Distance);
        if (lastCp < RunSession.CheckpointStride) return;

        AdsManager.I?.ShowRewarded("continue_checkpoint", () =>
        {
            var session = RunSession.I;
            if (session != null)
            {
                session.hasPendingContinue = true;
                session.continueDistance = lastCp;
                session.PersistState();
            }
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
            var session = RunSession.I;
            if (session != null)
            {
                session.x2GrantedThisResults = true;
                session.PersistState();
            }
            if (x2Btn) x2Btn.interactable = false;
            if (coinsTxt) coinsTxt.text = GameManager.I.Coins.ToString();
            if (scoreTxt) scoreTxt.text = ScoreSystem.CalcScore().ToString();
        });
    }

    public void OnBuyRemoveAds()
    {
        IAPManager.I?.BuyRemoveAds();
        if (removeAdsBtn) removeAdsBtn.interactable = false;
    }
}
