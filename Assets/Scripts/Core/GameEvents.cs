using System;
using UnityEngine;

public static class GameEvents
{
    // Movement
    public static event Action<int> OnLaneSwap;          // dir: -1 left, +1 right
    public static event Action OnJump;
    public static event Action OnSlide;

    // Collisions / state
    public static event Action OnNearMiss;               // fired by NearMissZone
    public static event Action OnHit;                    // player actually hit obstacle
    public static event Action<string> OnPowerupPickup;  // id "magnet"/"shield"/"dash"/...

    // Scoring / rewards
    public static event Action<int, Vector3> OnNearMissReward; // (amount, worldPos) for popup

    // ---- INVOKERS (call these from your existing systems) ----
    public static void LaneSwap(int dir) => OnLaneSwap?.Invoke(dir);
    public static void Jump() => OnJump?.Invoke();
    public static void Slide() => OnSlide?.Invoke();
    public static void NearMiss() => OnNearMiss?.Invoke();
    public static void Hit() => OnHit?.Invoke();
    public static void PowerupPickup(string id) => OnPowerupPickup?.Invoke(id);
    public static void NearMissReward(int amount, Vector3 worldPos) => OnNearMissReward?.Invoke(amount, worldPos);
}
