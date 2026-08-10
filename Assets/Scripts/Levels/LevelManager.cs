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

    // 0 = left, 1 = center, 2 = right
    private LevelTargetItem[] _activeTargets = new LevelTargetItem[3];

    public void LoadLevel(LevelData levelData)
    {
        _currentLevelData = levelData;
        StartLevel();
    }

    public void StartLevel()
    {
        _currentTargetIndex = 0;
        SpawnTarget(_currentLevelData.LevelTargets[_currentTargetIndex]);
    }

    private void SpawnTarget(LevelTargetItem targetItem)
    {
        Debug.Log($"Spawning target of type {targetItem.TargetType} at position {targetItem.SpawnPosition}");

        GameObject prefab = _targetPrefabs[(int)targetItem.TargetType];

        // use spawn position + 1 to adjust -1, 0, 1 to 0, 1, 2 index
        Transform spawnPoint = _spawnPoints[targetItem.SpawnPosition + 1];
        if (_activeTargets[targetItem.SpawnPosition + 1] != null)
        {
            Debug.LogWarning($"Target already active at position {targetItem.SpawnPosition}. Overwriting but not destroying.");
        }
        _activeTargets[targetItem.SpawnPosition + 1] = targetItem;

        GameObject instance = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        instance.GetComponent<LevelTarget>().Initialize(targetItem);

        _currentTargetIndex++;

        if (_currentTargetIndex >= _currentLevelData.LevelTargets.Count)
        {
            Debug.Log("All targets spawned");
            return;
        }

        // also spawn following target if it is set to spawn with previous
        LevelTargetItem nextTargetItem = _currentLevelData.LevelTargets[_currentTargetIndex];
        if (nextTargetItem.SpawnType == LevelTargetSpawnType.WithPrevious)
        {
            SpawnTarget(nextTargetItem);
        }
    }

    public void OnTargetHit(LevelTargetItem targetItem)
    {
        Debug.Log($"Target of type {targetItem.TargetType} hit at position {targetItem.SpawnPosition}");

        // clear the active target at the hit position
        _activeTargets[targetItem.SpawnPosition + 1] = null;

        if (_currentTargetIndex >= _currentLevelData.LevelTargets.Count)
        {
            Debug.Log("All targets spawned");
            return;
        }

        LevelTargetItem nextTargetItem = _currentLevelData.LevelTargets[_currentTargetIndex];

        if (nextTargetItem.SpawnType == LevelTargetSpawnType.OnPreviousHit)
        {
            if (targetItem.Id == nextTargetItem.Id - 1) SpawnTarget(nextTargetItem);
        }
        else if (nextTargetItem.SpawnType == LevelTargetSpawnType.OnAllPreviousHit)
        {
            bool allPreviousHit = true;
            for (int i = 0; i < _activeTargets.Length; i++)
            {
                if (_activeTargets[i] != null)
                {
                    allPreviousHit = false;
                    break;
                }
            }

            if (allPreviousHit) SpawnTarget(nextTargetItem);
        }
    }
}
