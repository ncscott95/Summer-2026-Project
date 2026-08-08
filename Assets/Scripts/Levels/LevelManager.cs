using System.Collections.Generic;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    // 0 = Generic, 1 = Timer, 2 = Turn, 3 = Threshold
    [SerializeField] private List<GameObject> _targetPrefabs;

    // 0 = left, 1 = center, 2 = right
    [SerializeField] private List<Transform> _spawnPoints;

    private LevelData _currentLevelData;
    private int _currentTargetIndex = 0;

    public void LoadLevel(LevelData levelData)
    {
        _currentLevelData = levelData;
        StartLevel();
    }

    public void StartLevel()
    {
        _currentTargetIndex = 0;
        TrySpawnNextTarget();
    }

    private void TrySpawnNextTarget()
    {
        if (_currentTargetIndex >= _currentLevelData.LevelTargets.Count)
        {
            Debug.Log("All targets spawned");
            return;
        }

        LevelTargetItem targetItem = _currentLevelData.LevelTargets[_currentTargetIndex];
        SpawnTarget(targetItem);

        _currentTargetIndex++;

        if (_currentTargetIndex >= _currentLevelData.LevelTargets.Count)
        {
            Debug.Log("All targets spawned");
            return;
        }

        // also spawn following target if it is set to spawn with previous
        targetItem = _currentLevelData.LevelTargets[_currentTargetIndex];
        if (targetItem.SpawnType == LevelTargetSpawnType.WithPrevious)
        {
            TrySpawnNextTarget();
        }
    }

    private void SpawnTarget(LevelTargetItem targetItem)
    {
        Debug.Log($"Spawning target of type {targetItem.TargetType} at position {targetItem.SpawnPosition}");

        GameObject prefab = _targetPrefabs[(int)targetItem.TargetType];

        // use spawn position + 1 to adjust -1, 0, 1 to 0, 1, 2 index
        Transform spawnPoint = _spawnPoints[targetItem.SpawnPosition + 1];

        GameObject instance = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        instance.GetComponent<LevelTarget>().Initialize(targetItem);
    }
}
