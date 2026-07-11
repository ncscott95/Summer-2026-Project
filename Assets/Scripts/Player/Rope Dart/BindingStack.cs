using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BindingStack : Singleton<BindingStack>
{
    public List<BindingStackElement> CurrentBindings { get; private set; } = new List<BindingStackElement>();

    // Units are used only for calculating behind-the-scenes "costs" of wrapping and binding.
    // These are not the same as physical length, but will be directly proportional to it.
    // I am working on the relatively arbitrary assumption that a normal swinging length is 3.
    public const int MaxBindUnits = 15;

    public BindingGraphData BindingGraph { get; private set; }

    // This dictionary maps binding names to the corresponding BindPointIDs that should be used for that binding.
    // These are the points that the rope physically travels through when the player usese that binding.
    public readonly Dictionary<string, BindPointID[]> BindingToPointsLookup = new Dictionary<string, BindPointID[]>
    {
        { "Idle",                           new BindPointID[] { BindPointID.AnchorHand, BindPointID.LeadHand } },
        { "Lead Down Spin",                 new BindPointID[] { BindPointID.LeadHand }},
        { "Lead Up Spin",                   new BindPointID[] { BindPointID.LeadHand }},
        { "Anchor Down Spin",               new BindPointID[] { BindPointID.LeadHand }},
        { "Anchor Up Spin",                 new BindPointID[] { BindPointID.LeadHand }},
        { "Lead Elbow",                     new BindPointID[] { BindPointID.LeadElbow }},
        { "Anchor Elbow",                   new BindPointID[] { BindPointID.AnchorElbow }},
        { "Dragon",                         new BindPointID[] { BindPointID.LeadArmpit, BindPointID.AnchorShoulder }},
        { "Dark Dragon",                    new BindPointID[] { BindPointID.AnchorArmpit, BindPointID.LeadShoulder }},
        { "Scorpion",                       new BindPointID[] { BindPointID.LeadShoulder, BindPointID.AnchorArmpit }},
        { "Dark Scorpion",                  new BindPointID[] { BindPointID.AnchorShoulder, BindPointID.LeadArmpit }},
        { "Lead Thigh Saddle",              new BindPointID[] { BindPointID.LeadKnee }},
        { "Anchor Thigh Holster",           new BindPointID[] { BindPointID.AnchorKnee }},
    };

    [SerializeField] private List<BindPointObject> bindPointObjects = new List<BindPointObject>(13);

    public override void Awake()
    {
        base.Awake();

        BindingGraph = JsonUtility.FromJson<BindingGraphData>(Resources.Load<TextAsset>("BindingGraph").text);
        Debug.Log(BindingGraph.nodes.Count);
    }

    public List<BindPointObject> TryPushBinding(string bindingInput)
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
        }

        // return the list of BindPointObjects corresponding to the given nodeId, can be used to get transform references
        List<BindPointObject> points = new List<BindPointObject>();
        foreach (BindPointID pointID in BindingToPointsLookup[newBindingId])
        {
            BindPointObject point = bindPointObjects[(int)pointID];
            points.Add(point);
        }

        Debug.Log($"Pushed binding: {newBindingId}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
        return points;
    }

    public BindingGraphData.BindingGraphNode PopBinding()
    {
        if (CurrentBindings.Count > 0)
        {
            string nodeID = CurrentBindings[CurrentBindings.Count - 1].Point;
            CurrentBindings.RemoveAt(CurrentBindings.Count - 1);

            Debug.Log($"Popped binding: {nodeID}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
            return BindingGraph.nodes.Find(n => n.nodeId == nodeID);
        }
        return null;
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

    public void MarkCurrentBindingAsWrapped()
    {
        if (CurrentBindings.Count > 0)
        {
            int lastIndex = CurrentBindings.Count - 1;
            BindingStackElement lastBinding = CurrentBindings[lastIndex];
            lastBinding.IsWrapPoint = true;
            CurrentBindings[lastIndex] = lastBinding;
            Debug.Log($"Set binding as wrapped: {CurrentBindings[CurrentBindings.Count - 1].Point}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
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
                Debug.Log($"Unmarked outermost wrapped binding: {binding.Point}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
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
                Debug.Log($"Reverted to last wrapped binding: {lastBinding.Point}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
                return BindingGraph.nodes.Find(n => n.nodeId == lastBinding.Point);
            }
            else if (lastBinding.IsRootPoint)
            {
                Debug.Log($"Reverted to root binding: {lastBinding.Point}. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
                return BindingGraph.nodes.Find(n => n.nodeId == lastBinding.Point);
            }
            else
            {
                CurrentBindings.RemoveAt(CurrentBindings.Count - 1);
            }
        }

        Debug.LogWarning($"No wrapped bindings to revert to. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
        return null;
    }

    public void ClearBindings()
    {
        CurrentBindings.Clear();
        Debug.Log($"Clearing all bindings. Current stack: {CurrentBindingsToString()}. Remaining units: {GetRemainingUnits()}");
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

    public List<BindPointObject> GetAllBindObjects()
    {
        List<BindPointObject> points = new List<BindPointObject>();
        foreach (var binding in CurrentBindings)
        {
            if (BindingToPointsLookup.TryGetValue(binding.Point, out BindPointID[] pointIDs))
            {
                foreach (BindPointID pointID in pointIDs)
                {
                    BindPointObject point = bindPointObjects[(int)pointID];
                    points.Add(point);
                }
            }
        }
        return points;
    }
    
    public BindPointObject GetBindPointObject(string bindingId)
    {
        if (BindingToPointsLookup.TryGetValue(bindingId, out BindPointID[] pointIDs))
        {
            // return the last BindPointObject for the given bindingId
            return bindPointObjects[(int)pointIDs[pointIDs.Length - 1]];
        }
        return null;
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
