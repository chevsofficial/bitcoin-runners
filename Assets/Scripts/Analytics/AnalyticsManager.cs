using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager I;
    void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

    public void Log(string name, Dictionary<string, object> p = null)
    {
        string json = p == null ? "{}" : MiniJson.ToJson(p);
        Debug.Log($"[ANALYTICS] {name} {json}");
    }

    // Convenience wrappers
    public void RunStart() => Log("run_start");
    public void RunEnd(int distance, int coins, int score, bool continued) =>
      Log("run_end", new() { { "distance", distance }, { "coins", coins }, { "score", score }, { "continued", continued } });

    public void AdImpression(string type, string placement) =>
      Log("ad_impression", new() { { "type", type }, { "placement", placement } });

    public void AdReward(string placement) =>
      Log("ad_reward", new() { { "placement", placement } });

    public void Purchase(string productId, bool success) =>
      Log("purchase", new() { { "productId", productId }, { "success", success } });

    public void PowerupStart(string type, float duration) =>
      Log("powerup_start", new() { { "type", type }, { "duration", duration } });
}

// Minimal JSON helper (no external deps)
public static class MiniJson
{
    public static string ToJson(Dictionary<string, object> dict)
    {
        System.Text.StringBuilder sb = new();
        sb.Append("{");
        bool first = true;
        foreach (var kv in dict)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append('"').Append(kv.Key).Append('"').Append(":");
            sb.Append(ValueToString(kv.Value));
        }
        sb.Append("}");
        return sb.ToString();
    }
    static string ValueToString(object v)
    {
        if (v == null) return "null";
        if (v is bool b) return b ? "true" : "false";
        if (v is string s) return $"\"{s}\"";
        if (v is int or float or double) return System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
        return $"\"{v}\"";
    }
}
