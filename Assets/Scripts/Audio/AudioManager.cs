// Assets/Scripts/Audio/AudioManager.cs
using System.Collections;
using UnityEngine;

public class AudioManager : SingletonServiceBehaviour<AudioManager>
{
    public static AudioManager I => ServiceLocator.TryGet(out AudioManager service) ? service : null;

    [SerializeField] public AudioSource sfx, music;        // existing
    [SerializeField] public AudioSource sfxLoop;           // NEW: for magnet chain loop bed (Loop = ON)
    [SerializeField] public AudioSource musicB;            // NEW: intense music deck
    [SerializeField] public AudioClip coin, hit, whoosh, musicLoop; // existing
    // NEW clips:
    [SerializeField] public AudioClip slideThunk;          // slide under low bar
    [SerializeField] public AudioClip coinChainStart;      // magnet start flourish
    [SerializeField] public AudioClip coinChainLoop;       // loopable soft bed
    [SerializeField] public AudioClip coinChainEnd;        // magnet end flourish
    [SerializeField] public AudioClip intenseLoop;         // intense music loop
    [Header("Tuning")]
    [SerializeField] public float musicFadeSec = 1.25f;
    [SerializeField] public float chainTimeout = 0.25f;

    private float _lastCoinTime;
    private bool _chainActive;

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;

    public override void Initialize()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        LoadSettings();
        ApplySettings();
        ConfigureMusicDecks();
    }

    public override void Shutdown()
    {
        StopAllCoroutines();
        EndCoinChain();

        if (music != null && music.isPlaying)
        {
            music.Stop();
        }

        if (musicB != null && musicB.isPlaying)
        {
            musicB.Stop();
        }

        if (sfxLoop != null && sfxLoop.isPlaying)
        {
            sfxLoop.Stop();
        }
    }

    void ConfigureMusicDecks()
    {
        if (music != null)
        {
            music.clip = musicLoop;
            music.loop = true;
            music.volume = MusicEnabled ? MusicVolume : 0f;

            if (MusicEnabled && music.clip != null && !music.isPlaying)
            {
                music.Play();
            }
            else if (!MusicEnabled && music.isPlaying)
            {
                music.Stop();
            }
        }

        if (musicB != null)
        {
            musicB.loop = true;
            musicB.volume = 0f;

            if (!MusicEnabled && musicB.isPlaying)
            {
                musicB.Stop();
            }
        }
    }

    public void PlayCoin()
    {
        if (!SfxEnabled || sfx == null || coin == null) return;
        sfx.PlayOneShot(coin, 0.9f * SfxVolume);
    }

    public void PlayHit()
    {
        if (!SfxEnabled || sfx == null || hit == null) return;
        sfx.PlayOneShot(hit, 1.0f * SfxVolume);
    }

    public void PlayLaneWhoosh()
    {
        if (!SfxEnabled || sfx == null || whoosh == null) return;
        sfx.PlayOneShot(whoosh, 0.9f * SfxVolume);
    }

    [System.Obsolete("Use PlayLaneWhoosh instead.")]
    public void PlayWhoosh() => PlayLaneWhoosh();

    public void PlaySlideThunk()
    {
        if (!SfxEnabled || sfx == null || slideThunk == null) return;
        sfx.PlayOneShot(slideThunk, 1.0f * SfxVolume);
    }

    // --- Magnet chain SFX (simple version) ---
    // Call this once per coin sucked by magnet.
    public void PlayCoinChainTick()
    {
        if (!SfxEnabled)
        {
            EndCoinChain();
            return;
        }

        if (!_chainActive)
        {
            if (sfx != null && coinChainStart != null)
            {
                sfx.PlayOneShot(coinChainStart, 0.9f * SfxVolume);
            }

            if (sfxLoop != null && coinChainLoop != null)
            {
                sfxLoop.clip = coinChainLoop;
                sfxLoop.loop = true;
                sfxLoop.volume = 0.5f * SfxVolume;
                if (!sfxLoop.isPlaying)
                {
                    sfxLoop.Play();
                }
            }

            _chainActive = true;
        }

        _lastCoinTime = Time.time;
    }

    public void EndCoinChain()
    {
        if (!_chainActive) return;

        if (sfxLoop != null && sfxLoop.isPlaying)
        {
            sfxLoop.Stop();
        }

        if (SfxEnabled && sfx != null && coinChainEnd != null)
        {
            sfx.PlayOneShot(coinChainEnd, 0.9f * SfxVolume);
        }

        _chainActive = false;
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        if (music != null)
        {
            if (MusicEnabled)
            {
                music.volume = MusicVolume;
                if (!music.isPlaying && music.clip != null)
                {
                    music.Play();
                }
            }
            else
            {
                music.Stop();
            }
        }

        if (musicB != null)
        {
            if (MusicEnabled)
            {
                musicB.volume = Mathf.Clamp(musicB.volume, 0f, MusicVolume);
                if (musicB.clip != null && !musicB.isPlaying && musicB.volume > 0f)
                {
                    musicB.Play();
                }
            }
            else
            {
                musicB.Stop();
            }
        }

        if (!MusicEnabled)
        {
            StopAllCoroutines();
            if (music != null)
            {
                music.volume = 0f;
            }
            if (musicB != null)
            {
                musicB.volume = 0f;
            }
        }

        SaveSystem.Load();
        SaveSystem.Data.musicEnabled = MusicEnabled;
        SaveSystem.Save();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        if (!SfxEnabled)
        {
            EndCoinChain();
            if (sfxLoop != null && sfxLoop.isPlaying)
            {
                sfxLoop.Stop();
            }
        }
        SaveSystem.Load();
        SaveSystem.Data.sfxEnabled = SfxEnabled;
        SaveSystem.Save();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        if (music != null)
        {
            music.volume = MusicVolume;
        }

        if (musicB != null)
        {
            if (musicB.isPlaying)
            {
                musicB.volume = MusicVolume;
            }
            else
            {
                musicB.volume = Mathf.Clamp(musicB.volume, 0f, MusicVolume);
            }
        }

        SaveSystem.Load();
        SaveSystem.Data.musicVol = MusicVolume;
        SaveSystem.Save();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        if (sfx != null)
        {
            sfx.volume = SfxVolume;
        }

        if (sfxLoop != null)
        {
            sfxLoop.volume = 0.5f * SfxVolume;
        }

        SaveSystem.Load();
        SaveSystem.Data.sfxVol = SfxVolume;
        SaveSystem.Save();
    }

    void Update()
    {
        if (!_chainActive)
        {
            return;
        }

        if (Time.time - _lastCoinTime > chainTimeout)
        {
            EndCoinChain();
            return;
        }

        if (sfxLoop == null || !sfxLoop.isPlaying)
        {
            EndCoinChain();
        }
    }

    // --- Two-deck music crossfades (simple) ---
    public void CrossfadeToIntense()
    {
        if (!MusicEnabled || musicB == null || intenseLoop == null) return;

        if (!musicB.isPlaying)
        {
            musicB.clip = intenseLoop;
            musicB.loop = true;
            musicB.volume = 0f;
            musicB.Play();
        }

        StopAllCoroutines();
        StartCoroutine(FadeCo(music, 0f, musicFadeSec));
        StartCoroutine(FadeCo(musicB, MusicVolume, musicFadeSec));
    }

    public void CrossfadeToBase()
    {
        if (!MusicEnabled || music == null || musicLoop == null) return;

        if (!music.isPlaying)
        {
            music.clip = musicLoop;
            music.loop = true;
            music.volume = 0f;
            music.Play();
        }

        StopAllCoroutines();
        if (musicB != null)
        {
            StartCoroutine(FadeCo(musicB, 0f, musicFadeSec));
        }

        StartCoroutine(FadeCo(music, MusicVolume, musicFadeSec));
    }

    IEnumerator FadeCo(AudioSource src, float to, float dur)
    {
        if (src == null) yield break;

        if (dur <= 0f)
        {
            src.volume = to;
            if (to <= 0f && src == musicB)
            {
                src.Stop();
            }
            yield break;
        }

        float from = src.volume;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }

        src.volume = to;

        if (to <= 0f && src == musicB)
        {
            src.Stop();
        }
    }

    private void LoadSettings()
    {
        SaveSystem.Load();
        MusicVolume = Mathf.Clamp01(SaveSystem.Data.musicVol);
        SfxVolume = Mathf.Clamp01(SaveSystem.Data.sfxVol);
        MusicEnabled = SaveSystem.Data.musicEnabled;
        SfxEnabled = SaveSystem.Data.sfxEnabled;
    }

    private void ApplySettings()
    {
        if (music != null)
        {
            music.volume = MusicEnabled ? MusicVolume : 0f;
            if (!MusicEnabled && music.isPlaying)
            {
                music.Stop();
            }
        }

        if (musicB != null)
        {
            musicB.volume = 0f;
            if (!MusicEnabled && musicB.isPlaying)
            {
                musicB.Stop();
            }
        }

        if (sfx != null)
        {
            sfx.volume = SfxVolume;
        }

        if (sfxLoop != null)
        {
            sfxLoop.volume = 0.5f * SfxVolume;
            if (!SfxEnabled && sfxLoop.isPlaying)
            {
                sfxLoop.Stop();
            }
        }
    }
}
