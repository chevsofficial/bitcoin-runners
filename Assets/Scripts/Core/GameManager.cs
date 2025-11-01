// Assets/Scripts/Core/GameManager.cs
using UnityEngine;

[DefaultExecutionOrder(-100)] // ensure GM initializes before binders/UI
public class GameManager : SingletonServiceBehaviour<GameManager>
{
    public static GameManager I => ServiceLocator.TryGet(out GameManager service) ? service : null;

    [Header("Config")]
    public GameConstants cfg;
    [Header("Difficulty")]
    public DifficultyProfile difficulty;

    [Header("Audio")]
    [Tooltip("Number of speed ramps before switching to the intense music deck (<=0 to disable).")]
    public int intenseRampThreshold = 5;

    public float Speed { get; private set; }
    public float Distance { get; private set; }
    public int Coins { get; private set; }
    public bool Alive { get; private set; } = true;

    float _startTime;
    bool _manualSpeed;
    bool _musicIntense;

    public void SetDistance(float d) => Distance = Mathf.Max(0f, d);

    public override void Initialize()
    {
        ResetRun();
    }

    public override void Shutdown()
    {
    }

    // -------- Run lifecycle --------
    public void ResetRun()
    {
        _manualSpeed = false;
        Speed = cfg ? cfg.startSpeed : 6f;
        Distance = 0;
        Coins = 0;
        Alive = true;
        _startTime = Time.time;
        _musicIntense = false;

        AudioManager.I?.CrossfadeToBase();
    }

    public void OverrideSpeed(float s) { _manualSpeed = true; Speed = s; }

    public void ReleaseSpeedOverride()
    {
        _manualSpeed = false;
        float elapsed = Time.time - _startTime;
        float rampInterval = cfg ? Mathf.Max(0.0001f, cfg.rampEverySec) : 5f;
        int ramps = Mathf.FloorToInt(elapsed / rampInterval);
        Speed = EvaluateAutomaticSpeed(elapsed, ramps);
    }

    void Update()
    {
        if (!Alive) return;

        float rampInterval = cfg ? Mathf.Max(0.0001f, cfg.rampEverySec) : 5f;
        float elapsed = Time.time - _startTime;
        int ramps = Mathf.FloorToInt(elapsed / rampInterval);

        if (!_manualSpeed)
        {
            Speed = EvaluateAutomaticSpeed(elapsed, ramps);
        }

        if (!_musicIntense && intenseRampThreshold > 0 && ramps >= intenseRampThreshold)
        {
            AudioManager.I?.CrossfadeToIntense();
            _musicIntense = true;
        }
        Distance += Speed * Time.deltaTime;
    }

    public void AddCoin(int n = 1) { Coins += n; }

    public void PlayerDied() => KillPlayer();

    public void KillPlayer()
    {
        if (!Alive) return;
        Alive = false;

        AudioManager.I?.CrossfadeToBase();
        _musicIntense = false;

    }

    float EvaluateAutomaticSpeed(float elapsed, int ramps)
    {
        if (difficulty)
        {
            return difficulty.SpeedAt(elapsed);
        }

        float start = cfg ? cfg.startSpeed : 6f;
        float delta = cfg ? cfg.rampDelta : 0.5f;
        float cap = cfg ? cfg.speedCap : 20f;
        return Mathf.Min(cap, start + ramps * delta);
    }
}
