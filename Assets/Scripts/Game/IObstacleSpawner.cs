public interface IObstacleSpawner
{
    void SetSpawnInterval(float seconds);
    void SetMaxConcurrent(int count);
    void SetMinEmptyLaneChance(float probability);
}
