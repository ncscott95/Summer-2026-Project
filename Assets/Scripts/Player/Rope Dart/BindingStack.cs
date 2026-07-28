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

    public BindingGraphData.BindingGraphConnection TryPushBindingNew(string bindingInput)
    {
        string newBindingId = "";

        if (CurrentBindings.Count > 0)
        {
            string lastBindingId = CurrentBindings[CurrentBindings.Count - 1].Point;
            BindingGraphData.BindingGraphNode lastBindingNode = BindingGraph.nodes.Find(n => n.nodeId == lastBindingId);

            BindingGraphData.BindingGraphConnection connection = lastBindingNode.connections.Find(c => c.input == bindingInput);
            if (connection == null)
            {
                Debug.LogWarning($"Cannot bind from {lastBindingId} with {bindingInput}. Transition not allowed.");
                return null;
            }

            if (GetRemainingUnits() < connection.unitCost)
            {
                Debug.LogWarning($"Cannot bind from {lastBindingId} with {bindingInput}. Unit cost {connection.unitCost} exceeds remaining units {GetRemainingUnits()}.");
                return null;
            }

            BindingStackElement element = new BindingStackElement
            {
                Point = connection.nodeId,
                UnitCost = connection.unitCost,
                IsWrapPoint = false,
                IsRootPoint = false
            };
            CurrentBindings.Add(element);

            newBindingId = connection.nodeId;

            return connection;
        }
        else
        {
            BindingStackElement element = new BindingStackElement
            {
                Point = bindingInput,
                UnitCost = 0,
                IsWrapPoint = false,
                // mark the first binding as a root point, which behaves as a wrap point that can't be unmarked
                IsRootPoint = true
            };
            CurrentBindings.Add(element);

            newBindingId = bindingInput;

            return null;
        }

        // BindingGraphData.BindingGraphNode newBindingNode = BindingGraph.nodes.Find(n => n.nodeId == newBindingId);

        // return newBindingNode;
    }

    public BindingGraphData.BindingGraphNode PeekBinding()
    {
        if (CurrentBindings.Count > 0)
        {
            string nodeID = CurrentBindings[CurrentBindings.Count - 1].Point;
            return BindingGraph.nodes.Find(n => n.nodeId == nodeID);
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

    public void MarkCurrentBindingAsWrapped()
    {
        if (CurrentBindings.Count > 0)
        {
            int lastIndex = CurrentBindings.Count - 1;
            BindingStackElement lastBinding = CurrentBindings[lastIndex];
            lastBinding.IsWrapPoint = true;
            CurrentBindings[lastIndex] = lastBinding;
        }
    }

    public void UnmarkOutermostWrappedBinding()
    {
        for (int i = CurrentBindings.Count - 1; i >= 0; i--)
        {
            if (CurrentBindings[i].IsWrapPoint)
            {
                BindingStackElement binding = CurrentBindings[i];
                binding.IsWrapPoint = false;
                CurrentBindings[i] = binding;
                return;
            }
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

            if (lastBinding.IsWrapPoint)
            {
                return BindingGraph.nodes.Find(n => n.nodeId == lastBinding.Point);
            }
            else if (lastBinding.IsRootPoint)
            {
                return BindingGraph.nodes.Find(n => n.nodeId == lastBinding.Point);
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
        while (CurrentBindings.Count > 0)
        {
            BindingStackElement lastBinding = CurrentBindings[CurrentBindings.Count - 1];

            if (lastBinding.IsRootPoint)
            {
                return BindingGraph.nodes.Find(n => n.nodeId == lastBinding.Point);
            }
            else
            {
                CurrentBindings.RemoveAt(CurrentBindings.Count - 1);
            }
        }

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

    // returns a string representing the current stack of bindings, with wrap points indicated by an asterisk (*)
    public string CurrentBindingsToString()
    {
        return string.Join(", ", CurrentBindings.Select(b => b.IsWrapPoint ? $"{b.Point}*" : b.IsRootPoint ? $"{b.Point}**" : b.Point));
    }
}

[System.Serializable]
public struct BindingStackElement
{
    public string Point;
    public int UnitCost;
    public bool IsWrapPoint;
    public bool IsRootPoint;
}
