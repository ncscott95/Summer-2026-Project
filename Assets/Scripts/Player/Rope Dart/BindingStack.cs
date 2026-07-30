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

    public override void Awake()
    {
        base.Awake();

        BindingGraph = JsonUtility.FromJson<BindingGraphData>(Resources.Load<TextAsset>("BindingGraph").text);
    }

    public BindingGraphData.BindingGraphConnection TryPushBinding(string bindingInput)
    {
        if (CurrentBindings.Count > 0)
        {
            string lastBindingId = CurrentBindings[CurrentBindings.Count - 1].NodeId;
            BindingGraphData.BindingGraphNode lastBindingNode = BindingGraph.Nodes.Find(n => n.NodeId == lastBindingId);

            List<BindingGraphData.BindingGraphConnection> possibleConnections = lastBindingNode.Connections.FindAll(c => c.Input == bindingInput);
            if (possibleConnections.Count == 0)
            {
                Debug.LogWarning($"No valid connections found for input {bindingInput} from binding {lastBindingId}.");
                return null;
            }

            foreach (BindingGraphData.BindingGraphConnection connection in possibleConnections)
            {
                if (CanUseConnection(connection))
                {
                    Debug.Log($"Using connection {connection.Nickname} from binding {lastBindingId} to {bindingInput}.");
                    UseGraphConnection(connection);
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

    private bool CanUseConnection(BindingGraphData.BindingGraphConnection connection)
    {
        if (GetRemainingUnits() < connection.UnitCost) return false;
        
        bool meetsLeadReqs, meetsSpinReqs; //, meetsPlaneReqs;

        meetsLeadReqs = connection.IsLeadSideValid && RopeDartManager.Instance.IsLeadSide || connection.IsAnchorSideValid && !RopeDartManager.Instance.IsLeadSide;
        meetsSpinReqs = connection.IsDownSpinValid && RopeDartManager.Instance.IsDownSpin || connection.IsUpSpinValid && !RopeDartManager.Instance.IsDownSpin;
        // TODO: implement plane states
        // meetsPlaneReqs = connection.IsWallPlaneValid && RopeDartManager.Instance.IsWallPlane || connection.IsDarkPlaneValid && RopeDartManager.Instance.IsDarkPlane;

        return meetsLeadReqs && meetsSpinReqs; // && meetsPlaneReqs;
    }

    private void UseGraphConnection(BindingGraphData.BindingGraphConnection connection)
    {
        foreach (BindingStackElement nodeUnitCost in connection.NodeSequence)
        {
            BindingStackElement element = new BindingStackElement
            {
                NodeId = nodeUnitCost.NodeId,
                UnitCost = nodeUnitCost.UnitCost
            };
            CurrentBindings.Add(element);
        }

        if (connection.FlipsLeadAnchor) RopeDartManager.Instance.FlipLeadAnchor();
        if (connection.FlipsDownUp) RopeDartManager.Instance.FlipSpinDirection();
        if (connection.FlipsWallDark) RopeDartManager.Instance.FlipPlane();
    }

    public BindingGraphData.BindingGraphNode PeekBinding()
    {
        if (CurrentBindings.Count > 0)
        {
            BindingStackElement lastBinding = CurrentBindings[CurrentBindings.Count - 1];
            return BindingGraph.Nodes.Find(n => n.NodeId == lastBinding.NodeId);
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

    // Removes bindings from the stack until it finds a wrapped point and returns it
    // If no points are wrapped, all points are removed and null is returned
    // This should never return null since Idle is always a wrapped point
    public BindingGraphData.BindingGraphNode RevertToLastWrappedBinding()
    {
        while (CurrentBindings.Count > 0)
        {
            BindingStackElement lastBinding = CurrentBindings[CurrentBindings.Count - 1];

            if (lastBinding.NodeId.Equals("Wrap") || lastBinding.NodeId.Equals("Idle"))
            {
                return BindingGraph.Nodes.Find(n => n.NodeId == lastBinding.NodeId);
            }
            else
            {
                CurrentBindings.RemoveAt(CurrentBindings.Count - 1);
            }
        }

        return null;
    }

    public BindingGraphData.BindingGraphNode RevertToRootBinding()
    {
        CurrentBindings.Clear();
        CurrentBindings.Add(new BindingStackElement("Idle", 0));

        return null;
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

    // returns a string representing the current stack of bindings, with the root indicated by an asterisk (*)
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
