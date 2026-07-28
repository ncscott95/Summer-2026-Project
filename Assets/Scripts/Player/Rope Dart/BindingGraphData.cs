using System;
using System.Collections.Generic;

[Serializable]
public class BindingGraphData
{
    [Serializable]
    public class BindingGraphNode
    {
        public string NodeId;
        public bool IsStable;
        public bool DoesDecay;
        public bool CanCast;
        public bool CanTurn;
        public List<string> BindPoints;
        public List<BindingGraphConnection> Connections;
    }

    [Serializable]
    public class BindingGraphConnection
    {
        public string NodeId;
        public string Input;
        public int UnitCost;
        public string Animation;
    }

    public List<BindingGraphNode> Nodes = new List<BindingGraphNode>();
}
