using UnityEngine;

public class RopeDartManager : Singleton<RopeDartManager>
{
    private const int MaxTargetsHit = 1;
    public int TargetsHit { get; private set; } = 0;
    public bool IsLeadSide { get; private set; } = true;

    void Start()
    {
        Reset();
    }

    public void OnCastEnd()
    {
        BindingStack.Instance.TryPushBinding("(Nothing)");
    }

    public void OnDragonEnd()
    {
        BindingStack.Instance.TryPushBinding("(Nothing)");
    }

    public void OnNeckEnd()
    {
        BindingStack.Instance.TryPushBinding("(Nothing)");
    }

    public void OnRetrieveEnd()
    {
        BindingStack.Instance.TryPushBinding("(Nothing)");
    }

    public void Reset()
    {
        BindingStack.Instance.ClearBindings();
        BindingStack.Instance.TryPushBinding("Idle");
        BindingStack.Instance.TryPushBinding("(Nothing)");
        RopeDartStatusUI.Instance.UpdateStatusUI();
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
