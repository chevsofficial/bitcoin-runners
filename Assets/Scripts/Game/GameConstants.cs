using UnityEngine;

[CreateAssetMenu(menuName = "BR/GameConstants")]
public class GameConstants : ScriptableObject
{
    [Header("Lanes")]
    public float laneWidth = 2.2f;  // meters
    public int laneCount = 3;

    [Header("Speed")]
    public float startSpeed = 6f;
    public float rampEverySec = 5f;
    public float rampDelta = 0.15f;
    public float speedCap = 16f;

    [Header("Runner")]
    public float laneSwitchTime = 0.12f;
    public float jumpAirTime = 0.55f;
    public float slideTime = 0.6f;
    public Vector2 colliderSize = new(0.6f, 1.7f);

    [Header("Scoring")]
    public int coinScore = 10;
    public int nearMissBonus = 5;

    [Header("Generation")]
    public float segmentLen = 20f;
    public float minObstacleSpacingEarly = 8f;
    public float minObstacleSpacingLate = 6f;
}
