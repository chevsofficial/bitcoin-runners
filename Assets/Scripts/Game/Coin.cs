// Assets/Scripts/Game/Coin.cs
using UnityEngine;

public class Coin : MonoBehaviour
{
    bool _collected;

    void OnEnable() { _collected = false; }

    void OnTriggerEnter(Collider other)
    {
        if (_collected || !GameManager.I.Alive) return;

        if (other.GetComponent<RunnerController>())
        {
            _collected = true;
            GameManager.I.AddCoin(1);
            AudioManager.I?.PlayCoin();  // <-- SFX
            // recycle instead of SetActive(false)
            SimplePool.RecycleAny(gameObject);
        }
    }
}
