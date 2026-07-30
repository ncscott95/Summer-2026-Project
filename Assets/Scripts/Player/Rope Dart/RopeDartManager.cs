using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RopeDartManager : Singleton<RopeDartManager>
{
    private const float BaseSpinSpeed = 360f;
    private const float SpiralDuration = 1.5f;

    public RopeDartState CurrentState { get; private set; } = RopeDartState.Idle;
    public bool IsLeadSide { get; private set; } = true;
    public bool IsFrontPlane { get; private set; } = true;
    public bool IsClockwise { get; private set; } = true;

    // down spin = CW on lead side, CCW on anchor side
    // up spin   = CCW on lead side, CW on anchor side
    public bool IsDownSpin => IsClockwise == IsLeadSide;

    // 0 = up, 90 = right, 180 = down, 270 = left
    public float RawAngle { get; private set; } = 0f;

    [SerializeField] private RopeDartVisualManager _ropeDartVisualManager;

    private float _debugTimer = 0f;
    private float _debugCastDuration = 0.5f;
    
    private bool _isSpiraling = false;
    private float _spiralTimer = 0f;
    private float _spiralStartAngle = 0f;
    private bool _isStalled = false;

    private bool _isLastCastRight = true;

    void Start()
    {
        Reset();
    }

    void Update()
    {
        if (CurrentState == RopeDartState.Spinning)
        {
            float oldAngle = RawAngle;

            if (!_isSpiraling && !_isStalled)
            {
                RawAngle += (IsClockwise ? 1 : -1) * BaseSpinSpeed * Time.deltaTime;
                RawAngle = Mathf.Repeat(RawAngle, 360f);
            }
            else
            {
                _spiralTimer += Time.deltaTime;
                _spiralTimer = Mathf.Min(_spiralTimer, SpiralDuration);
                float degreesTraversed = 360f * (3f - Mathf.Sqrt(9f - 6f * _spiralTimer));
                float directionMultiplier = IsClockwise ? 1f : -1f;
                RawAngle = Mathf.Repeat(_spiralStartAngle + directionMultiplier * degreesTraversed, 360f);

                if (_spiralTimer >= SpiralDuration)
                {
                    _isSpiraling = false;
                    _isStalled = true;
                }
            }

            // detection for additional "beats"
            if (oldAngle < 180f && RawAngle >= 180f)
            {
                // Debug.Log("Spin beat");
                BindingGraphNode currentBinding = BindingStack.Instance.PeekBinding();
                if (currentBinding != null && currentBinding.DoesDecay)
                {
                    BindingStack.Instance.UpdateCurrentBindingUnitCost(1);
                }
            }
        }
        else if (CurrentState == RopeDartState.Casting)
        {
            // TODO: placeholder timer, replace with logic for detecting when the dart has reached max length
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= _debugCastDuration)
            {
                _debugTimer = 0f;
                OnEndCast();
            }
        }
        else if (CurrentState == RopeDartState.Retrieving)
        {
            // TODO: placeholder timer, replace with logic for detecting when the dart has reached max length
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= _debugCastDuration)
            {
                _debugTimer = 0f;
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

        BindingGraphConnection bindingConnection = BindingStack.Instance.TryPushBinding("Spin");
        _ropeDartVisualManager.UpdateVisuals("Spin" + (IsLeadSide ? "_Lead" : "_Anchor"));
    
        _isSpiraling = false;
        _isStalled = false;
        // TODO: not sure if this is a good idea or not
        RawAngle = 180f;
        RopeDartStatusUI.Instance.UpdateStatusUI();

        CurrentState = RopeDartState.Spinning;
    }

    public void Cast()
    {
        if (CurrentState != RopeDartState.Spinning)
            return;

        BindingGraphConnection bindingConnection = BindingStack.Instance.TryPushBinding("Cast");
        if (bindingConnection == null)
            return;

        Debug.Log("Casting");

        // BindingGraphNode newOriginNode = BindingStack.Instance.RevertToLastWrappedBinding();
        // assumes that casts always unwrap any wraps and revert to root
        BindingGraphNode newOriginNode = BindingStack.Instance.RevertToRootBinding();

        _isLastCastRight = IsClockwise ? RawAngle > 315 || RawAngle > 0f && RawAngle < 135f : RawAngle > 45f && RawAngle < 225f;

        _ropeDartVisualManager.UpdateVisuals("Cast_" + (_isLastCastRight ? "East" : "West"));
        RopeDartStatusUI.Instance.UpdateStatusUI();

        IsLeadSide = IsFrontPlane ? _isLastCastRight : !_isLastCastRight;
        CurrentState = RopeDartState.Casting;
        _debugTimer = 0f;
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

        BindingGraphConnection bindingConnection = BindingStack.Instance.TryPushBinding(bindingInput);
        if (bindingConnection == null)
            return;

        // TODO: temp, always pop the binding before this one to get rid of the extra spin binding
        // BindingStack.Instance.RemoveBindingAtIndex(BindingStack.Instance.CurrentBindings.Count - 2);

        if (BindingStack.Instance.PeekBinding().DoesDecay)
        {
            _isSpiraling = true;
            _spiralTimer = 0f;
            _spiralStartAngle = 180f;
        }

        _ropeDartVisualManager.UpdateVisuals(bindingConnection);
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    public void TryStartWrap()
    {
        BindingGraphNode currentBinding = BindingStack.Instance.PeekBinding();

        if (currentBinding == null)
        {
            Debug.LogWarning("Cannot start wrap: no current binding.");
            return;
        }

        BindingGraphConnection bindingConnection = BindingStack.Instance.TryPushBinding("Wrap");
        if (bindingConnection == null)
            return;

        HandleWrapBuff(currentBinding.NodeId);
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

    // unused since wraps have been changed from hold to fire-and-forget
    public void EndWrap()
    {
        // TODO: unsure what should happen if you release a wrap while still spinning
        RopeDartStatusUI.Instance.UpdateStatusUI();
    }

    public void ShiftPlane(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle <= 45f || angle > 315f)
        {
            // right
            IsLeadSide = IsFrontPlane;
        }
        else if (angle > 45f && angle <= 135f)
        {
            // up
            if (IsFrontPlane) IsLeadSide = !IsLeadSide;
            IsFrontPlane = false;
        }
        else if (angle > 135f && angle <= 225f)
        {
            // left
            IsLeadSide = !IsFrontPlane;
        }
        else if (angle > 225f && angle <= 315f)
        {
            // down
            if (!IsFrontPlane) IsLeadSide = !IsLeadSide;
            IsFrontPlane = true;
        }

        _ropeDartVisualManager.UpdateVisuals("Spin" + (IsLeadSide ? "_Lead" : "_Anchor"));
    }

    public void OnMaxLength()
    {
        CurrentState = RopeDartState.Extended;
    }

    public void Retrieve()
    {
        if (CurrentState != RopeDartState.Extended && CurrentState != RopeDartState.Casting)
            return;

        IsClockwise = _isLastCastRight;

        CurrentState = RopeDartState.Retrieving;
    }

    public void OnEndRetrieve()
    {
        Reset();
    }

    public void Reset()
    {
        BindingStack.Instance.ClearBindings();
        BindingStack.Instance.TryPushBinding("Idle");
        RopeDartStatusUI.Instance.UpdateStatusUI();
        CurrentState = RopeDartState.Idle;
        RawAngle = 180f;
    }

    public void FailCombo()
    {
        Reset();
    }

    public void CollideWithGround()
    {
        CurrentState = RopeDartState.Extended;
    }

    public void FlipLeadAnchor()
    {
        IsLeadSide = !IsLeadSide;
        IsFrontPlane = !IsFrontPlane;
        // _ropeDartVisualManager.UpdateVisuals("Spin" + (IsLeadSide ? "_Lead" : "_Anchor"));
    }

    public void FlipSpinDirection()
    {
        // assumes that this is only used for turns
        // a stall would also flip spin direction, but would not flip lead/anchor side
        IsLeadSide = !IsLeadSide;
        // _ropeDartVisualManager.UpdateVisuals("Spin" + (IsLeadSide ? "_Lead" : "_Anchor"));
    }

    public void FlipPlane()
    {
        IsFrontPlane = !IsFrontPlane;
        // _ropeDartVisualManager.UpdateVisuals("Spin" + (IsLeadSide ? "_Lead" : "_Anchor"));
    }
}

public enum RopeDartState
{
    Idle,
    Spinning,
    Casting,
    Extended,
    Retrieving,
    Stalling
}
