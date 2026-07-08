using UnityEngine;
using System.Collections.Generic;

public class BindPointStack : Singleton<BindPointStack>
{
    public Stack<BindPointStackElement> WrappedPoints { get; private set; } = new Stack<BindPointStackElement>();

    // Units are used only for calculating behind-the-scenes "costs" of wrapping and binding.
    // These are not the same as physical length, but will be directly proportional to it.
    // I am working on the relatively arbitrary assumption that a normal swinging length is 3.
    public const int MaxBindUnits = 15;

    // Represents the cost of transitioning from one BindPointID to another. 
    // The first index is the starting point, the second index is the ending point.
    // For example, UnitCostGrid[(int)BindPointID.LeadHand, (int)BindPointID.AnchorFoot] gives 
    // the cost of binding from the lead hand to the anchor foot.
    // A cost of null indicates that the transition is not allowed.
    public readonly int?[,] UnitCostGrid = new int?[14, 14]
    {
        //   R    LH    LS    LA    LE    LK    LF    AH    AS    AA    AE    AK    AF    N
        { null,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 }, // Root

        { null, null,    1,    1,    1,    1,    2, null,    1,    1,    1,    1,    2, null }, // LeadHand
        { null, null, null, null, null, null, null,    1, null,    1, null, null, null, null }, // LeadShoulder
        { null, null, null, null, null, null, null,    1,    1, null, null, null, null, null }, // LeadArmpit
        { null, null, null, null,    1, null, null, null, null, null, null, null, null, null }, // LeadElbow
        { null, null, null, null, null, null, null, null, null, null, null, null, null, null }, // LeadKnee
        { null, null, null, null, null, null, null, null, null, null, null, null, null, null }, // LeadFoot

        { null,    1, null, null, null, null, null, null, null, null, null, null, null, null }, // AnchorHand
        { null, null, null,    1, null, null, null,    1, null, null, null, null, null, null }, // AnchorShoulder
        { null, null,    1, null, null, null, null,    1, null, null, null, null, null, null }, // AnchorArmpit
        { null, null, null, null, null, null, null, null, null, null,    1, null, null, null }, // AnchorElbow
        { null, null, null, null, null, null, null, null, null, null, null, null, null, null }, // AnchorKnee
        { null, null, null, null, null, null, null, null, null, null, null, null, null, null }, // AnchorFoot

        { null, null, null, null, null, null, null, null, null, null, null, null, null, null }  // Neck
    };

    [SerializeField] private List<BindPointObject> bindPointObjects = new List<BindPointObject>(13);

    public BindPointObject TryPushWrappedPoint(BindPointID pointID)
    {
        Debug.Log($"Trying to push {pointID}, count: {WrappedPoints.Count}");

        BindPointObject point = bindPointObjects[(int)pointID];
        if (point == null)
        {
            Debug.LogWarning($"Bind point {pointID} not found. Make sure it is registered correctly.");
            return null;
        }

        if (WrappedPoints.Count == 0)
        {
            BindPointStackElement nullElement = new BindPointStackElement
            {
                Point = bindPointObjects[(int)BindPointID.Root],
                UnitCost = 0
            };
            WrappedPoints.Push(nullElement);
        }

        BindPointObject lastPoint = WrappedPoints.Count > 0 ? WrappedPoints.Peek().Point : null;
        Debug.Log($"Attempting to wrap from {lastPoint.ID} to {point.ID}");

        int? unitCost = UnitCostGrid[(int)lastPoint.ID, (int)point.ID];
        if (unitCost == null)
        {
            Debug.LogWarning($"Cannot wrap from {lastPoint.ID} to {point.ID}. Transition not allowed.");
            return null;
        }

        if (GetRemainingUnits() < unitCost.Value)
        {
            Debug.LogWarning($"Cannot wrap to {point.ID}. Unit cost {unitCost.Value} exceeds remaining units {GetRemainingUnits()}.");
            return null;
        }

        BindPointStackElement element = new BindPointStackElement
        {
            Point = point,
            UnitCost = unitCost.Value
        };
        WrappedPoints.Push(element);

        return point;
    }

    public BindPointObject PopWrappedPoint()
    {
        if (WrappedPoints.Count > 0)
        {
            return WrappedPoints.Pop().Point;
        }
        return null;
    }

    public void ClearWrappedPoints()
    {
        WrappedPoints.Clear();
    }

    public BindPointObject PeekWrappedPoint()
    {
        if (WrappedPoints.Count > 0)
        {
            return WrappedPoints.Peek().Point;
        }
        return null;
    }

    public int GetTotalUnitCost()
    {
        int totalCost = 0;
        foreach (var point in WrappedPoints)
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
public struct BindPointStackElement
{
    public BindPointObject Point;
    public int UnitCost;
}
