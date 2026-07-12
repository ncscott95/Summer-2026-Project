using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RopeDartManagerNew : Singleton<RopeDartManagerNew>
{
    #pragma warning disable CS0162
    private const bool DEBUG_IsUsingRopeDartVisualManager = true;

    public RopeDartData Data;

    public RopeDartState CurrentState { get; private set; } = RopeDartState.Idle;
    public bool IsClockwise { get; private set; } = true;
    public float RawAngle { get; private set; } = 0f;

    private bool isTryingToSpin = false;
    private bool isRetrieveFromRight = true;

    void Start()
    {
        if (!DEBUG_IsUsingRopeDartVisualManager)
        {
            GetComponent<RopeDartVisualManager>().enabled = false;
        }

        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.Initialize();
        Reset();
        ResetRopeToHands();
    }

    public void StartSpin()
    {
        if (CurrentState != RopeDartState.Idle && CurrentState != RopeDartState.Retrieving && CurrentState != RopeDartState.Stalling)
            return;

        if (CurrentState == RopeDartState.Idle)
        {
            // default to a wheel plane down spin
            IsClockwise = true;
        }
        else if (CurrentState == RopeDartState.Stalling)
        {
            // flip spin direction when starting a new spin during stall
            IsClockwise = !IsClockwise;
        }

        List<BindPointObject> newOriginPoints = BindingStack.Instance.TryPushBinding("Spin " + (IsClockwise ? "Down" : "Up"));
        if (newOriginPoints != null && newOriginPoints.Count > 0)
        {
            Transform newOrigin = newOriginPoints[^1].transform;
            if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.SetOrigin(newOrigin);
        }
        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();

        CurrentState = RopeDartState.Spinning;
    }

    public void StopSpin()
    {
        if (CurrentState != RopeDartState.Spinning)
            return;

        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.SetOrigin(BindingStack.Instance.GetBindPointObject(BindingStack.Instance.RevertToLastWrappedBinding()?.nodeId).transform);
        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();

        CurrentState = RopeDartState.Stalling;
    }

    public void Cast()
    {
        if (CurrentState != RopeDartState.Spinning)
            return;
        
        BindingGraphData.BindingGraphNode currentBinding = BindingStack.Instance.PeekBinding();
        if (currentBinding == null || !currentBinding.canCast)
        {
            // TODO: maybe cause a failure state if the player tries to cast from a binding that doesn't allow it
            Debug.LogWarning($"Cannot cast: current binding {currentBinding?.nodeId ?? "null"} does not allow casting.");
            return;
        }

        Debug.Log("Casting");

        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.SetOrigin(BindingStack.Instance.GetBindPointObject(BindingStack.Instance.RevertToLastWrappedBinding()?.nodeId).transform);
        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();

        CurrentState = RopeDartState.Casting;
    }

    public void OnEndCast()
    {
        OnMaxLength();
    }

    public void TwineSimple()
    {
        // TODO: pick an elbow depending on which side the player is spinning on (lead/anchor)
        Twine("Bind Lead");
    }

    public void Twine(string bindingInput)
    {
        if (CurrentState != RopeDartState.Spinning)
            return;

        List<BindPointObject> points = BindingStack.Instance.TryPushBinding(bindingInput);
        if (points == null || points.Count == 0)
            return;

        BindPointObject point = points[points.Count - 1];
        Debug.Log($"Twining to {point.ID} at position {point.Position}");

        if (DEBUG_IsUsingRopeDartVisualManager)
        {
            RopeDartVisualManager.Instance.SetRadius(Vector3.Distance(point.Position, RopeDartVisualManager.Instance.CurrentOrigin.position));
            RopeDartVisualManager.Instance.SetOrigin(point.transform);
        }

        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();

        if (RopeDartVisualManager.Instance.CurrentRadius == 0) Reset();
    }

    public void TryStartWrap()
    {
        BindingGraphData.BindingGraphNode currentBinding = BindingStack.Instance.PeekBinding();

        if (currentBinding == null)
        {
            Debug.LogWarning("Cannot start wrap: no current binding.");
            return;
        }

        if (!currentBinding.canWrap)
        {
            Debug.LogWarning($"Cannot start wrap: current binding {currentBinding.nodeId} does not allow wrapping.");
            return;
        }

        HandleWrapBuff(currentBinding.nodeId);

        BindingStack.Instance.MarkCurrentBindingAsWrapped();

        if (isTryingToSpin)
        {
            // keep spinning from lead hand
            // TODO: there's probably a simplification here
            if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.ResetOriginAndRadius();
            BindingStack.Instance.TryPushBinding("Spin " + (IsClockwise ? "Down" : "Up"));
            UpdateRopeRenderer();
            RopeDartStatusUI.Instance.UpdateStatusUI();
        }
    }

    // TODO: this should probably be moved out to a separate class eventually
    private void HandleWrapBuff(string bindingId)
    {
        switch (bindingId)
        {
            case "Dragon":
                Debug.Log("Starting wrap buff for Dragon");
                break;
            case "Dark Dragon":
                Debug.Log("Starting wrap buff for Dark Dragon");
                break;
            case "Scorpion":
                Debug.Log("Starting wrap buff for Scorpion");
                break;
            case "Dark Scorpion":
                Debug.Log("Starting wrap buff for Dark Scorpion");
                break;
            default:
                Debug.LogWarning($"No wrap buff defined for binding {bindingId}");
                break;
        }
    }

    public void EndWrap()
    {
        // TODO: unsure what should happen if you release a wrap while still spinning
        BindingStack.Instance.UnmarkOutermostWrappedBinding();
        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    public void ShiftPlane(Vector2 direction)
    {
        // TODO
    }

    public void OnMaxLength()
    {
        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.SetVelocity(Vector3.zero);
        CurrentState = RopeDartState.Extended;
    }

    public void Retrieve()
    {
        if (CurrentState != RopeDartState.Extended && CurrentState != RopeDartState.Casting)
            return;

        CurrentState = RopeDartState.Retrieving;

        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.OnRetrieve();
    }

    public void OnEndRetrieve()
    {
        // IsClockwise = RopeDartVisualManager.Instance.CurrentDirection.x < 0;

        if (isTryingToSpin) StartSpin();
        else Reset();
    }

    public void Reset()
    {
        // TODO: maybe temp for testing twining
        // currentOrigin = startOrigin;
        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.OnReset();
        CurrentState = RopeDartState.Idle;
    }

    public void FailCombo()
    {
        Reset();
    }

    public void CollideWithGround()
    {
        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.OnCollideWithGround();
        CurrentState = RopeDartState.Extended;
    }

    public void ToggleTryingToSpin(bool value)
    {
        isTryingToSpin = value;
    }

    private void ResetRopeToHands()
    {
        BindingStack.Instance.ClearBindings();
        BindingStack.Instance.TryPushBinding("Idle");
        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    private void UpdateRopeRenderer()
    {
        List<BindPointObject> points = BindingStack.Instance.GetAllBindObjects();
        Debug.Log($"Current bindings: {BindingStack.Instance.CurrentBindingsToString()}");
        Debug.Log($"Updating rope renderer with points: {string.Join(", ", points.Select(p => p.ID))}");
        RopeRenderer.Instance.Reset();
        RopeRenderer.Instance.AddPointsBeforeHead(points);
    }
}

public enum RopeDartStateNew
{
    Idle,
    Spinning,
    Stalling,
    Casting,
    Extended,
    Retrieving
}
