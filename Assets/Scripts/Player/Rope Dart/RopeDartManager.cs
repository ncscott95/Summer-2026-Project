using UnityEngine;

public class RopeDartManager : Singleton<RopeDartManager>
{
    private const int MaxTargetsHit = 1;
    public int TargetsHit { get; private set; } = 0;
    public bool IsLeadSide { get; private set; } = true;

    void Start()
    {
        Reset();
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    public void Reset()
    {
        BindingStack.Instance.ClearBindings();
        BindingStack.Instance.TryPushBinding("Idle");
        BindingStack.Instance.TryPushBinding("(Start)");
    }

    public void FlipLeadAnchor()
    {
        IsLeadSide = !IsLeadSide;
    }

    public void ResetHitCount()
    {
        TargetsHit = 0;
    }

    public bool TryHitTarget()
    {
        if (TargetsHit >= MaxTargetsHit) return false;

        TargetsHit++;
        return true;
    }
}
