using System.Collections.Generic;
using UnityEngine;

public class LevelDatabase : Singleton<LevelDatabase>
{
    [SerializeField] private List<LevelData> _levels;

    public LevelData GetLevelDataById(string id)
    {
        return _levels.Find(level => level.Id == id);
    }
}
