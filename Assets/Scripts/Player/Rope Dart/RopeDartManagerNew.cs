using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RopeDartManagerNew : Singleton<RopeDartManagerNew>
{
    // disable warnings for unreachable code
    #pragma warning disable CS0162
    private const bool DEBUG_IsUsingRopeDartVisualManager = false;

    public RopeDartData Data;

    public RopeDartState CurrentState { get; private set; } = RopeDartState.Idle;
    public bool IsClockwise { get; private set; } = true;
    public float RawAngle { get; private set; } = 0f;

    private bool isTryingToSpin = false;
    private float debugTimer = 0f;
    private float debugCastDuration = 0.5f;

    void Start()
    {
        if (!DEBUG_IsUsingRopeDartVisualManager)
        {
            RopeDartVisualManager.Instance.enabled = false;
            RopeRenderer.Instance.enabled = false;
        }

        if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.Initialize();
        Reset();
        ResetRopeToHands();
    }

    void Update()
    {
        if (CurrentState == RopeDartState.Spinning)
        {
            float oldAngle = RawAngle;
            // TODO: should be scaled by current spin radius
            RawAngle += (IsClockwise ? 1 : -1) * Data.SpinLinearSpeed * Time.deltaTime;
            RawAngle = Mathf.Repeat(RawAngle, 360f);

            // detection for additional "beats"
            if (oldAngle < 180f && RawAngle >= 180f)
            {
                Debug.Log("Spin beat");
            }
        }
        else if (CurrentState == RopeDartState.Casting)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugCastDuration)
            {
                debugTimer = 0f;
                OnEndCast();
            }
        }
        else if (CurrentState == RopeDartState.Retrieving)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugCastDuration)
            {
                debugTimer = 0f;
                OnEndRetrieve();
            }
        }
    }

    public void StartSpin()
    {
        if (CurrentState != RopeDartState.Idle && CurrentState != RopeDartState.Retrieving)
            return;

        // TODO: this won't work since you can get stuck in a permanent state of idle, need to make sure you can cast or reverse spin from this state to release slack
        // if (BindingStack.Instance.GetRemainingUnits() <= 0)
        // {
        //     // out of slack units, cannot spin
        //     // this is "holding" the dart in place
        //     Debug.LogWarning("Cannot start spin: no remaining units.");
        //     return;
        // }

        if (CurrentState == RopeDartState.Idle)
        {
            // default to a wheel plane down spin
            IsClockwise = true;
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

        BindingGraphData.BindingGraphNode newOriginNode = BindingStack.Instance.RevertToLastWrappedBinding();
        if (newOriginNode != null)
        {
            Transform newOrigin = BindingStack.Instance.GetBindPointObject(newOriginNode.nodeId).transform;
            if (DEBUG_IsUsingRopeDartVisualManager) RopeDartVisualManager.Instance.SetOrigin(newOrigin);
        }

        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();

        CurrentState = RopeDartState.Casting;
        debugTimer = 0f;
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
        if (DEBUG_IsUsingRopeDartVisualManager)
        {
            RopeDartVisualManager.Instance.SetRadius(Vector3.Distance(point.Position, RopeDartVisualManager.Instance.CurrentOrigin.position));
            RopeDartVisualManager.Instance.SetOrigin(point.transform);
        }

        UpdateRopeRenderer();
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    public void TryStartWrap()
    {
        BindingGraphData.BindingGraphNode currentBinding = BindingStack.Instance.PeekBinding();

        if (currentBinding == null)
        {
            Debug.LogWarning("Cannot start wrap: no current binding.");
            return;
        }

        // TODO: replaced canWrap with a specific binding for a wrap, search if connections has a binding with wrap
        // if (!currentBinding.canWrap)
        // {
        //     Debug.LogWarning($"Cannot start wrap: current binding {currentBinding.nodeId} does not allow wrapping.");
        //     return;
        // }

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
        
        IsClockwise = RawAngle > 0f && RawAngle < 180f;

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
        RawAngle = 180f;
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
        RopeRenderer.Instance.Reset();
        RopeRenderer.Instance.AddPointsBeforeHead(points);
    }
}

public enum RopeDartStateNew
{
    Idle,
    Spinning,
    Casting,
    Extended,
    Retrieving
}
