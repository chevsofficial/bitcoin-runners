using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI distanceTxt, coinsTxt, scoreTxt;
    void Update()
    {
        distanceTxt.text = Mathf.FloorToInt(GameManager.I.Distance).ToString();
        coinsTxt.text = GameManager.I.Coins.ToString();
        scoreTxt.text = CalcScore().ToString();
    }
    public static int CalcScore()
    {
        var gm = GameManager.I;
        if (gm == null) return 0;

        // Use centralized tuning from GameConstants; safe fallback if cfg isn’t set.
        int coinValue = (gm.cfg != null) ? gm.cfg.coinScore : 10;

        return Mathf.FloorToInt(gm.Distance) + gm.Coins * coinValue;
    }
}
