using UnityEngine;
using TMPro;

public class LevelSelectButton : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _levelNameText;

    private LevelData _levelData;

    public void Initialize(LevelData levelData)
    {
        _levelData = levelData;

        if (_levelData == null)
        {
            Debug.LogError("Level data is null. Cannot initialize button.");
            return;
        }

        _levelNameText.text = _levelData.LevelName;
    }

    public void OnButtonClick()
    {
        GameManager.Instance.StartGameplayLevel(_levelData);
    }
}
