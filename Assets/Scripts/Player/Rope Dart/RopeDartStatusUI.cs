using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RopeDartStatusUI : Singleton<RopeDartStatusUI>
{
    [SerializeField] private GameObject _barSegmentContainer;
    [SerializeField] private Transform _angleIndicator;
    [SerializeField] private TextMeshProUGUI _bindingStackText;
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _unitCostText;

    public readonly Dictionary<string, Color32> BindingToColorLookup = new Dictionary<string, Color32>
    {
        { "Idle",           new Color32(0, 0, 0, 255) },
        { "Spin",           new Color32(255, 0, 0, 255) },
        { "Cast",           new Color32(0, 255, 0, 255) },
        { "Retrieve",       new Color32(255, 0, 255, 255) },
        { "Wrap",           new Color32(255, 255, 0, 255) },
        { "LeadElbow",      new Color32(0, 255, 255, 255) },
        { "AnchorElbow",    new Color32(128, 0, 128, 255) },
        { "LeadNeck",       new Color32(255, 165, 0, 255) },
        { "AnchorNeck",     new Color32(0, 128, 0, 255) },
        { "LeadSide",       new Color32(0, 0, 255, 255) },
        { "AnchorSide",     new Color32(128, 128, 128, 255) },
    };

    private List<Image> _bindingImages = new List<Image>();
    private Color32 _spinColor = new Color32(255, 0, 0, 255);
    private Color32 _slackColor = new Color32(0, 0, 0, 255);

    public override void Awake()
    {
        base.Awake();

        if (_barSegmentContainer != null) _bindingImages = new List<Image>(_barSegmentContainer.GetComponentsInChildren<Image>());
    }

    void Update()
    {
        UpdateStatusUI();
        if (_angleIndicator != null) _angleIndicator.localRotation = Quaternion.Euler(0f, 0f, -RopeDartManager.Instance.RawAngle);
        if (_bindingStackText != null) _bindingStackText.text = $"Binding: {BindingStack.Instance.CurrentBindingsToString()}";
        string stateText = $"State: {RopeDartManager.Instance.CurrentState}, {(RopeDartManager.Instance.IsFrontPlane ? "F" : "B")}/{(RopeDartManager.Instance.IsLeadSide ? "L" : "A")}/{(RopeDartManager.Instance.IsDownSpin ? "D" : "U")}/{(RopeDartManager.Instance.IsClockwise ? "CW" : "CCW")}/{(RopeDartManager.Instance.IsLastCastEast ? "E" : "W")}/{(RopeDartManager.Instance.IsCoiling ? "C" : "-")}";
        if (_stateText != null) _stateText.text = stateText;
        // string unitCostText = $"Unit Cost: {BindingStack.Instance.GetAllTotalUnitCost()}/{BindingStack.MaxAllBindUnits} (Live: {BindingStack.Instance.GetLiveTotalUnitCost()}/{BindingStack.MaxLiveBindUnits})";
        string unitCostText = $"Unit Cost: {BindingStack.Instance.GetAllTotalUnitCost()}/{BindingStack.MaxAllBindUnits}";
        if (_unitCostText != null) _unitCostText.text = unitCostText;
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

        // set the next up to 3 segments to the spin color if the player is currently spinning or idle
        // if (RopeDartManager.Instance.CurrentState == RopeDartState.Spinning || RopeDartManager.Instance.CurrentState == RopeDartState.Idle)
        // {
        //     for (int i = 0; i < 3; i++)
        //     {
        //         if (segmentIndex >= _bindingImages.Count) break;

        //         _bindingImages[segmentIndex].color = _spinColor;
        //         segmentIndex++;
        //     }
        // }

        // set the remaining segments to the slack color
        for (int i = segmentIndex; i < _bindingImages.Count; i++)
        {
            _bindingImages[i].color = _slackColor;
        }
    }
}
