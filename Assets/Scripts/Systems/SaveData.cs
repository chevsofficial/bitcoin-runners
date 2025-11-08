using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SaveVersions
{
    public const int Unknown = 0;
    public const int Initial = 1;
    public const int AudioSettings = 2;
    public const int Current = AudioSettings;
}

[Serializable]
public class SaveData
{
    public int version = SaveVersions.Current;
    public int bestScore;
    public int totalSats; // your coin bank if you keep one
    public bool removeAds;
    public float musicVol = 0.8f, sfxVol = 1.0f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    public bool runHasPendingContinue;
    public float runContinueDistance;
    public float runContinueElapsed;
    public int runContinueCoins;
    public bool runX2Consumed;

    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            version = SaveVersions.Current,
            musicVol = 0.8f,
            sfxVol = 1.0f,
            musicEnabled = true,
            sfxEnabled = true
        };
    }
}

public interface ISaveStorage
{
    bool TryLoad(out string serializedData);
    void Save(string serializedData);
}

sealed class PlayerPrefsSaveStorage : ISaveStorage
{
    readonly string _key;
    readonly string _backupKey;

    public PlayerPrefsSaveStorage(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key must be provided", nameof(key));
        _key = key;
        _backupKey = key + "_backup";
    }

    public bool TryLoad(out string serializedData)
    {
        if (TryReadKey(_key, out serializedData))
        {
            return true;
        }

        if (TryReadKey(_backupKey, out serializedData))
        {
            // Restore the backup so it becomes the active save.
            PlayerPrefs.SetString(_key, serializedData);
            PlayerPrefs.Save();
            return true;
        }

        serializedData = null;
        return false;
    }

    static bool TryReadKey(string key, out string serializedData)
    {
        if (PlayerPrefs.HasKey(key))
        {
            serializedData = PlayerPrefs.GetString(key, string.Empty);
            return !string.IsNullOrEmpty(serializedData);
        }

        serializedData = null;
        return false;
    }

    public void Save(string serializedData)
    {
        if (serializedData == null) throw new ArgumentNullException(nameof(serializedData));

        if (PlayerPrefs.HasKey(_key))
        {
            var current = PlayerPrefs.GetString(_key, string.Empty);
            if (!string.IsNullOrEmpty(current))
            {
                PlayerPrefs.SetString(_backupKey, current);
            }
        }

        PlayerPrefs.SetString(_key, serializedData);
        PlayerPrefs.Save();
    }
}

public interface ISaveDataMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SaveData Migrate(SaveData data);
}

sealed class LegacyPlayerPrefsMigration : ISaveDataMigration
{
    const string kRemoveAds = "remove_ads";
    const string kBestScore = "best_score";

    public int FromVersion => SaveVersions.Unknown;
    public int ToVersion => SaveVersions.Initial;

    public SaveData Migrate(SaveData data)
    {
        var migrated = data ?? SaveData.CreateDefault();

        if (PlayerPrefs.HasKey(kBestScore))
        {
            int legacyBest = PlayerPrefs.GetInt(kBestScore, migrated.bestScore);
            migrated.bestScore = Mathf.Max(migrated.bestScore, legacyBest);
        }

        if (PlayerPrefs.HasKey(kRemoveAds))
        {
            bool legacyRemoveAds = PlayerPrefs.GetInt(kRemoveAds, migrated.removeAds ? 1 : 0) == 1;
            migrated.removeAds |= legacyRemoveAds;
        }

        migrated.version = ToVersion;
        return migrated;
    }
}

sealed class AudioSettingsMigration : ISaveDataMigration
{
    const string kMusicVolume = "audio.music.volume";
    const string kSfxVolume = "audio.sfx.volume";
    const string kMusicEnabled = "audio.music.enabled";
    const string kSfxEnabled = "audio.sfx.enabled";

    public int FromVersion => SaveVersions.Initial;
    public int ToVersion => SaveVersions.AudioSettings;

    public SaveData Migrate(SaveData data)
    {
        // Start from defaults so newly introduced audio flags keep their intended values
        // when they are absent in legacy serialized data.
        var migrated = SaveData.CreateDefault();

        if (data != null)
        {
            migrated.bestScore = data.bestScore;
            migrated.totalSats = data.totalSats;
            migrated.removeAds = data.removeAds;
            migrated.runHasPendingContinue = data.runHasPendingContinue;
            migrated.runContinueDistance = data.runContinueDistance;
            migrated.runContinueElapsed = data.runContinueElapsed;
            migrated.runContinueCoins = data.runContinueCoins;
            migrated.runX2Consumed = data.runX2Consumed;
            migrated.musicVol = Mathf.Clamp01(data.musicVol);
            migrated.sfxVol = Mathf.Clamp01(data.sfxVol);
        }

        if (PlayerPrefs.HasKey(kMusicVolume))
        {
            migrated.musicVol = Mathf.Clamp01(PlayerPrefs.GetFloat(kMusicVolume, migrated.musicVol));
        }

        if (PlayerPrefs.HasKey(kSfxVolume))
        {
            migrated.sfxVol = Mathf.Clamp01(PlayerPrefs.GetFloat(kSfxVolume, migrated.sfxVol));
        }

        if (PlayerPrefs.HasKey(kMusicEnabled))
        {
            migrated.musicEnabled = PlayerPrefs.GetInt(kMusicEnabled, migrated.musicEnabled ? 1 : 0) == 1;
        }

        if (PlayerPrefs.HasKey(kSfxEnabled))
        {
            migrated.sfxEnabled = PlayerPrefs.GetInt(kSfxEnabled, migrated.sfxEnabled ? 1 : 0) == 1;
        }

        migrated.version = ToVersion;
        return migrated;
    }
}

public static class SaveSystem
{
    const string KEY = "save_v1";
    public static SaveData Data { get; private set; } = SaveData.CreateDefault();

    static bool _loaded;
    static ISaveStorage _storage = new PlayerPrefsSaveStorage(KEY);
    static readonly List<ISaveDataMigration> _migrations = new List<ISaveDataMigration>
    {
        new LegacyPlayerPrefsMigration(),
        new AudioSettingsMigration()
    };

    public static void SetStorage(ISaveStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _loaded = false;
    }

    public static void SetMigrations(IEnumerable<ISaveDataMigration> migrations)
    {
        _migrations.Clear();
        if (migrations != null)
        {
            _migrations.AddRange(migrations.OrderBy(m => m.FromVersion));
        }

        if (_migrations.Count == 0)
        {
            _migrations.Add(new LegacyPlayerPrefsMigration());
            _migrations.Add(new AudioSettingsMigration());
        }

        _loaded = false;
    }

    public static void Load()
    {
        if (_loaded)
        {
            return;
        }

        SaveData loadedData = null;

        if (_storage.TryLoad(out var json))
        {
            try
            {
                loadedData = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to deserialize save data: {ex.Message}. Falling back to defaults.");
            }
        }

        Data = loadedData ?? SaveData.CreateDefault();
        ApplyMigrations();

        if (Data.version > SaveVersions.Current)
        {
            Debug.LogWarning($"[SaveSystem] Loaded save data from newer version {Data.version}. Proceeding without downgrading.");
        }
        else if (Data.version != SaveVersions.Current)
        {
            Data.version = SaveVersions.Current;
        }

        _loaded = true;
    }

    public static void Save()
    {
        EnsureLoaded();
        if (Data.version < SaveVersions.Current)
        {
            Data.version = SaveVersions.Current;
        }
        else if (Data.version > SaveVersions.Current)
        {
            Debug.LogWarning($"[SaveSystem] Save data version {Data.version} exceeds supported version {SaveVersions.Current}. Keeping higher version tag.");
        }

        try
        {
            var json = JsonUtility.ToJson(Data);
            _storage.Save(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to save data: {ex.Message}");
        }
    }

    static void EnsureLoaded()
    {
        if (!_loaded)
        {
            Load();
        }
    }

    static void ApplyMigrations()
    {
        int guard = 0;

        while (Data != null && Data.version < SaveVersions.Current)
        {
            var migration = _migrations.FirstOrDefault(m => m.FromVersion == Data.version);
            if (migration == null)
            {
                Debug.LogWarning($"[SaveSystem] Missing migration for save version {Data.version}. Resetting to defaults.");
                Data = SaveData.CreateDefault();
                break;
            }

            Data = migration.Migrate(Data) ?? SaveData.CreateDefault();

            guard++;
            if (guard > 16)
            {
                Debug.LogWarning("[SaveSystem] Migration guard triggered. Resetting save data to defaults.");
                Data = SaveData.CreateDefault();
                break;
            }
        }
    }
}
