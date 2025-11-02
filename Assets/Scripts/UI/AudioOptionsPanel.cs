using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsPanel : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;

    [Header("Mute Toggles")]
    [SerializeField] Toggle musicEnabledToggle;
    [SerializeField] Toggle sfxEnabledToggle;

    bool _suppressCallbacks;

    void Awake()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
        }

        if (musicEnabledToggle != null)
        {
            musicEnabledToggle.onValueChanged.AddListener(HandleMusicEnabledChanged);
        }

        if (sfxEnabledToggle != null)
        {
            sfxEnabledToggle.onValueChanged.AddListener(HandleSfxEnabledChanged);
        }
    }

    void OnEnable()
    {
        RefreshFromSettings();
    }

    void OnDestroy()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        }

        if (musicEnabledToggle != null)
        {
            musicEnabledToggle.onValueChanged.RemoveListener(HandleMusicEnabledChanged);
        }

        if (sfxEnabledToggle != null)
        {
            sfxEnabledToggle.onValueChanged.RemoveListener(HandleSfxEnabledChanged);
        }
    }

    void RefreshFromSettings()
    {
        SaveSystem.Load();

        float musicVolume = Mathf.Clamp01(SaveSystem.Data.musicVol);
        float sfxVolume = Mathf.Clamp01(SaveSystem.Data.sfxVol);
        bool musicEnabled = SaveSystem.Data.musicEnabled;
        bool sfxEnabled = SaveSystem.Data.sfxEnabled;

        var audio = AudioManager.I;
        if (audio != null)
        {
            musicVolume = audio.MusicVolume;
            sfxVolume = audio.SfxVolume;
            musicEnabled = audio.MusicEnabled;
            sfxEnabled = audio.SfxEnabled;
        }

        _suppressCallbacks = true;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = Mathf.Lerp(musicVolumeSlider.minValue, musicVolumeSlider.maxValue, musicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = Mathf.Lerp(sfxVolumeSlider.minValue, sfxVolumeSlider.maxValue, sfxVolume);
        }

        if (musicEnabledToggle != null)
        {
            musicEnabledToggle.isOn = musicEnabled;
        }

        if (sfxEnabledToggle != null)
        {
            sfxEnabledToggle.isOn = sfxEnabled;
        }

        _suppressCallbacks = false;
    }

    void HandleMusicVolumeChanged(float value)
    {
        if (_suppressCallbacks) return;

        value = NormalizeSliderValue(musicVolumeSlider, value);
        if (AudioManager.I != null)
        {
            AudioManager.I.SetMusicVolume(value);
        }
        else
        {
            SaveSystem.Load();
            SaveSystem.Data.musicVol = value;
            SaveSystem.Save();
        }
    }

    void HandleSfxVolumeChanged(float value)
    {
        if (_suppressCallbacks) return;

        value = NormalizeSliderValue(sfxVolumeSlider, value);
        if (AudioManager.I != null)
        {
            AudioManager.I.SetSfxVolume(value);
        }
        else
        {
            SaveSystem.Load();
            SaveSystem.Data.sfxVol = value;
            SaveSystem.Save();
        }
    }

    void HandleMusicEnabledChanged(bool value)
    {
        if (_suppressCallbacks) return;

        if (AudioManager.I != null)
        {
            AudioManager.I.SetMusicEnabled(value);
        }
        else
        {
            SaveSystem.Load();
            SaveSystem.Data.musicEnabled = value;
            SaveSystem.Save();
        }
    }

    void HandleSfxEnabledChanged(bool value)
    {
        if (_suppressCallbacks) return;

        if (AudioManager.I != null)
        {
            AudioManager.I.SetSfxEnabled(value);
        }
        else
        {
            SaveSystem.Load();
            SaveSystem.Data.sfxEnabled = value;
            SaveSystem.Save();
        }
    }

    static float NormalizeSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return Mathf.Clamp01(value);
        }

        float min = slider.minValue;
        float max = slider.maxValue;
        if (Mathf.Approximately(max, min))
        {
            return Mathf.Clamp01(value);
        }

        return Mathf.Clamp01(Mathf.InverseLerp(min, max, value));
    }
}
