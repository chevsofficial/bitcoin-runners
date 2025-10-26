using UnityEngine;
using TMPro;

public class TextJitterOnValueChange : MonoBehaviour
{
    public TMP_Text tmp;
    public float popScale = 1.15f;
    public float duration = 0.06f;

    string _lastText;
    Vector3 _baseScale;
    float _t;

    void Awake()
    {
        if (!tmp) tmp = GetComponent<TMP_Text>();
        _baseScale = transform.localScale;
        _lastText = tmp ? tmp.text : string.Empty;
    }

    void LateUpdate()
    {
        if (!tmp) return;
        if (_lastText != tmp.text)
        {
            _lastText = tmp.text;
            _t = duration;
            transform.localScale = _baseScale * popScale;
        }

        if (_t > 0f)
        {
            _t -= Time.unscaledDeltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(1f - (_t / duration));
            transform.localScale = Vector3.Lerp(_baseScale * popScale, _baseScale, a);
        }
    }
}
