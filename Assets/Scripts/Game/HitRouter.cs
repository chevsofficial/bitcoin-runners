// Assets/Scripts/Game/HitRouter.cs
using UnityEngine;
using Debug = UnityEngine.Debug;

public class HitRouter : MonoBehaviour
{
    PowerupSystem _pu;

    void Awake() { _pu = GetComponent<PowerupSystem>(); }

    public void TryKill()
    {
        if (GameManager.I == null || !GameManager.I.Alive) return;
        if (_pu && (_pu.Invulnerable || _pu.TryConsumeShieldHit())) return;
        AudioManager.I?.PlayHit();
        GameManager.I.PlayerDied();
    }
}
