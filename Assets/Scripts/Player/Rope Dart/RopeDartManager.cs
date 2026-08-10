using UnityEngine;

public class RopeDartManager : Singleton<RopeDartManager>
{
    private const float BaseSpinSpeed = 360f;
    private const float SpiralDuration = 1.5f;

    public RopeDartState CurrentState { get; private set; } = RopeDartState.Idle;
    public bool IsLeadSide { get; private set; } = true;
    public bool IsFrontPlane { get; private set; } = true;
    public bool IsClockwise { get; private set; } = true;
    public bool IsLastCastEast { get; private set; } = true;
    public bool IsCoiling => CurrentState == RopeDartState.Coiling || CurrentState == RopeDartState.Uncoiling;
    public bool IsStalled => CurrentState == RopeDartState.Stalled;

    // facing east = lead side on front plane, anchor side on back plane
    // facing west = anchor side on front plane, lead side on back plane
    public bool IsFacingEast => IsLeadSide == IsFrontPlane;

    // down spin = CW facing east, CCW facing west
    // up spin   = CCW facing east, CW facing west
    public bool IsDownSpin => IsClockwise == IsFacingEast;
    public bool IsWallPlane => BindingStack.Instance.GetIsWallPlane();

    // 0 = up, 90 = right, 180 = down, 270 = left
    public float RawAngle { get; private set; } = 0f;

    [SerializeField] private RopeDartVisualManager _ropeDartVisualManager;

    private float _debugTimer = 0f;
    private float _debugCastDuration = 0.5f;

    private float _coilTimer = 0f;
    private float _coilStartAngle = 0f;
    private string _coilNodeType = "";

    void Start()
    {
        Reset();
    }

    void Update()
    {
        float oldAngle = RawAngle;

        if (CurrentState == RopeDartState.Spinning)
        {
            RawAngle += (IsClockwise ? 1 : -1) * BaseSpinSpeed * Time.deltaTime;
            RawAngle = Mathf.Repeat(RawAngle, 360f);
        }
        else if (CurrentState == RopeDartState.Coiling)
        {
            _coilTimer += Time.deltaTime;
            _coilTimer = Mathf.Min(_coilTimer, SpiralDuration);
            float degreesTraversed = 360f * (3f - Mathf.Sqrt(9f - 6f * _coilTimer));
            float directionMultiplier = IsClockwise ? 1f : -1f;
            RawAngle = Mathf.Repeat(_coilStartAngle + directionMultiplier * degreesTraversed, 360f);

            if ((IsClockwise && oldAngle < 180f && RawAngle >= 180f) || (!IsClockwise && oldAngle > 180f && RawAngle <= 180f))
            {
                Debug.Log("Coil beat");
                if (!BindingStack.Instance.TryPushBinding("(Nothing)"))
                {
                    // ran out of slack, stall the spin
                    Debug.Log("Stalling spin due to lack of slack");
                    CurrentState = RopeDartState.Stalled;
                }
                
                if (_coilTimer == SpiralDuration)
                {
                    // ran out of slack, stall the spin
                    Debug.Log("Stalling spin due to lack of slack");
                    CurrentState = RopeDartState.Stalled;
                }
            }
        }
        else if (CurrentState == RopeDartState.Uncoiling)
        {
            _coilTimer -= Time.deltaTime;
            _coilTimer = Mathf.Max(_coilTimer, 0f);
            float degreesTraversed = 360f * (3f - Mathf.Sqrt(9f - 6f * _coilTimer));
            float directionMultiplier = IsClockwise ? -1f : 1f;
            RawAngle = Mathf.Repeat(_coilStartAngle + directionMultiplier * degreesTraversed, 360f);

            if (_coilNodeType == "Neck" )
            {
                if ((!IsClockwise && oldAngle < 180f && RawAngle >= 180f) || (IsClockwise && oldAngle > 180f && RawAngle <= 180f))
                {
                    Debug.Log("Uncoil beat: top of spin");
                    if (!BindingStack.Instance.RemoveLastBindingWithIdEndingWith("Neck"))
                    {
                        CurrentState = RopeDartState.Spinning;
                        BindingStack.Instance.TryPushBinding("Spin");
                    }

                    if (_coilTimer == 0f)
                    {
                        CurrentState = RopeDartState.Spinning;
                        BindingStack.Instance.TryPushBinding("Spin");
                    }
                }
            }

            // TODO: this detects both the top and bottom of the spin, but we only want to detect the bottom
            if ((IsClockwise && oldAngle < 180f && RawAngle >= 180f) || (!IsClockwise && oldAngle > 180f && RawAngle <= 180f))
            {
                Debug.Log("Uncoil beat: bottom of spin");
                if (!BindingStack.Instance.RemoveLastBindingWithIdEndingWith(_coilNodeType))
                {
                    CurrentState = RopeDartState.Spinning;
                    BindingStack.Instance.TryPushBinding("Spin");
                }

                if (_coilTimer == 0f)
                {
                    BindingStack.Instance.RemoveLastBindingWithIdEndingWith(_coilNodeType);
                    CurrentState = RopeDartState.Spinning;
                    BindingStack.Instance.TryPushBinding("Spin");
                }
            }
        }
        else if (CurrentState == RopeDartState.Stalled)
        {
            // do nothing
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
                OnRetrieveEnd();
            }
        }
    }

    public void StartSpin()
    {
        if (CurrentState == RopeDartState.Idle)
        {
            // default to a down spin
            IsClockwise = IsDownSpin;
        }
        else if (CurrentState == RopeDartState.Stalled)
        {
            CurrentState = RopeDartState.Uncoiling;
            IsClockwise = !IsClockwise;
            _coilStartAngle = RawAngle;
            return;
        }
    
        // IsCoiling = false;
        // _isStalled = false;
        // TODO: not sure if this is a good idea or not
        RawAngle = 180f;

        CurrentState = RopeDartState.Spinning;
    }

    public void Cast()
    {
        Debug.Log("Casting");

        IsLastCastEast = IsClockwise ? RawAngle > 315 || RawAngle > 0f && RawAngle < 135f : RawAngle > 45f && RawAngle < 225f;

        IsLeadSide = IsFrontPlane ? IsLastCastEast : !IsLastCastEast;
        CurrentState = RopeDartState.Casting;
        _debugTimer = 0f;
    }

    public void OnEndCast()
    {
        OnMaxLength();
    }

    public void StartWrap()
    {
        string wrapId = BindingStack.Instance.DetectWrap();
        HandleWrapBuff(wrapId);

        BindingStack.Instance.TryPushBinding("Spin");
    }

    private void HandleWrapBuff(string wrapId)
    {
        switch (wrapId)
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
            case "Necklace":
                Debug.Log("Starting wrap buff for Necklace");
                break;
            case "Belt":
                Debug.Log("Starting wrap buff for Belt");
                break;
            case "Butterfly":
                Debug.Log("Starting wrap buff for Butterfly");
                break;
            default:
                Debug.LogWarning($"No wrap buff defined for binding {wrapId}");
                break;
        }
    }

    public void OnMaxLength()
    {
        CurrentState = RopeDartState.Extended;
    }

    public void Retrieve()
    {
        if (CurrentState != RopeDartState.Extended && CurrentState != RopeDartState.Casting)
            return;

        IsClockwise = IsLastCastEast;

        CurrentState = RopeDartState.Retrieving;
    }

    public void OnRetrieveEnd()
    {
        // Reset();
        // TODO: replace with push binding "(Nothing)"
        BindingStack.Instance.TryPushBinding("Spin");
    }

    public void Reset()
    {
        BindingStack.Instance.ClearBindings();
        BindingStack.Instance.TryPushBinding("Idle");
        RopeDartStatusUI.Instance.UpdateStatusUI();
        CurrentState = RopeDartState.Idle;
        RawAngle = 180f;
    }

    public void FlipLeadAnchor()
    {
        IsLeadSide = !IsLeadSide;
        IsFrontPlane = !IsFrontPlane;

        if (CurrentState == RopeDartState.Coiling)
        {
            CurrentState = RopeDartState.Uncoiling;
        }
    }

    public void FlipSpinDirection()
    {
        // assumes that this is only used for turns
        // a stall would also flip spin direction, but would not flip lead/anchor side
        IsLeadSide = !IsLeadSide;
    }

    public void FlipPlane()
    {
        // TODO: figure out how to track being in "dark plane" beyond the raw value of IsWallPlane
    }

    public void SetCoiling(bool isCoiling)
    {
        if (CurrentState == RopeDartState.Spinning && isCoiling)
        {
            CurrentState = RopeDartState.Coiling;
            RawAngle = 180f;
            _coilStartAngle = RawAngle;

            BindingGraphNode lastBinding = BindingStack.Instance.PeekBinding();
            string nodeId = lastBinding != null ? lastBinding.NodeId : "";
            if (nodeId.EndsWith("Elbow"))
            {
                _coilNodeType = "Elbow";
            }
            else if (nodeId.EndsWith("Neck"))
            {
                _coilNodeType = "Neck";
            }
        }
    }
}

public enum RopeDartState
{
    Idle,
    Spinning,
    Casting,
    Extended,
    Retrieving,
    Stalled,
    Coiling,
    Uncoiling,
    Pendulum,
}
