using System;
using System.Collections.Generic;

[Serializable]
public class BindingGraphData
{
    public List<BindingGraphNode> Nodes = new List<BindingGraphNode>();
}

[Serializable]
public class BindingGraphNode
{
    public string NodeId;
    public bool DoesDecay;
    public List<BindingGraphConnection> Connections;
}

[Serializable]
public class BindingGraphConnection
{
    public string Nickname;
    public string Input;

    // Requirements
    public int UnitCost;
    public bool IsLeadSideValid;
    public bool IsAnchorSideValid;
    public bool IsDownSpinValid;
    public bool IsUpSpinValid;
    public bool IsWallPlaneValid;
    public bool IsDarkPlaneValid;

    // Outcomes
    public bool FlipsLeadAnchor;
    public bool FlipsDownUp;
    public bool FlipsWallDark;
    public List<BindingStackElement> NodeSequence;
    public string Animation;
}
