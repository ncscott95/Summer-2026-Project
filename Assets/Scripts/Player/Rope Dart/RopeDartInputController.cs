using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float ReleaseSpinBufferDuration = 0.1f;
    private const float DartDirectionBufferDuration = 0.1f;
    private const float CastBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;

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
            // TODO: twine with modifier based on dartDirectionBuffer.GetLastBufferedInput()
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
        if (input.magnitude < 0.5f)
            return;

        if (castBuffer.Interrupt())
        {
            // TODO: modify cast based on input
        }
        else if (twineBuffer.Interrupt())
        {
            // TODO: modify twine based on input
        }
        else
        {
            dartDirectionBuffer.StartBuffer(input);
        }
    }
}
