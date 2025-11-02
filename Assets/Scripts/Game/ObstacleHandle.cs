// Assets/Scripts/Game/ObstacleHandle.cs
using UnityEngine;

/// <summary>
/// Lightweight metadata component that identifies obstacle prefabs coming from pools.
/// Ensures the correct tag is applied exactly once per instance.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PooledRef))]
public class ObstacleHandle : MonoBehaviour
{
    public enum ObstacleType
    {
        None = 0,
        Barrier = 1,
        LowBar = 2,
        Gap = 3,
    }

    [SerializeField] ObstacleType type = ObstacleType.None;
    [SerializeField] string obstacleTag = string.Empty;

    public ObstacleType Type => type;
    public string Tag => obstacleTag;

    /// <summary> Obstacles with a type other than None count toward the concurrent limit. </summary>
    public bool BlocksLane => type != ObstacleType.None;

    void Awake()
    {
        ApplyTag();
    }

    void OnValidate()
    {
        ApplyTag();
    }

    public void Configure(ObstacleType newType, string newTag)
    {
        bool changed = type != newType || obstacleTag != newTag;
        if (!changed)
        {
            ApplyTag();
            return;
        }

        type = newType;
        obstacleTag = newTag;
        ApplyTag();
    }

    void ApplyTag()
    {
        if (string.IsNullOrEmpty(obstacleTag)) return;
        if (gameObject.tag != obstacleTag)
        {
            gameObject.tag = obstacleTag;
        }
    }
}
