using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BindingStack : Singleton<BindingStack>
{
    public List<BindingStackElement> AllCurrentBindings { get; private set; } = new List<BindingStackElement>();
    public (int start, int end) PrimedBindingRange { get; private set; } = (-1, -1);

    // Units are used only for calculating behind-the-scenes "costs" of wrapping and binding.
    // These are not the same as physical length, but will be directly proportional to it.
    public const int MaxAllBindUnits = 3;

    public BindingGraphData BindingGraph { get; private set; }

    // these work in backwards order: if the top binding is Dragon, the next binding down must be Belt to prime
    private static readonly List<KeyValuePair<string, List<string>>> _primingBindings = new List<KeyValuePair<string, List<string>>>
    {
        new KeyValuePair<string, List<string>>("Dragon", new List<string> { "D Scorpion", "D Belt" }),
        new KeyValuePair<string, List<string>>("D Dragon", new List<string> { "Scorpion", "Belt" }),
        new KeyValuePair<string, List<string>>("Necklace", new List<string> { "D Dragon", "D Necklace" }),
        new KeyValuePair<string, List<string>>("D Necklace", new List<string> { "Dragon", "Necklace" }),
        new KeyValuePair<string, List<string>>("Scorpion", new List<string> { "D Dragon", "D Necklace" }),
        new KeyValuePair<string, List<string>>("D Scorpion", new List<string> { "Dragon", "Necklace" }),
        new KeyValuePair<string, List<string>>("Belt", new List<string> { "D Scorpion", "D Belt" }),
        new KeyValuePair<string, List<string>>("D Belt", new List<string> { "Scorpion", "Belt" }),
        // new KeyValuePair<string, List<string>>("Manacle", new List<string> { "Manacle" }),
        // new KeyValuePair<string, List<string>>("Overlord", new List<string> { "Overlord" }),
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
        if (AllCurrentBindings.Count > 0)
        {
            string lastBindingId = AllCurrentBindings[AllCurrentBindings.Count - 1].NodeId;
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
                    // Debug.Log($"Using connection {connection.Nickname} from binding {lastBindingId} with input {bindingInput}.");
                    OnSuccessfulGraphConnection(connection);
                    return connection;
                }
            }

            Debug.LogWarning($"No valid connections could be used for input {bindingInput} from binding {lastBindingId}.");
            return null;
        }
        else
        {
            AllCurrentBindings.Add(new BindingStackElement("Idle", 0));

            return null;
        }
    }

    private bool CanUseConnection(BindingGraphConnection connection)
    {
        if (MaxAllBindUnits - GetAllTotalUnitCost() < connection.UnitCost)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because {connection.UnitCost} units would exceed remaining {MaxAllBindUnits - GetAllTotalUnitCost()} all units.");
            return false;
        }

        bool meetsSideReqs;

        meetsSideReqs = (connection.IsLeadSideValid && RopeDartManager.Instance.IsLeadSide) || (connection.IsAnchorSideValid && !RopeDartManager.Instance.IsLeadSide);
        if (!meetsSideReqs)
        {
            Debug.Log($"FAIL: Cannot use connection {connection.Nickname} because side requirements not met. Connection requires lead side: {connection.IsLeadSideValid}, anchor side: {connection.IsAnchorSideValid}. Current state is lead side: {RopeDartManager.Instance.IsLeadSide}.");
            return false;
        }

        return meetsSideReqs;
    }

    private void OnSuccessfulGraphConnection(BindingGraphConnection connection)
    {
        foreach (BindingStackElement nodeUnitCost in connection.NodeSequence)
        {
            // "." can be used as shorthand for no additional node, or a binding that doesn't add a node to the stack
            if (nodeUnitCost.NodeId == ".") break;

            AllCurrentBindings.Add(new BindingStackElement(nodeUnitCost.NodeId, nodeUnitCost.UnitCost));
        }

        RemoveLastBindingWithId("Retrieve");

        if (connection.FlipsLeadAnchor) RopeDartManager.Instance.FlipLeadAnchor();

        if (connection.Input == "(Start)")
        {
            // required to start first animation after starting the level
            // _ropeDartVisualManager.StartAnimation(connection.Animation, false);
            RopeDartVisualManager.Instance.StartAnimation(connection.Animation);
        }
        else if (connection.Input == "Cast")
        {
            RemoveLastBindingWithId("Spin");
            HandleCastUnwind();
            ScoringSystem.Instance.AddBinding(connection.Nickname, connection.BasePoints);
            RopeDartManager.Instance.ResetHitCount();
            // RopeDartInputController.Instance.StartCastEndBuffer();
        }
        else if (connection.Input == "Retrieve")
        {
            RemoveLastBindingWithId("Cast");
            // RopeDartInputController.Instance.StartRetrieveEndBuffer();
        }
        else if (connection.Input.StartsWith("Twine"))
        {
            RemoveLastBindingWithId("Spin");
            if (connection.Nickname == "Elbow Shot")
            {
                HandleCastUnwind();
                ScoringSystem.Instance.AddBinding(connection.Nickname, connection.BasePoints);
                RopeDartManager.Instance.ResetHitCount();
                // RopeDartInputController.Instance.StartElbowEndBuffer();
            }
            else if (connection.Nickname == "Dragon" || connection.Nickname == "Necklace" || connection.Nickname == "Scorpion" || connection.Nickname == "Belt")
            {
                string newBindingName = ResolveWrapBindingName(connection.Nickname);
                RenameBindingAtIndex(AllCurrentBindings.Count - 1, newBindingName);
                DetectPrimedBindings();
                ScoringSystem.Instance.AddBinding(newBindingName, connection.BasePoints);

                if (connection.Nickname == "Dragon")
                {
                    // RopeDartInputController.Instance.StartDragonEndBuffer();
                }
                else if (connection.Nickname == "Necklace")
                {
                    // RopeDartInputController.Instance.StartNeckEndBuffer();
                }
            }
        }
        else if (connection.Input == "(Nothing)")
        {
            if (connection.Nickname == "Spin")
            {
                // do not decay multiplier on first spin after another action
                // ScoringSystem.Instance.DoMultiplierDecay();
                // RopeDartInputController.Instance.StartSpinEndBuffer();
            }
            else if (connection.Nickname == "Continue Spin")
            {
                RemoveLastBindingWithId("Spin");
                ScoringSystem.Instance.DoMultiplierDecay();
                // RopeDartInputController.Instance.StartSpinEndBuffer();
            }
            else if (connection.Nickname == "Retrieve")
            {
                RemoveLastBindingWithId("Cast");
                // RopeDartInputController.Instance.StartRetrieveEndBuffer();
            }
        }
        else
        {
            Debug.LogWarning($"Unhandled input {connection.Input} for binding {connection.Nickname}");
        }

        // _ropeDartVisualManager.UpdateVisuals(connection);
        // if (connection.Input == "(Start)")
        // {
        //     required to start first animation after starting the level
        //     _ropeDartVisualManager.StartAnimation(connection.Animation, false);
        //     RopeDartVisualManager.Instance.StartAnimation(connection.Animation, false);
        // }
        // else
        // {
        //     _ropeDartVisualManager.SetBufferedAnimation(connection);
            
        // }
    }

    private string ResolveWrapBindingName(string bindingName)
    {
        string previousBindingName = AllCurrentBindings[AllCurrentBindings.Count - 2].NodeId;
        string darkBindingName = "D " + bindingName;

        // if there is a previous wrap binding, automatically use the version that primes with the previous binding

        KeyValuePair<string, List<string>> lightPrimingBindings = _primingBindings.Find(p => p.Key == bindingName);
        KeyValuePair<string, List<string>> darkPrimingBindings = _primingBindings.Find(p => p.Key == darkBindingName);

        if (lightPrimingBindings.Value != null && lightPrimingBindings.Value.Contains(previousBindingName))
        {
            return bindingName;
        }

        if (darkPrimingBindings.Value != null && darkPrimingBindings.Value.Contains(previousBindingName))
        {
            RopeDartVisualManager.Instance.SetDarkTrigger();
            return darkBindingName;
        }

        // if there is no previous wrap binding, use the default version based on whether the player is lead or anchor side
        // lead:   dragon = dark,  necklace = light, scorpion = dark,  belt = dark
        // anchor: dragon = light, necklace = dark,  scorpion = light, belt = light

        if (bindingName == "Dragon" || bindingName == "Scorpion" || bindingName == "Belt")
        {
            if (RopeDartManager.Instance.IsLeadSide) RopeDartVisualManager.Instance.SetDarkTrigger();
            return RopeDartManager.Instance.IsLeadSide ? darkBindingName : bindingName;
        }
        else if (bindingName == "Necklace")
        {
            if (!RopeDartManager.Instance.IsLeadSide) RopeDartVisualManager.Instance.SetDarkTrigger();
            return !RopeDartManager.Instance.IsLeadSide ? darkBindingName : bindingName;
        }

        return bindingName;
    }

    // working backwards from the top binding, check if the binding below it is a valid priming binding
    // once an invalid priming binding is found, stop checking and set the primed binding range to the last valid priming binding found
    // if the first pair of bindings checked is invalid, start over at the next binding down
    // if no valid priming bindings are found, set the primed binding range to (-1, -1)
    public void DetectPrimedBindings()
    {
        int topValidPrimingIndex = -1;
        int lastValidPrimingIndex = -1;

        for (int i = AllCurrentBindings.Count - 1; i > 0; --i)
        {
            BindingStackElement currentBinding = AllCurrentBindings[i];
            BindingStackElement previousBinding = AllCurrentBindings[i - 1];
            KeyValuePair<string, List<string>> primingBinding = _primingBindings.Find(p => p.Key == currentBinding.NodeId);

            if (primingBinding.Key != null && primingBinding.Value.Contains(previousBinding.NodeId))
            {
                if (topValidPrimingIndex == -1) topValidPrimingIndex = i;
                lastValidPrimingIndex = i - 1;

                // TODO: this hard codes primings to only ever be two bindings long
                PrimedBindingRange = (lastValidPrimingIndex, topValidPrimingIndex);
                break;
            }
            else if (lastValidPrimingIndex != -1)
            {
                PrimedBindingRange = (lastValidPrimingIndex, topValidPrimingIndex);
                break;
            }
        }

        if (lastValidPrimingIndex == -1)
        {
            PrimedBindingRange = (-1, -1);
        }

        string primedBindingsString = PrimedBindingRange.start != -1 && PrimedBindingRange.end != -1
            ? string.Join(", ", AllCurrentBindings.GetRange(PrimedBindingRange.start, PrimedBindingRange.end - PrimedBindingRange.start + 1).Select(b => b.NodeId))
            : "None";
        Debug.Log($"Primed bindings: {primedBindingsString}");
    }

    public void HandleCastUnwind()
    {
        int topNonCastIndex = AllCurrentBindings.FindLastIndex(b => b.NodeId != "Cast");
        int primedBindingCount = PrimedBindingRange.end - PrimedBindingRange.start + 1;
        if (topNonCastIndex == PrimedBindingRange.end && primedBindingCount > 1)
        {
            for (int i = 0; i < primedBindingCount; i++)
            {
                RemoveBindingAtIndex(PrimedBindingRange.start);
            }
            PrimedBindingRange = (-1, -1);
        }
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

    public void RenameBindingAtIndex(int index, string newName)
    {
        if (index >= 0 && index < AllCurrentBindings.Count)
        {
            BindingStackElement binding = AllCurrentBindings[index];
            binding.NodeId = newName;
            AllCurrentBindings[index] = binding;
        }
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

    public void ClearBindings()
    {
        AllCurrentBindings.Clear();
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
