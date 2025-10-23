// Assets/Scripts/FX/CameraShaker.cs
using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    Vector3 _base;
    Coroutine _co;
    void Awake() { _base = transform.localPosition; }
    public void Shake(float dur = 0.25f, float amp = 0.05f)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(DoShake(dur, amp));
    }
    IEnumerator DoShake(float d, float a)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float n1 = Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f;
            float n2 = Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f;
            transform.localPosition = _base + new Vector3(n1 * a, n2 * a, 0f);
            yield return null;
        }
        transform.localPosition = _base;
    }
}
