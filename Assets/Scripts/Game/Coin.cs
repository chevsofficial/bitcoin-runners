// Assets/Scripts/Game/Coin.cs
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("FX")]
    public GameObject sparklePrefab;    // assign CoinFX_Sparkle
    public Transform visualRoot;        // assign the mesh root (scales on pop)
    public float popScale = 1.25f;
    public float popTime = 0.08f;

    bool _collected;

    void Reset()
    {
        // Try to auto-bind
        if (!visualRoot) visualRoot = transform;
    }

    void OnEnable() { _collected = false; if (visualRoot) visualRoot.localScale = Vector3.one; }

    void OnTriggerEnter(Collider other)
    {
        if (_collected || !GameManager.I.Alive) return;
        if (other.GetComponent<RunnerController>())
        {
            Collect();
        }
    }

    // Called by magnet path too
    public void Collect(bool viaMagnet = false)
    {
        if (_collected) return;
        _collected = true;

        GameManager.I.AddCoin(1);
        if (viaMagnet)
        {
            AudioManager.I?.PlayCoinChainTick();
        }
        else
        {
            AudioManager.I?.PlayCoin();
        }

        if (sparklePrefab)
        {
            var fx = SimplePool.GetOrInstantiate(sparklePrefab);
            fx.transform.position = transform.position + Vector3.up * 0.1f;
            SimplePool.RecycleAfter(fx, 0.5f);
        }

        if (visualRoot) StartCoroutine(DoPop());

        // Recycle this coin instance
        SimplePool.RecycleAny(gameObject);
    }

    System.Collections.IEnumerator DoPop()
    {
        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float k = 1f + (popScale - 1f) * (t / popTime);
            visualRoot.localScale = new Vector3(k, k, k);
            yield return null;
        }
    }
}
