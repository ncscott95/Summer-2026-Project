using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [SerializeField] private TargetSpawner _targetSpawner;

    private LevelData _currentLevelData;

    public void LoadLevel(LevelData levelData)
    {
        _currentLevelData = levelData;
        _targetSpawner.Initialize(_currentLevelData);
        StartLevel();
    }

    public void StartLevel()
    {
        _targetSpawner.SpawnNextTarget();
    }

    public void OnTargetHit(LevelTargetItem targetItem)
    {
        Debug.Log($"Target of type {targetItem.TargetType} hit at position {targetItem.SpawnPosition}");
        _targetSpawner.OnTargetDestroy(targetItem);
        ScoringSystem.Instance.ScoreHit(targetItem);
    }

    public void OnTargetExpire(LevelTargetItem targetItem)
    {
        Debug.Log($"Target of type {targetItem.TargetType} expired at position {targetItem.SpawnPosition}");
        _targetSpawner.OnTargetDestroy(targetItem);
    }
}
