using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Data", menuName = "Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    public string Id;
    public string LevelName;
    public List<LevelTargetItem> LevelTargets;
}

[Serializable]
public struct LevelTargetItem
{
    public int Id;
    public LevelTargetType TargetType;
    public LevelTargetSpawnType SpawnType;
    // -1 = west, 0 = center top, 1 = east
    public int SpawnPosition;
    public float PointValue;
    public float ModValue;
}

public enum LevelTargetType
{
    Generic,
    Timer,
    Turn,
    Threshold,
}

public enum LevelTargetSpawnType
{
    WithPrevious,
    OnPreviousHit,
    OnAllPreviousHit,
}
