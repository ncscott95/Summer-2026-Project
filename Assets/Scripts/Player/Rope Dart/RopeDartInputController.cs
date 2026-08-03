using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float DartDirectionBufferDuration = 0.1f;
    private const float CastBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    // private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => RopeDartManager.Instance.ShiftPlane(direction));
    private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, null);
    private readonly InputBuffer _castBuffer = new(CastBufferDuration, () => TryCastBinding());
    private readonly InputBuffer _twineBuffer = new(TwineBufferDuration, () => TryTwineSimple());

    private bool _isDirectionConsumed = false;

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleSpinRetrieveInput()
    {
        if (BindingStack.Instance.TryPushBinding("Spin") != null)
        {
            // successfully started a spin
        }
        else if (BindingStack.Instance.TryPushBinding("Retrieve") != null)
        {
            // successfully started a retrieve
        }
    }

    public void HandleCastInput()
    {
        if (_dartDirectionBuffer.Interrupt())
        {
            HelperCastWithDirection(_dartDirectionBuffer.GetLastBufferedInput());
            _isDirectionConsumed = true;
        }
        else
        {
            _castBuffer.StartBuffer();
        }
    }

    private static void TryCastBinding()
    {
        if (BindingStack.Instance.TryPushBinding("Cast") != null)
        {
            // successfully started a cast
        }
    }

    public void HandleTwineInput()
    {
        if (_dartDirectionBuffer.Interrupt())
        {
            HelperTwineWithDirection(_dartDirectionBuffer.GetLastBufferedInput());
            _isDirectionConsumed = true;
        }
        else
        {
            _twineBuffer.StartBuffer();
        }
    }

    private static void TryTwineSimple()
    {
        string bindingInput = RopeDartManager.Instance.IsLeadSide ? "Bind Lead" : "Bind Anchor";

        if (BindingStack.Instance.TryPushBinding(bindingInput) != null)
        {
            // successfully started a twine
        }
    }

    public void HandleWrapInput()
    {
        if (BindingStack.Instance.TryPushBinding("Wrap") != null)
        {
            // successfully started a wrap
        }
    }

    public void HandleDartDirectionInput(Vector2 input)
    {
        if (input.magnitude < DirectionDeadzone)
        {
            _isDirectionConsumed = false;
            return;
        }

        if (_isDirectionConsumed)
            return;

        if (_castBuffer.Interrupt())
        {
            HelperCastWithDirection(input);
            _isDirectionConsumed = true;
        }
        else if (_twineBuffer.Interrupt())
        {
            HelperTwineWithDirection(input);
            _isDirectionConsumed = true;
        }
        else
        {
            _dartDirectionBuffer.StartBuffer(input);
        }
    }

    private void HelperTwineWithDirection(Vector2 direction)
    {
        string bindingInput = "";

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle <= 22.5f || angle > 337.5f) bindingInput = "Bind Lead";
        else if (angle > 22.5f && angle <= 67.5f) bindingInput = "Bind Lead Up";
        else if (angle > 67.5f && angle <= 112.5f) bindingInput = "Bind Up";
        else if (angle > 112.5f && angle <= 157.5f) bindingInput = "Bind Anchor Up";
        else if (angle > 157.5f && angle <= 202.5f) bindingInput = "Bind Anchor";
        else if (angle > 202.5f && angle <= 247.5f) bindingInput = "Bind Anchor Down";
        else if (angle > 247.5f && angle <= 292.5f) bindingInput = "Bind Down";
        else if (angle > 292.5f && angle <= 337.5f) bindingInput = "Bind Lead Down";

        if (BindingStack.Instance.TryPushBinding(bindingInput) != null)
        {
            // successfully started a twine
        }
    }

    private void HelperCastWithDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle > 247.5f && angle <= 292.5f)
        {
            // down
            // TODO: cast with foot
        }
        else
        {
            // all other inputs
            // TODO: cast with hand
        }
    }

    public void HandleTurnInput()
    {
        if (BindingStack.Instance.TryPushBinding("Turn") != null)
        {
            // successfully started a turn
        }
    }

    public void HandleCrossInput()
    {
        if (BindingStack.Instance.TryPushBinding("Cross") != null)
        {
            // successfully started a cross
        }
    }
}
