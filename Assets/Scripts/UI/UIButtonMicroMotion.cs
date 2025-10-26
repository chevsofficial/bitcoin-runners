using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonMicroMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.05f;
    public float pressScale = 0.97f;
    public float lerp = 12f;

    Vector3 _base;
    Vector3 _target;

    void Awake()
    {
        _base = transform.localScale;
        _target = _base;
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-lerp * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, _target, k);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _target = _base * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _target = _base;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _target = _base * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _target = _base * hoverScale;
    }
}
