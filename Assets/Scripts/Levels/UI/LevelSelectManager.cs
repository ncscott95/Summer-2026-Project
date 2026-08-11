using UnityEngine;
using TMPro;

public class LevelSelectManager : Singleton<LevelSelectManager>
{
    [SerializeField] private Transform _levelButtonContainer;
    [SerializeField] private GameObject _levelButtonPrefab;
    [SerializeField] private TextMeshProUGUI _regionTitleText;

    private LevelRegion _currentRegion = LevelRegion.Temp;

    void Start()
    {
        // TODO: eventually, Temp will be used as an empty state and does not show any levels
        _currentRegion = LevelRegion.Temp;
        UpdateLevelButtons();
    }

    public void SelectRegion(LevelRegion region)
    {
        _currentRegion = region;
        UpdateLevelButtons();
    }

    private void UpdateLevelButtons()
    {
        LevelList levelList = LevelDatabase.Instance.GetLevelListByRegion(_currentRegion);
        
        foreach (Transform child in _levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < levelList.Levels.Count; i++)
        {
            GameObject newButton = Instantiate(_levelButtonPrefab, _levelButtonContainer);
            newButton.GetComponent<LevelSelectButton>().Initialize(levelList.Levels[i]);
        }

        _regionTitleText.text = $"{_currentRegion} Levels";
    }
}
