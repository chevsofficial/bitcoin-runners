// Assets/Scripts/Game/Powerups/PowerupSystem.cs
using UnityEngine;
using System.Collections;

public enum PowerType { None, Magnet, Shield, Dash }

public class PowerupSystem : MonoBehaviour
{
    public float magnetRadius = 4f;
    public LayerMask coinMask;
    public float dashSpeedMultiplier = 1.5f;

    public PowerType Active { get; private set; }
    float _timeLeft;
    RunnerController _runner;
    float _dashEndSpeed;

    public System.Action<PowerType, float> OnPowerupTick; // for UI
    public System.Action<PowerType> OnPowerupStart, OnPowerupEnd;

    void Awake() { _runner = GetComponent<RunnerController>(); }

    void Update()
    {
        if (Active == PowerType.None) return;
        _timeLeft -= Time.deltaTime;
        if (Active == PowerType.Magnet)
        {
            if (Time.frameCount % 3 == 0) CollectNearbyCoins();
        }
        OnPowerupTick?.Invoke(Active, Mathf.Max(0f, _timeLeft));
        if (_timeLeft <= 0f) EndPowerup();
    }

    public void Activate(PowerType type, float duration)
    {
        if (Active != PowerType.None) EndPowerup();
        Active = type;
        _timeLeft = duration;
        if (type == PowerType.Dash)
        {
            _dashEndSpeed = GameManager.I.Speed;
            // temporary boost (clamped by speedCap later)
            GameManager.I.OverrideSpeed(Mathf.Min(GameManager.I.cfg.speedCap, _dashEndSpeed * dashSpeedMultiplier));
            Invulnerable = true;
        }
        else if (type == PowerType.Shield)
        {
            Invulnerable = true;
        }
        OnPowerupStart?.Invoke(type);
        AnalyticsManager.I?.PowerupStart(type.ToString(), duration);
        AudioManager.I?.PlayWhoosh();
    }

    void EndPowerup()
    {
        if (Active == PowerType.Dash)
        {
            // let ramp logic take back over next frame; no need to hard reset
            Invulnerable = false;
        }
        else if (Active == PowerType.Shield)
        {
            Invulnerable = false;
        }
        var ended = Active;
        Active = PowerType.None;
        _timeLeft = 0f;
        OnPowerupEnd?.Invoke(ended);
    }

    void CollectNearbyCoins()
    {
        var hits = Physics.OverlapSphere(transform.position, magnetRadius, coinMask);
        foreach (var h in hits)
        {
            var coin = h.GetComponent<Coin>();
                    if (coin && h.gameObject.activeSelf)
                    {
                        // Let the coin handle SFX/FX and recycle itself (pooled-safe)
            coin.SendMessage("OnTriggerEnter", GetComponent<Collider>(), SendMessageOptions.DontRequireReceiver);
                        // Or, if you implemented a public Collect(): coin.Collect();
                    }
        }
    }

    public bool Invulnerable { get; private set; }
    public bool TryConsumeShieldHit()
    {
        if (Active == PowerType.Shield)
        {
            EndPowerup();
            return true;
        }
        return false;
    }
}
