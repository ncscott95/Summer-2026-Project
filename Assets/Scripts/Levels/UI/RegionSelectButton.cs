using UnityEngine;

public class RegionSelectButton : MonoBehaviour
{
    [SerializeField] private LevelRegion _region;

    public void OnButtonClick()
    {
        LevelSelectManager.Instance.SelectRegion(_region);
    }
}
