using UnityEngine;
using System.Collections.Generic;

public class BindPointStack : Singleton<BindPointStack>
{
    public Stack<BindingStackElement> WrappedBindings { get; private set; } = new Stack<BindingStackElement>();

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

    public List<BindPointObject> TryPushWrappedBinding(string bindingInput)
    {
        string newBindingId = "";

        if (WrappedBindings.Count > 0)
        {
            string lastBindingId = WrappedBindings.Peek().Point;
            BindingGraphData.BindingGraphNode lastBindingNode = BindingGraph.nodes.Find(n => n.nodeId == lastBindingId);

            string lastBindingConnections = lastBindingNode.connections != null ? string.Join(", ", lastBindingNode.connections.ConvertAll(c => $"{c.input} -> {c.nodeId} (Cost: {c.unitCost})")) : "No connections";

            BindingGraphData.BindingGraphConnection connection = lastBindingNode.connections.Find(c => c.input == bindingInput);
            if (connection == null)
            {
                Debug.LogWarning($"Cannot bind from lastBindingId {lastBindingId} with {bindingInput}. Transition not allowed.");
                return null;
            }

            if (GetRemainingUnits() < connection.unitCost)
            {
                Debug.LogWarning($"Cannot bind with {bindingInput}. Unit cost {connection.unitCost} exceeds remaining units {GetRemainingUnits()}.");
                return null;
            }

            BindingStackElement element = new BindingStackElement
            {
                Point = connection.nodeId,
                UnitCost = connection.unitCost
            };
            WrappedBindings.Push(element);

            newBindingId = connection.nodeId;
        }
        else
        {
            BindingStackElement element = new BindingStackElement
            {
                Point = bindingInput,
                UnitCost = 0
            };
            WrappedBindings.Push(element);

            newBindingId = bindingInput;
        }

        // get and return the list of BindPointObjects corresponding to the given nodeId, to get transform references
        List<BindPointObject> points = new List<BindPointObject>();
        foreach (BindPointID pointID in BindingToPointsLookup[newBindingId])
        {
            BindPointObject point = bindPointObjects[(int)pointID];
            points.Add(point);
        }

        return points;
    }

    public BindingGraphData.BindingGraphNode PopWrappedBinding()
    {
        if (WrappedBindings.Count > 0)
        {
            string nodeID = WrappedBindings.Pop().Point;
            return BindingGraph.nodes.Find(n => n.nodeId == nodeID);
        }
        return null;
    }

    public BindingGraphData.BindingGraphNode PeekWrappedBinding()
    {
        if (WrappedBindings.Count > 0)
        {
            string nodeID = WrappedBindings.Peek().Point;
            return BindingGraph.nodes.Find(n => n.nodeId == nodeID);
        }
        return null;
    }

    public void ClearWrappedBindings()
    {
        WrappedBindings.Clear();
    }

    public int GetTotalUnitCost()
    {
        int totalCost = 0;
        foreach (var point in WrappedBindings)
        {
            totalCost += point.UnitCost;
        }
        return totalCost;
    }

    public int GetRemainingUnits()
    {
        return MaxBindUnits - GetTotalUnitCost();
    }
}

[System.Serializable]
public struct BindingStackElement
{
    public string Point;
    public int UnitCost;
}
