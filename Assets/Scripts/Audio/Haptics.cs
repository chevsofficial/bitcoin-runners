using UnityEngine;

public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject _vibrator;
    static bool _initialized;

    static void EnsureInit()
    {
        if (_initialized) return;
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        _initialized = true;
    }

    static void VibrateAndroid(long ms, int amplitude /*1-255*/)
    {
        EnsureInit();
        if (_vibrator == null) return;
        var sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
        if (sdk >= 26)
        {
            var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
            _vibrator.Call("vibrate", effect);
        }
        else
        {
            _vibrator.Call("vibrate", ms);
        }
    }
#endif

    public static void Light()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(15, 64);
#else
        Handheld.Vibrate();
#endif
    }

    public static void Medium()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(25, 160);
#else
        Handheld.Vibrate();
#endif
    }

    public static void Heavy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(40, 255);
#else
        Handheld.Vibrate();
#endif
    }
}
