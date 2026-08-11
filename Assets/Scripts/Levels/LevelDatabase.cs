using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelDatabase : Singleton<LevelDatabase>
{
    // 0 = temp, 1 = wood, 2 = fire, 3 = metal, 4 = water, 5 = earth
    private LevelList[] _levelLists = new LevelList[6];
    private List<string> _regionIds = new List<string>(){ "Temp", "Wood", "Fire", "Metal", "Water", "Earth" };

    public override void Awake()
    {
        base.Awake();

        LoadData();
    }

    private void LoadData()
    {
        for (int i = 0; i < _regionIds.Count; i++)
        {
            string regionId = _regionIds[i];
            TextAsset jsonData = Resources.Load<TextAsset>($"{regionId}");
            if (jsonData != null)
            {
                LevelList levelList = JsonUtility.FromJson<LevelList>(jsonData.text);
                _levelLists[i] = levelList;
            }
            else
            {
                Debug.LogWarning($"No level data found for region: {regionId}");
            }
        }
    }

    public LevelData GetLevelDataById(string id)
    {
        string[] strings = id.Split('_');
        if (strings.Length < 2)
        {
            Debug.LogError($"Invalid level ID format: {id}");
            return null;
        }

        int regionIndex = _regionIds.IndexOf(strings[0]);
        if (regionIndex == -1)
        {
            Debug.LogError($"Unknown region in level ID: {id}");
            return null;
        }

        int levelIndex = int.TryParse(strings[1], out int index) ? index - 1 : -1;
        if (levelIndex < 0)
        {
            Debug.LogError($"Invalid level index in level ID: {id}");
            return null;
        }
        
        return _levelLists[regionIndex].Levels[levelIndex];
    }
}
