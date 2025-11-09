using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI distanceTxt, coinsTxt, scoreTxt;
    private bool _missingBindingWarningLogged;
    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || distanceTxt == null || coinsTxt == null || scoreTxt == null)
        {
            if (!_missingBindingWarningLogged)
            {
                _missingBindingWarningLogged = true;

                var missing = new System.Collections.Generic.List<string>();
                if (gm == null) missing.Add("GameManager.I");
                if (distanceTxt == null) missing.Add(nameof(distanceTxt));
                if (coinsTxt == null) missing.Add(nameof(coinsTxt));
                if (scoreTxt == null) missing.Add(nameof(scoreTxt));

                Debug.LogWarning($"ScoreSystem missing bindings: {string.Join(", ", missing)}", this);
            }
            return;
        }

        if (_missingBindingWarningLogged)
        {
            _missingBindingWarningLogged = false;
        }

        distanceTxt.text = Mathf.FloorToInt(gm.Distance).ToString();
        coinsTxt.text = gm.Coins.ToString();
        scoreTxt.text = CalcScore().ToString();
    }

    public static int CalcScore()
    {
        var gm = GameManager.I;
        if (gm == null) return 0;

        // Use centralized tuning from GameConstants; safe fallback if cfg isnt set.
        int coinValue = (gm.cfg != null) ? gm.cfg.coinScore : 10;

        return Mathf.FloorToInt(gm.Distance) + gm.Coins * coinValue;
    }
}
