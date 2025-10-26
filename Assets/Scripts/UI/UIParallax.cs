using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [System.Serializable]
    public struct Layer
    {
        public RectTransform rt;
        public float strength;
    }

    public Layer[] layers;
    public float maxOffset = 25f;
    public float lerp = 6f;

    Vector2 _target;

    void Update()
    {
        Vector2 p = Input.mousePosition;
        Vector2 view = new Vector2(
            Screen.width > 0 ? (p.x / Screen.width - 0.5f) : 0f,
            Screen.height > 0 ? (p.y / Screen.height - 0.5f) : 0f);
        _target = view * maxOffset;

        float k = 1f - Mathf.Exp(-lerp * Time.unscaledDeltaTime);
        foreach (var layer in layers)
        {
            if (!layer.rt) continue;
            Vector2 want = _target * layer.strength;
            layer.rt.anchoredPosition = Vector2.Lerp(layer.rt.anchoredPosition, want, k);
        }
    }
}
