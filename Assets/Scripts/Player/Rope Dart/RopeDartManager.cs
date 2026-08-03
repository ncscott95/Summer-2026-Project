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
    
    private bool _isSpiraling = false;
    private float _spiralTimer = 0f;
    private float _spiralStartAngle = 0f;
    private bool _isStalled = false;

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

            // TODO: need to push "(Nothing)" bindings if current binding does decay and the rope has reached an angle specific to that binding

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
    
        _isSpiraling = false;
        _isStalled = false;
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
