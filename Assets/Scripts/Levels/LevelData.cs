using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelList
{
    public List<LevelData> Levels = new List<LevelData>();
}

[Serializable]
public class LevelData
{
    public string LevelId;
    public string LevelName;
    public List<LevelTargetItem> LevelTargets;
}

[Serializable]
public class LevelTargetItem
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
    unknown,
    Points,
}

public enum LevelTargetSpawnType
{
    WithPrevious,
    OnPreviousHit,
    OnAllPreviousHit,
}
