using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RopeDartStatusUI : Singleton<RopeDartStatusUI>
{
    [SerializeField] private GameObject _barSegmentContainer;
    [SerializeField] private TextMeshProUGUI _bindingStackText;
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _unitCostText;
    [SerializeField] private TextMeshProUGUI _scoreText;

    public readonly Dictionary<string, Color32> BindingToColorLookup = new Dictionary<string, Color32>
    {
        { "Idle",           new Color32(0, 0, 0, 255) },
        { "Spin",           new Color32(255, 0, 0, 255) },
        { "Cast",           new Color32(0, 255, 0, 255) },
        { "Retrieve",       new Color32(0, 0, 255, 255) },
        { "Dragon",         new Color32(255, 255, 0, 255) },
        { "D Dragon",       new Color32(128, 128, 0, 255) },
        { "Necklace",       new Color32(255, 0, 255, 255) },
        { "D Necklace",     new Color32(128, 0, 128, 255) },
        { "Scorpion",       new Color32(0, 255, 255, 255) },
        { "D Scorpion",     new Color32(0, 128, 128, 255) },
        { "Belt",           new Color32(255, 255, 255, 255) },
        { "D Belt",         new Color32(128, 128, 128, 255) },
    };

    private List<Image> _bindingImages = new List<Image>();
    private Color32 _slackColor = new Color32(0, 0, 0, 255);

    public override void Awake()
    {
        base.Awake();

        if (_barSegmentContainer != null) _bindingImages = new List<Image>(_barSegmentContainer.GetComponentsInChildren<Image>());
    }

    void Update()
    {
        UpdateStatusUI();
        if (_bindingStackText != null) _bindingStackText.text = $"Binding: {BindingStack.Instance.CurrentBindingsToString()}";
        string stateText = $"State: S:{(RopeDartManager.Instance.IsLeadSide ? "L" : "A")}";
        if (_stateText != null) _stateText.text = stateText;
        if (_unitCostText != null) _unitCostText.text = $"Unit Cost: {BindingStack.Instance.GetAllTotalUnitCost()}/{BindingStack.MaxAllBindUnits}";
    }

    public void UpdateStatusUI()
    {
        int segmentIndex = 0;

        foreach (BindingStackElement binding in BindingStack.Instance.AllCurrentBindings)
        {
            // set the next binding.UnitCost segments to the color of the binding
            for (int i = 0; i < binding.UnitCost; i++)
            {
                if (segmentIndex >= _bindingImages.Count) break;

                _bindingImages[segmentIndex].color = BindingToColorLookup[binding.NodeId];
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
