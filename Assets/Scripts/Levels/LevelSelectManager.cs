using System.Collections.Generic;
using UnityEngine;

public class LevelSelectManager : MonoBehaviour
{
    [SerializeField] private Transform _levelButtonContainer;

    private List<LevelSelectButton> _levelButtons = new List<LevelSelectButton>();

    void Start()
    {
        // TODO: probably move this logic when we have to load different lists of levels based on which region we are selecting
        _levelButtons = new List<LevelSelectButton>(_levelButtonContainer.GetComponentsInChildren<LevelSelectButton>());
        foreach (LevelSelectButton button in _levelButtons)
        {
            button.Initialize();
        }
    }
}
