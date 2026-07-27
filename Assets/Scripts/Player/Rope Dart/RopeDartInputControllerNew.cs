using UnityEngine;

public class RopeDartInputControllerNew : Singleton<RopeDartInputControllerNew>
{
    private const float DartDirectionBufferDuration = 0.1f;
    private const float CastBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    private readonly InputBuffer<Vector2> dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => RopeDartManagerNew.Instance.ShiftPlane(direction));
    private readonly InputBuffer castBuffer = new(CastBufferDuration, () => RopeDartManagerNew.Instance.Cast());
    private readonly InputBuffer twineBuffer = new(TwineBufferDuration, () => RopeDartManagerNew.Instance.TwineSimple());

    private bool isDirectionConsumed = false;

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleSpinRetrieveInput()
    {
        RopeDartManagerNew.Instance.ToggleTryingToSpin(true);

        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Idle)
        {
            RopeDartManagerNew.Instance.StartSpin();
        }
        else if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Extended || RopeDartManagerNew.Instance.CurrentState == RopeDartState.Casting)
        {
            RopeDartManagerNew.Instance.Retrieve();
        }
    }

    public void HandleCastInput()
    {
        if (dartDirectionBuffer.Interrupt())
        {
            HelperCastWithDirection(dartDirectionBuffer.GetLastBufferedInput());
            isDirectionConsumed = true;
        }
        else
        {
            castBuffer.StartBuffer();
        }
    }

    public void HandleTwineInput()
    {
        if (dartDirectionBuffer.Interrupt())
        {
            HelperTwineWithDirection(dartDirectionBuffer.GetLastBufferedInput());
            isDirectionConsumed = true;
        }
        else
        {
            twineBuffer.StartBuffer();
        }
    }

    public void HandleWrapInput()
    {
        RopeDartManagerNew.Instance.TryStartWrap();
    }

    public void HandleWrapInputEnd()
    {
        RopeDartManagerNew.Instance.EndWrap();
    }

    public void HandleDartDirectionInput(Vector2 input)
    {
        if (input.magnitude < DirectionDeadzone)
        {
            isDirectionConsumed = false;
            return;
        }

        if (isDirectionConsumed)
            return;

        if (castBuffer.Interrupt())
        {
            HelperCastWithDirection(input);
            isDirectionConsumed = true;
        }
        else if (twineBuffer.Interrupt())
        {
            HelperTwineWithDirection(input);
            isDirectionConsumed = true;
        }
        else
        {
            dartDirectionBuffer.StartBuffer(input);
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

        RopeDartManagerNew.Instance.Twine(bindingInput);
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
}
