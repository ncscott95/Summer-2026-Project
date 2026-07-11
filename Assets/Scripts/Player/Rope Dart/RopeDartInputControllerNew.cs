using UnityEngine;

public class RopeDartInputControllerNew : Singleton<RopeDartInputControllerNew>
{
    private const float ReleaseSpinBufferDuration = 0.1f;
    private const float DartDirectionBufferDuration = 0.1f;
    private const float CastBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    private readonly InputBuffer releaseSpinBuffer = new(ReleaseSpinBufferDuration, () => RopeDartManagerNew.Instance.StopSpin());
    private readonly InputBuffer<Vector2> dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => RopeDartManagerNew.Instance.ShiftPlane(direction));
    private readonly InputBuffer castBuffer = new(CastBufferDuration, () => RopeDartManagerNew.Instance.Cast());
    private readonly InputBuffer twineBuffer = new(TwineBufferDuration, () => RopeDartManagerNew.Instance.TwineSimple());

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleSpinRetrieveInput()
    {
        RopeDartManagerNew.Instance.ToggleTryingToSpin(true);

        releaseSpinBuffer.TryForceEnd();

        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Idle || RopeDartManagerNew.Instance.CurrentState == RopeDartState.Stalling) 
        {
            RopeDartManagerNew.Instance.StartSpin();
        }
        else if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Extended || RopeDartManagerNew.Instance.CurrentState == RopeDartState.Casting) 
        {
            RopeDartManagerNew.Instance.Retrieve();
        }
    }

    public void HandleSpinInputEnd()
    {
        RopeDartManagerNew.Instance.ToggleTryingToSpin(false);
        
        if (RopeDartManagerNew.Instance.CurrentState == RopeDartState.Spinning)
        {
            releaseSpinBuffer.StartBuffer();
        }
    }

    public void HandleCastInput()
    {
        releaseSpinBuffer.Interrupt();

        if (dartDirectionBuffer.Interrupt())
        {
            // TODO: cast with modifier based on dartDirectionBuffer.GetLastBufferedInput()
            HelperCastWithDirection(dartDirectionBuffer.GetLastBufferedInput());
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
            return;

        if (castBuffer.Interrupt())
        {
            // TODO: modify cast based on input
            HelperCastWithDirection(input);
        }
        else if (twineBuffer.Interrupt())
        {
            // TODO: modify twine based on input
            HelperTwineWithDirection(input);
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

        if (angle <= 22.5f || angle > 337.5f)
        {
            // right
            bindingInput = "Bind Lead";
        }
        else if (angle > 22.5f && angle <= 67.5f)
        {
            // up-right
            bindingInput = "Bind Lead Up";
        }
        else if (angle > 67.5f && angle <= 112.5f)
        {
            // up
            bindingInput = "Bind Up";
        }
        else if (angle > 112.5f && angle <= 157.5f)
        {
            // up-left
            bindingInput = "Bind Anchor Up";
        }
        else if (angle > 157.5f && angle <= 202.5f)
        {
            // left
            bindingInput = "Bind Anchor";
        }
        else if (angle > 202.5f && angle <= 247.5f)
        {
            // down-left
            bindingInput = "Bind Anchor Down";
        }
        else if (angle > 247.5f && angle <= 292.5f)
        {
            // down
            bindingInput = "Bind Down";
        }
        else if (angle > 292.5f && angle <= 337.5f)
        {
            // down-right
            bindingInput = "Bind Lead Down";
        }

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
