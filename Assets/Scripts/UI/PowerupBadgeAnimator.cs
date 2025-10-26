using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PowerupBadgeAnimator : MonoBehaviour
{
    [Header("Refs")]
    public Image radialImage;
    public Transform scaleTarget;

    [Header("Timings")]
    public float popInTime = 0.15f;
    public float pulseFreq = 3f;
    public float pulseMag = 0.08f;
    public float lowTimePulseThreshold = 2f;

    float _total;
    float _remaining;
    Vector3 _baseScale;
    bool _active;

    void Awake()
    {
        if (!scaleTarget) scaleTarget = transform;
        if (scaleTarget) _baseScale = scaleTarget.localScale;
        if (radialImage) radialImage.fillAmount = 0f;
        if (scaleTarget) scaleTarget.localScale = Vector3.zero;
    }

    public void OnPowerupStart(float totalSeconds)
    {
        _total = Mathf.Max(0.01f, totalSeconds);
        _remaining = _total;
        _active = true;
        if (radialImage) radialImage.fillAmount = 1f;
        StopAllCoroutines();
        StartCoroutine(PopIn());
    }

    public void OnTick(float remainingSeconds)
    {
        if (!_active) return;
        _remaining = Mathf.Clamp(remainingSeconds, 0f, _total);
        if (radialImage)
        {
            radialImage.fillAmount = (_total <= 0f) ? 0f : (_remaining / _total);
        }

        if (!scaleTarget) return;

        if (_remaining <= lowTimePulseThreshold)
        {
            float s = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f * pulseFreq) * pulseMag;
            scaleTarget.localScale = _baseScale * s;
        }
        else
        {
            scaleTarget.localScale = _baseScale;
        }

        if (_remaining <= 0f)
        {
            OnPowerupEnd();
        }
    }

    public void OnPowerupEnd()
    {
        if (!_active) return;
        _active = false;
        StopAllCoroutines();
        StartCoroutine(PopOut());
    }

    IEnumerator PopIn()
    {
        float t = 0f;
        while (t < popInTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.SmoothStep(0f, 1f, popInTime <= 0f ? 1f : t / popInTime);
            if (scaleTarget)
            {
                scaleTarget.localScale = _baseScale * a;
            }
            yield return null;
        }
        if (scaleTarget)
        {
            scaleTarget.localScale = _baseScale;
        }
    }

    IEnumerator PopOut()
    {
        float t = 0f;
        const float duration = 0.12f;
        Vector3 start = scaleTarget ? scaleTarget.localScale : Vector3.one;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.SmoothStep(0f, 1f, duration <= 0f ? 1f : t / duration);
            if (scaleTarget)
            {
                scaleTarget.localScale = start * a;
            }
            yield return null;
        }
        if (scaleTarget)
        {
            scaleTarget.localScale = Vector3.zero;
        }
        if (radialImage)
        {
            radialImage.fillAmount = 0f;
        }
    }
}
