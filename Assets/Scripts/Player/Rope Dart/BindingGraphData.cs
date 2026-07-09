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
        public bool canCast;
        public bool canWrap;
        public bool canTurn;
        public List<BindingGraphConnection> connections;
    }

    [Serializable]
    public class BindingGraphConnection
    {
        public string input;
        public string nodeId;
        public int unitCost;
    }

    public List<BindingGraphNode> nodes = new List<BindingGraphNode>();
}
