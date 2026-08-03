using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BindingStack : Singleton<BindingStack>
{
    public List<BindingStackElement> CurrentBindings { get; private set; } = new List<BindingStackElement>();

    // Units are used only for calculating behind-the-scenes "costs" of wrapping and binding.
    // These are not the same as physical length, but will be directly proportional to it.
    public const int MaxBindUnits = 10;

    public BindingGraphData BindingGraph { get; private set; }

    [SerializeField] private RopeDartVisualManager _ropeDartVisualManager;

    private static readonly List<KeyValuePair<List<string>, string>> _wrapBindings = new List<KeyValuePair<List<string>, string>>
    {
        new KeyValuePair<List<string>, string>(new List<string> { "LeadSide", "AnchorNeck" }, "Dragon"),
        new KeyValuePair<List<string>, string>(new List<string> { "AnchorSide", "LeadNeck" }, "Dark Dragon"),
        new KeyValuePair<List<string>, string>(new List<string> { "LeadNeck", "AnchorSide" }, "Scorpion"),
        new KeyValuePair<List<string>, string>(new List<string> { "AnchorNeck", "LeadSide" }, "Dark Scorpion"),
        new KeyValuePair<List<string>, string>(new List<string> { "LeadNeck", "AnchorNeck" }, "Necklace"),
        new KeyValuePair<List<string>, string>(new List<string> { "AnchorNeck", "LeadNeck" }, "Necklace"),
        new KeyValuePair<List<string>, string>(new List<string> { "LeadSide", "AnchorSide" }, "Belt"),
        new KeyValuePair<List<string>, string>(new List<string> { "AnchorSide", "LeadSide" }, "Belt"),
        new KeyValuePair<List<string>, string>(new List<string> { "AnchorNeck", "LeadNeck", "LeadSide", "AnchorSide" }, "Butterfly"),
    };

    private bool _isWallPlane = true;
    public bool GetIsWallPlane() { return _isWallPlane; }

    public override void Awake()
    {
        base.Awake();

        BindingGraph = JsonUtility.FromJson<BindingGraphData>(Resources.Load<TextAsset>("BindingGraph").text);
    }

    public BindingGraphConnection TryPushBinding(string bindingInput)
    {
        if (CurrentBindings.Count > 0)
        {
            string lastBindingId = CurrentBindings[CurrentBindings.Count - 1].NodeId;
            BindingGraphNode lastBindingNode = BindingGraph.Nodes.Find(n => n.NodeId == lastBindingId);

            List<BindingGraphConnection> possibleConnections = lastBindingNode.Connections.FindAll(c => c.Input == bindingInput);
            if (possibleConnections.Count == 0)
            {
                Debug.LogWarning($"No connections found for input {bindingInput} from binding {lastBindingId}.");
                return null;
            }

            foreach (BindingGraphConnection connection in possibleConnections)
            {
                if (CanUseConnection(connection))
                {
                    Debug.Log($"Using connection {connection.Nickname} from binding {lastBindingId} with input {bindingInput}.");
                    OnSuccessfulGraphConnection(connection);
                    return connection;
                }
            }

            Debug.LogWarning($"No valid connections could be used for input {bindingInput} from binding {lastBindingId}.");
            return null;
        }
        else
        {
            CurrentBindings.Add(new BindingStackElement("Idle", 0));

            return null;
        }
    }

    private bool CanUseConnection(BindingGraphConnection connection)
    {
        if (GetRemainingUnits() < connection.UnitCost) return false;

        bool meetsSideReqs, meetsSpinReqs, meetsPlaneReqs;

        meetsSideReqs = connection.IsLeadSideValid && RopeDartManager.Instance.IsLeadSide || connection.IsAnchorSideValid && !RopeDartManager.Instance.IsLeadSide;
        meetsSpinReqs = connection.IsDownSpinValid && RopeDartManager.Instance.IsDownSpin || connection.IsUpSpinValid && !RopeDartManager.Instance.IsDownSpin;
        meetsPlaneReqs = connection.IsWallPlaneValid && RopeDartManager.Instance.IsWallPlane || connection.IsDarkPlaneValid && !RopeDartManager.Instance.IsWallPlane;

        return meetsSideReqs && meetsSpinReqs && meetsPlaneReqs;
    }

    private void OnSuccessfulGraphConnection(BindingGraphConnection connection)
    {
        foreach (BindingStackElement nodeUnitCost in connection.NodeSequence)
        {
            // "." can be used as shorthand for no additional node, or a binding that doesn't add a node to the stack
            if (nodeUnitCost.NodeId == ".") break;

            CurrentBindings.Add(new BindingStackElement(nodeUnitCost.NodeId, nodeUnitCost.UnitCost));
        }

        // remove a previous "Retrieve" binding
        RemoveLastBindingWithId("Retrieve");

        if (connection.FlipsLeadAnchor) RopeDartManager.Instance.FlipLeadAnchor();
        if (connection.FlipsDownUp) RopeDartManager.Instance.FlipSpinDirection();
        if (connection.FlipsWallDark) RopeDartManager.Instance.FlipPlane();

        if (connection.Input == "Spin")
        {
            RopeDartManager.Instance.StartSpin();
        }
        else if (connection.Input == "Cast")
        {
            OnCast();
            RopeDartManager.Instance.Cast();
        }
        else if (connection.Input == "Retrieve")
        {
            // remove a previous "Cast" binding
            RemoveLastBindingWithId("Cast");
            RopeDartManager.Instance.Retrieve();
        }
        else if (connection.Input == "Wrap")
        {
            RopeDartManager.Instance.StartWrap();
        }
        else if (connection.Input.StartsWith("Twine"))
        {
            // remove a previous "Spin" binding from the stack
            RemoveLastBindingWithId("Spin");
        }

        // TODO: temp commented to prevent errors while testing wrap detection and unwrapping
        // _ropeDartVisualManager.UpdateVisuals(connection);
    }

    public string DetectWrap()
    {
        List<KeyValuePair<List<string>, string>> possibleWrapResults = _wrapBindings.ToList();

        Debug.Log($"Possible wrap results: {string.Join(", ", possibleWrapResults.Select(w => w.Value))}");

        for (int i = 1; i < CurrentBindings.Count; i++)
        {
            // skip the last binding since it is always "Wrap"
            BindingStackElement binding = CurrentBindings[^(i + 1)];
            if (binding.NodeId == "Wrap") continue;

            foreach (var wrapResult in possibleWrapResults.ToList())
            {
                if (wrapResult.Key.Count <= i || wrapResult.Key[^i] != binding.NodeId)
                {
                    possibleWrapResults.Remove(wrapResult);
                }
                else if (wrapResult.Key.Count == i + 1 && wrapResult.Key[^1] == binding.NodeId)
                {
                    return wrapResult.Value;
                }
            }
        }

        return null;
    }

    public void OnCast()
    {
        // "Spin", "Elbow", "Knee", and "Foot" bindings all automatically unwind when being cast from
        BindingGraphNode previousBindingNode = GetBindingAtIndex(CurrentBindings.Count - 2);
        if (previousBindingNode.NodeId.EndsWith("Spin") || previousBindingNode.NodeId.EndsWith("Elbow")
                || previousBindingNode.NodeId.EndsWith("Knee") || previousBindingNode.NodeId.EndsWith("Foot"))
        {
            RemoveBindingAtIndex(CurrentBindings.Count - 2);
        }

        int wrapIndex = CurrentBindings.FindIndex(BindingStackElement => BindingStackElement.NodeId == "Wrap");

        if (wrapIndex != -1)
        {
            while (wrapIndex > 0 && wrapIndex < CurrentBindings.Count - 1)
            {
                BindingStackElement beforeWrap = CurrentBindings[wrapIndex - 1];
                BindingStackElement afterWrap = CurrentBindings[wrapIndex + 1];

                if (beforeWrap.NodeId.StartsWith('L') && afterWrap.NodeId.StartsWith('L') || (beforeWrap.NodeId.StartsWith('A') && afterWrap.NodeId.StartsWith('A')))
                {
                    RemoveBindingAtIndex(wrapIndex + 1);
                    RemoveBindingAtIndex(wrapIndex - 1);

                    if (beforeWrap.NodeId == afterWrap.NodeId) --wrapIndex;
                    else break;
                }
                else
                {
                    break;
                }
            }

            RemoveBindingAtIndex(wrapIndex);
        }

        _isWallPlane = CurrentBindings.Count(b => b.NodeId == "LeadNeck" || b.NodeId == "LeadSide" || b.NodeId == "AnchorNeck" || b.NodeId == "AnchorSide") % 2 == 0;
    }

    public BindingGraphNode PeekBinding()
    {
        return GetBindingAtIndex(CurrentBindings.Count - 1);
    }

    public BindingGraphNode GetBindingAtIndex(int index)
    {
        if (index >= 0 && index < CurrentBindings.Count)
        {
            BindingStackElement binding = CurrentBindings[index];
            return BindingGraph.Nodes.Find(n => n.NodeId == binding.NodeId);
        }
        return null;
    }

    public void RemoveBindingAtIndex(int index)
    {
        if (index >= 0 && index < CurrentBindings.Count)
        {
            CurrentBindings.RemoveAt(index);
        }
    }

    public void RemoveLastBindingWithId(string nodeId)
    {
        int lastIndex = CurrentBindings.FindLastIndex(b => b.NodeId == nodeId);
        if (lastIndex != -1)
        {
            CurrentBindings.RemoveAt(lastIndex);
        }
    }

    public void ClearBindings()
    {
        CurrentBindings.Clear();
    }

    public void UpdateCurrentBindingUnitCost(int deltaCost)
    {
        if (CurrentBindings.Count > 0)
        {
            int lastIndex = CurrentBindings.Count - 1;
            BindingStackElement lastBinding = CurrentBindings[lastIndex];
            lastBinding.UnitCost += deltaCost;
            CurrentBindings[lastIndex] = lastBinding;
        }
    }

    public int GetTotalUnitCost()
    {
        int totalCost = 0;
        foreach (var point in CurrentBindings)
        {
            totalCost += point.UnitCost;
        }
        return totalCost;
    }

    public int GetRemainingUnits()
    {
        return MaxBindUnits - GetTotalUnitCost();
    }

    // returns a string representing the current stack of bindings
    public string CurrentBindingsToString()
    {
        return string.Join(", ", CurrentBindings.Select(b => b.NodeId));
    }
}

[System.Serializable]
public struct BindingStackElement
{
    public string NodeId;
    public int UnitCost;

    public BindingStackElement(string nodeId, int unitCost)
    {
        NodeId = nodeId;
        UnitCost = unitCost;
    }
}
