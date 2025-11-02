using UnityEngine;

/// <summary>
/// Minimal marker component so pooled instances can be tracked uniquely.
/// </summary>
[DisallowMultipleComponent]
public class PooledRef : MonoBehaviour
{
    // Intentionally empty. TrackGenerator only needs a stable component reference.
}