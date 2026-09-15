using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    // spawn delay to prevent targets from immediately spawning after previous target is hit
    private const float SpawnDelay = 0.5f;

    // 0 = Generic, 1 = Timer, 2 = Turn, 3 = Threshold
    [SerializeField] private List<GameObject> _targetPrefabs;

    // 0 = left, 1 = center, 2 = right
    [SerializeField] private List<Transform> _spawnPoints;

    private LevelData _currentLevelData;
    private int _currentTargetIndex = 0;

    // 0 = left, 1 = center, 2 = right
    private LevelTargetItem[] _activeTargets = new LevelTargetItem[3];

    public void Initialize(LevelData levelData)
    {
        _currentLevelData = levelData;
        _currentTargetIndex = 0;
    }

    public void SpawnNextTarget()
    {
        SpawnTarget(_currentLevelData.LevelTargets[_currentTargetIndex]);
    }

    public void OnTargetDestroy(LevelTargetItem targetItem)
    {
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

    private void SpawnTarget(LevelTargetItem targetItem)
    {
        StartCoroutine(InstantiateTargetWithDelay(targetItem, SpawnDelay));
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

    private IEnumerator InstantiateTargetWithDelay(LevelTargetItem targetItem, float delay)
    {
        yield return new WaitForSeconds(delay);

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
        // scale the target based on its spawn position (-1 = west, 0 = center, 1 = east)
        // effectively means targets cannot spawn in center, as scale would be 0
        instance.transform.localScale = new Vector3(targetItem.SpawnPosition, 1f, 1f);
        instance.GetComponent<LevelTarget>().Initialize(targetItem);
    }
}
