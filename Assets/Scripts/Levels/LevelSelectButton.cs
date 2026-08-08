using UnityEngine;
using TMPro;

public class LevelSelectButton : MonoBehaviour
{
    public string LevelIdToLoad;

    [SerializeField] private TextMeshProUGUI _levelNameText;

    private LevelData _levelData;

    void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        _levelData = LevelDatabase.Instance.GetLevelDataById(LevelIdToLoad);

        if (_levelData == null)
        {
            Debug.LogError($"Level with ID {LevelIdToLoad} not found in the database.");
            return;
        }

        _levelNameText.text = _levelData.LevelName;
    }

    public void LoadLevel()
    {
        LevelData levelData = LevelDatabase.Instance.GetLevelDataById(LevelIdToLoad);
        if (levelData != null)
        {
            LevelManager.Instance.LoadLevel(levelData);
        }
        else
        {
            Debug.LogError($"Level with ID {LevelIdToLoad} not found in the database.");
        }
    }
}
