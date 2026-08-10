using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BindingStack : Singleton<BindingStack>
{
    public List<BindingStackElement> AllCurrentBindings { get; private set; } = new List<BindingStackElement>();
    // public List<BindingStackElement> LiveCurrentBindings { get; private set; } = new List<BindingStackElement>();

    // Units are used only for calculating behind-the-scenes "costs" of wrapping and binding.
    // These are not the same as physical length, but will be directly proportional to it.
    public const int MaxAllBindUnits = 9;
    // public const int MaxLiveBindUnits = 3;
    // private int _liveMaxBindUnits = MaxAllBindUnits;

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

    public bool TryPushBinding(string bindingInput)
    {
        if (AllCurrentBindings.Count > 0)
        {
            string lastBindingId = AllCurrentBindings[AllCurrentBindings.Count - 1].NodeId;
            BindingGraphNode lastBindingNode = BindingGraph.Nodes.Find(n => n.NodeId == lastBindingId);

            List<BindingGraphConnection> possibleConnections = lastBindingNode.Connections.FindAll(c => c.Input == bindingInput);
            if (possibleConnections.Count == 0)
            {
                Debug.LogWarning($"No connections found for input {bindingInput} from binding {lastBindingId}.");
                return false;
            }

            foreach (BindingGraphConnection connection in possibleConnections)
            {
                if (CanUseConnection(connection))
                {
                    Debug.Log($"Using connection {connection.Nickname} from binding {lastBindingId} with input {bindingInput}.");
                    OnSuccessfulGraphConnection(connection);
                    return true;
                }
            }

            Debug.LogWarning($"No valid connections could be used for input {bindingInput} from binding {lastBindingId}.");
            return false;
        }
        else
        {
            AllCurrentBindings.Add(new BindingStackElement("Idle", 0));

            return false;
        }
    }

    private bool CanUseConnection(BindingGraphConnection connection)
    {
        if (MaxAllBindUnits - GetAllTotalUnitCost() < connection.UnitCost)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because {connection.UnitCost} units would exceed remaining {MaxAllBindUnits - GetAllTotalUnitCost()} all units.");
            return false;
        }

        // if (MaxLiveBindUnits - GetLiveTotalUnitCost() < connection.UnitCost)
        // {
        //     Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because {connection.UnitCost} units would exceed remaining {MaxLiveBindUnits - GetLiveTotalUnitCost()} live units.");
        //     return false;
        // }

        bool meetsSideReqs, meetsSpinReqs, meetsPlaneReqs, meetsCoilReq, meetsStallReq;

        meetsSideReqs = (connection.IsLeadSideValid && RopeDartManager.Instance.IsLeadSide) || (connection.IsAnchorSideValid && !RopeDartManager.Instance.IsLeadSide);
        if (!meetsSideReqs)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because side requirements not met. Connection requires lead side: {connection.IsLeadSideValid}, anchor side: {connection.IsAnchorSideValid}. Current state is lead side: {RopeDartManager.Instance.IsLeadSide}.");
            return false;
        }

        meetsSpinReqs = (connection.IsDownSpinValid && RopeDartManager.Instance.IsDownSpin) || (connection.IsUpSpinValid && !RopeDartManager.Instance.IsDownSpin);
        if (!meetsSpinReqs)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because spin requirements not met. Connection requires down spin: {connection.IsDownSpinValid}, up spin: {connection.IsUpSpinValid}. Current state is down spin: {RopeDartManager.Instance.IsDownSpin}.");
            return false;
        }

        meetsPlaneReqs = (connection.IsWallPlaneValid && RopeDartManager.Instance.IsWallPlane) || (connection.IsDarkPlaneValid && !RopeDartManager.Instance.IsWallPlane);
        if (!meetsPlaneReqs)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because plane requirements not met. Connection requires wall plane: {connection.IsWallPlaneValid}, dark plane: {connection.IsDarkPlaneValid}. Current state is wall plane: {RopeDartManager.Instance.IsWallPlane}.");
            return false;
        }

        meetsCoilReq = (connection.IsCoilingNeeded && RopeDartManager.Instance.IsCoiling) || (!connection.IsCoilingNeeded && !RopeDartManager.Instance.IsCoiling);
        if (!meetsCoilReq)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because coil requirements not met. Connection requires coiling: {connection.IsCoilingNeeded}. Current state is coiling: {RopeDartManager.Instance.IsCoiling}.");
            return false;
        }

        meetsStallReq = (connection.IsStalledNeeded && RopeDartManager.Instance.IsStalled) || (!connection.IsStalledNeeded && !RopeDartManager.Instance.IsStalled);
        if (!meetsStallReq)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because stall requirements not met. Connection requires stalled: {connection.IsStalledNeeded}. Current state is stalled: {RopeDartManager.Instance.IsStalled}.");
            return false;
        }

        return meetsSideReqs && meetsSpinReqs && meetsPlaneReqs && meetsCoilReq && meetsStallReq;
    }

    private void OnSuccessfulGraphConnection(BindingGraphConnection connection)
    {
        foreach (BindingStackElement nodeUnitCost in connection.NodeSequence)
        {
            // "." can be used as shorthand for no additional node, or a binding that doesn't add a node to the stack
            if (nodeUnitCost.NodeId == ".") break;

            AllCurrentBindings.Add(new BindingStackElement(nodeUnitCost.NodeId, nodeUnitCost.UnitCost));
            // LiveCurrentBindings.Add(new BindingStackElement(nodeUnitCost.NodeId, nodeUnitCost.UnitCost));
        }

        // remove a previous "Retrieve" binding
        RemoveLastBindingWithId("Retrieve");

        if (connection.FlipsLeadAnchor) RopeDartManager.Instance.FlipLeadAnchor();
        if (connection.FlipsDownUp) RopeDartManager.Instance.FlipSpinDirection();
        if (connection.FlipsWallDark) RopeDartManager.Instance.FlipPlane();
        _isWallPlane = AllCurrentBindings.Count(b => b.NodeId == "LeadNeck" || b.NodeId == "LeadSide" || b.NodeId == "AnchorNeck" || b.NodeId == "AnchorSide") % 2 == 0;
        RopeDartManager.Instance.SetCoiling(connection.SetsCoiling);

        if (connection.Input == "Spin")
        {
            // only do this for "Spin" bindings, not "Uncoil" or any others
            // if (connection.Nickname == "Spin")
            // {
            //     LiveCurrentBindings.Clear();
            //     _liveMaxBindUnits = Mathf.Min(MaxLiveBindUnits, MaxAllBindUnits - GetAllTotalUnitCost());
            // }

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
            // LiveCurrentBindings.Clear();
            // _liveMaxBindUnits = Mathf.Min(MaxLiveBindUnits, MaxAllBindUnits - GetAllTotalUnitCost());
            RopeDartManager.Instance.StartWrap();
        }
        else if (connection.Input.StartsWith("Twine"))
        {
            if (GetBindingAtIndex(AllCurrentBindings.Count - 2).NodeId.EndsWith("Elbow"))
            {
                // _liveMaxBindUnits = Mathf.Min(MaxLiveBindUnits, MaxAllBindUnits - GetAllTotalUnitCost());
            }
            else
            {
                // _liveMaxBindUnits = MaxAllBindUnits;
                // remove a previous "Spin" binding from the stack
                // assumes that twines to all points other than elbows release the lead hand
                RemoveLastBindingWithId("Spin");
            }
        }
        else
        {
            // Debug.LogWarning($"Unhandled input {connection.Input} for binding {connection.Nickname}");
        }

        // TODO: temp commented to prevent errors while testing wrap detection and unwrapping
        if (connection.Input != "(Nothing)") _ropeDartVisualManager.UpdateVisuals(connection);
    }

    public string DetectWrap()
    {
        List<KeyValuePair<List<string>, string>> possibleWrapResults = _wrapBindings.ToList();

        Debug.Log($"Possible wrap results: {string.Join(", ", possibleWrapResults.Select(w => w.Value))}");

        for (int i = 1; i < AllCurrentBindings.Count; i++)
        {
            // skip the last binding since it is always "Wrap"
            BindingStackElement binding = AllCurrentBindings[^(i + 1)];
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
        BindingGraphNode previousBindingNode = GetBindingAtIndex(AllCurrentBindings.Count - 2);
        bool wasCastFromElbow = previousBindingNode.NodeId.EndsWith("Elbow");
        if (previousBindingNode.NodeId.EndsWith("Spin") || previousBindingNode.NodeId.EndsWith("Elbow"))
        {
            RemoveBindingAtIndex(AllCurrentBindings.Count - 2);
        }

        // remove all "Elbow" bindings from the top of the stack until we reach a binding that is not an "Elbow" binding
        if (wasCastFromElbow)
        {
            while (true)
            {
                previousBindingNode = GetBindingAtIndex(AllCurrentBindings.Count - 2);
                if (!previousBindingNode.NodeId.EndsWith("Elbow")) break;
                RemoveBindingAtIndex(AllCurrentBindings.Count - 2);
            }
        }

        int wrapIndex = AllCurrentBindings.FindIndex(BindingStackElement => BindingStackElement.NodeId == "Wrap");

        if (wrapIndex != -1)
        {
            while (wrapIndex > 0 && wrapIndex < AllCurrentBindings.Count - 1)
            {
                BindingStackElement beforeWrap = AllCurrentBindings[wrapIndex - 1];
                BindingStackElement afterWrap = AllCurrentBindings[wrapIndex + 1];

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

        _isWallPlane = AllCurrentBindings.Count(b => b.NodeId == "LeadNeck" || b.NodeId == "LeadSide" || b.NodeId == "AnchorNeck" || b.NodeId == "AnchorSide") % 2 == 0;
    }

    public BindingGraphNode PeekBinding()
    {
        return GetBindingAtIndex(AllCurrentBindings.Count - 1);
    }

    public BindingGraphNode GetBindingAtIndex(int index)
    {
        if (index >= 0 && index < AllCurrentBindings.Count)
        {
            BindingStackElement binding = AllCurrentBindings[index];
            return BindingGraph.Nodes.Find(n => n.NodeId == binding.NodeId);
        }
        return null;
    }

    public void RemoveBindingAtIndex(int index)
    {
        if (index >= 0 && index < AllCurrentBindings.Count)
        {
            AllCurrentBindings.RemoveAt(index);
        }
    }

    public bool RemoveLastBindingWithId(string nodeId)
    {
        int lastIndex = AllCurrentBindings.FindLastIndex(b => b.NodeId == nodeId);
        if (lastIndex != -1)
        {
            AllCurrentBindings.RemoveAt(lastIndex);
            return true;
        }
        return false;
    }

    public bool RemoveLastBindingWithIdEndingWith(string nodeIdPrefix)
    {
        int lastIndex = AllCurrentBindings.FindLastIndex(b => b.NodeId.EndsWith(nodeIdPrefix));
        if (lastIndex != -1)
        {
            AllCurrentBindings.RemoveAt(lastIndex);
            return true;
        }
        return false;
    }

    public void ClearBindings()
    {
        AllCurrentBindings.Clear();
    }

    public void UpdateCurrentBindingUnitCost(int deltaCost)
    {
        if (AllCurrentBindings.Count > 0)
        {
            int lastIndex = AllCurrentBindings.Count - 1;
            BindingStackElement lastBinding = AllCurrentBindings[lastIndex];
            lastBinding.UnitCost += deltaCost;
            AllCurrentBindings[lastIndex] = lastBinding;
        }
    }

    public int GetAllTotalUnitCost()
    {
        int totalCost = 0;
        foreach (var point in AllCurrentBindings)
        {
            totalCost += point.UnitCost;
        }
        return totalCost;
    }

    // public int GetLiveTotalUnitCost()
    // {
    //     int totalCost = 0;
    //     foreach (var point in LiveCurrentBindings)
    //     {
    //         totalCost += point.UnitCost;
    //     }
    //     return totalCost;
    // }

    // returns a string representing the current stack of bindings
    public string CurrentBindingsToString()
    {
        return string.Join(", ", AllCurrentBindings.Select(b => $"{b.NodeId} ({b.UnitCost})"));
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
