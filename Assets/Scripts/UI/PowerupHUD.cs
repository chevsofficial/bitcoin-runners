// Assets/Scripts/UI/PowerupHUD.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(Image))]
public class PowerupHUD : MonoBehaviour
{
    [Header("UI")]
    public Image icon;                // drag Badge (Image) here
    public TextMeshProUGUI timeTxt;   // drag TimeText here

    [Header("Animation")]
    public PowerupBadgeAnimator badgeAnimator;

    [Header("Sprites")]
    public Sprite magnetSprite;
    public Sprite shieldSprite;
    public Sprite dashSprite;

    PowerupSystem _pu;

    void Start()
    {
        // find the single PowerupSystem (on your Runner)
        _pu = FindFirstObjectByType<PowerupSystem>();
        if (_pu != null)
        {
            _pu.OnPowerupStart += OnStartPU;
            _pu.OnPowerupTick += OnTickPU;
            _pu.OnPowerupEnd += OnEndPU;
        }
        if (!badgeAnimator)
        {
            badgeAnimator = GetComponentInChildren<PowerupBadgeAnimator>();
        }
        // start hidden until a power-up activates
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_pu != null)
        {
            _pu.OnPowerupStart -= OnStartPU;
            _pu.OnPowerupTick -= OnTickPU;
            _pu.OnPowerupEnd -= OnEndPU;
        }
    }

    void OnStartPU(PowerType t)
    {
        icon.sprite = t == PowerType.Magnet ? magnetSprite
                    : t == PowerType.Shield ? shieldSprite
                    : dashSprite;
        if (timeTxt)
        {
            timeTxt.text = Mathf.CeilToInt(_pu != null ? _pu.ActiveDuration : 0f).ToString();
        }

        if (badgeAnimator && _pu != null)
        {
            badgeAnimator.OnPowerupStart(_pu.ActiveDuration);
        }

        gameObject.SetActive(true);
    }

    void OnTickPU(PowerType t, float timeLeft)
    {
        if (timeTxt)
        {
            timeTxt.text = Mathf.CeilToInt(timeLeft).ToString();
        }

        badgeAnimator?.OnTick(timeLeft);
    }

    void OnEndPU(PowerType t)
    {
        badgeAnimator?.OnPowerupEnd();
        gameObject.SetActive(false);
    }
}
