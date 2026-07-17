using System;
using System.Collections.Generic;

[Serializable]
public class BindingGraphData
{
    [Serializable]
    public class BindingGraphNode
    {
        public string nodeId;
        public bool isStable;
        public bool doesDecay;
        public bool canCast;
        public bool canTurn;
        public List<string> bindPoints;
        public List<BindingGraphConnection> connections;
    }

    [Serializable]
    public class BindingGraphConnection
    {
        public string nodeId;
        public string input;
        public int unitCost;
    }

    public List<BindingGraphNode> nodes = new List<BindingGraphNode>();
}
