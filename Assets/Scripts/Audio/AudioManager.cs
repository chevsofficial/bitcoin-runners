// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "audio.music.volume";
    private const string SfxVolumeKey = "audio.sfx.volume";
    private const string MusicEnabledKey = "audio.music.enabled";
    private const string SfxEnabledKey = "audio.sfx.enabled";

    public static AudioManager I { get; private set; }

    [SerializeField] public AudioSource sfx, music;
    [SerializeField] public AudioClip coin, hit, whoosh, musicLoop;

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
    }

    void Start()
    {
        if (music != null && musicLoop != null)
        {
            music.clip = musicLoop;
            music.loop = true;

            if (MusicEnabled)
            {
                music.Play();
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

    public void PlayWhoosh()
    {
        if (!SfxEnabled || sfx == null || whoosh == null) return;
        sfx.PlayOneShot(whoosh, 0.9f * SfxVolume);
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        if (music != null)
        {
            if (MusicEnabled)
            {
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

        PlayerPrefs.SetInt(MusicEnabledKey, MusicEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        PlayerPrefs.SetInt(SfxEnabledKey, SfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        if (music != null)
        {
            music.volume = MusicVolume;
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        if (sfx != null)
        {
            sfx.volume = SfxVolume;
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
    }

    private void ApplySettings()
    {
        if (music != null)
        {
            music.volume = MusicVolume;
            if (!MusicEnabled && music.isPlaying)
            {
                music.Stop();
            }
        }

        if (sfx != null)
        {
            sfx.volume = SfxVolume;
        }
    }
}
