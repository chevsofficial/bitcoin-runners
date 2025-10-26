// Assets/Scripts/Game/Powerups/PowerupPickup.cs
using UnityEngine;

public class PowerupPickup : MonoBehaviour
{
    public PowerType type = PowerType.Magnet;
    public float duration = 8f; // Dash will use ~3s

    bool _collected; // guard against double-trigger in the same frame

    void OnEnable() { _collected = false; } // reset when reused from pool

    void Update()
    {
        // Simple idle spin so it's visible
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected || !GameManager.I.Alive) return;

        var pu = other.GetComponent<PowerupSystem>();
        if (pu)
        {
            _collected = true;
            pu.Activate(type, duration);
            GameEvents.PowerupPickup(type.ToString());
            AudioManager.I?.PlayLaneWhoosh();
            Haptics.Medium();
            HitStop.I.DoHitStopFrames(6);
            // recycle back to its pool instead of SetActive(false)
            SimplePool.RecycleAny(gameObject);
        }
    }
}
