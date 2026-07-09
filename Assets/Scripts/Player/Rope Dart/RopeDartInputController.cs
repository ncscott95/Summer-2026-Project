using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float ReleaseSpinBufferDuration = 0.1f;
    private const float DartDirectionBufferDuration = 0.1f;
    private const float CastBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    private readonly InputBuffer releaseSpinBuffer = new(ReleaseSpinBufferDuration, () => RopeDartManager.Instance.StopSpin());
    private readonly InputBuffer<Vector2> dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => RopeDartManager.Instance.ShiftPlane(direction));
    private readonly InputBuffer castBuffer = new(CastBufferDuration, () => RopeDartManager.Instance.Cast());
    private readonly InputBuffer twineBuffer = new(TwineBufferDuration, () => RopeDartManager.Instance.TwineSimple());

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleSpinRetrieveInput()
    {
        RopeDartManager.Instance.ToggleTryingToSpin(true);

        releaseSpinBuffer.TryForceEnd();

        if (RopeDartManager.Instance.CurrentState == RopeDartState.Idle || RopeDartManager.Instance.CurrentState == RopeDartState.Stalling) 
        {
            RopeDartManager.Instance.StartSpin();
        }
        else if (RopeDartManager.Instance.CurrentState == RopeDartState.Extended || RopeDartManager.Instance.CurrentState == RopeDartState.Casting) 
        {
            RopeDartManager.Instance.Retrieve();
        }
    }

    public void HandleSpinInputEnd()
    {
        RopeDartManager.Instance.ToggleTryingToSpin(false);
        
        if (RopeDartManager.Instance.CurrentState == RopeDartState.Spinning)
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
        
    }

    public void HandleWrapInputEnd()
    {
        
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

        RopeDartManager.Instance.Twine(bindingInput);
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
