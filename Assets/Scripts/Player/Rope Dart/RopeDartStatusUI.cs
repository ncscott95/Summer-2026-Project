using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RopeDartStatusUI : Singleton<RopeDartStatusUI>
{
    [SerializeField] private GameObject _barSegmentContainer;

    public readonly Dictionary<string, Color32> BindingToColorLookup = new Dictionary<string, Color32>
    {
        { "Idle",                           new Color32(0, 0, 0, 255) },
        { "Lead Down Spin",                 new Color32(0, 255, 0, 255) },
        { "Lead Up Spin",                   new Color32(0, 255, 0, 255) },
        { "Anchor Down Spin",               new Color32(0, 255, 0, 255) },
        { "Anchor Up Spin",                 new Color32(0, 255, 0, 255) },
        { "Lead Elbow",                     new Color32(255, 165, 0, 255) },
        { "Anchor Elbow",                   new Color32(0, 165, 255, 255) },
        { "Dragon",                         new Color32(128, 0, 128, 255) },
        { "Dark Dragon",                    new Color32(128, 0, 128, 128) },
        { "Scorpion",                       new Color32(255, 20, 147, 255) },
        { "Dark Scorpion",                  new Color32(255, 20, 147, 128) },
        { "Lead Thigh Saddle",              new Color32(255, 215, 0, 255) },
        { "Anchor Thigh Holster",           new Color32(0, 191, 255, 255) },
    };

    private List<Image> _bindingImages = new List<Image>();
    private Color32 _spinColor = new Color32(255, 0, 0, 255);
    private Color32 _slackColor = new Color32(0, 0, 0, 255);

    public override void Awake()
    {
        base.Awake();

        _bindingImages = new List<Image>(_barSegmentContainer.GetComponentsInChildren<Image>());
    }

    void Update()
    {
        UpdateStatusUI();
    }

    public void UpdateStatusUI()
    {
        int segmentIndex = 0;

        foreach (BindingStackElement binding in BindingStack.Instance.CurrentBindings)
        {
            // set the next binding.UnitCost segments to the color of the binding
            for (int i = 0; i < binding.UnitCost; i++)
            {
                if (segmentIndex >= _bindingImages.Count) break;

                _bindingImages[segmentIndex].color = BindingToColorLookup[binding.Point];
                segmentIndex++;
            }
        }

        // set the next up to 3 segments to the spin color if the player is currently spinning or idle
        if (RopeDartManager.Instance.CurrentState == RopeDartState.Spinning || RopeDartManager.Instance.CurrentState == RopeDartState.Idle)
        {
            for (int i = 0; i < 3; i++)
            {
                if (segmentIndex >= _bindingImages.Count) break;

                _bindingImages[segmentIndex].color = _spinColor;
                segmentIndex++;
            }
        }

        // set the remaining segments to the slack color
        for (int i = segmentIndex; i < _bindingImages.Count; i++)
        {
            _bindingImages[i].color = _slackColor;
        }
    }
}
