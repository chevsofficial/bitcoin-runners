using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int bestScore;
    public int totalSats; // your coin bank if you keep one
    public bool removeAds;
    public float musicVol = 0.8f, sfxVol = 1.0f;
    public bool runHasPendingContinue;
    public float runContinueDistance;
    public bool runX2Consumed;
}

public static class SaveSystem
{
    const string KEY = "save_v1";
    public static SaveData Data { get; private set; } = new SaveData();

    public static void Load()
    {
        if (PlayerPrefs.HasKey(KEY))
        {
            var json = PlayerPrefs.GetString(KEY);
            Data = JsonUtility.FromJson<SaveData>(json);
        }
    }
    public static void Save()
    {
        var json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }
}
