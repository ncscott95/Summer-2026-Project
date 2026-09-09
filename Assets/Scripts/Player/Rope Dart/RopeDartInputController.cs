using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float DartDirectionBufferDuration = 0.2f;
    private const float TwineBufferDuration = 0.2f;
    private const float DirectionDeadzone = 0.5f;

    // Animation timing values
    private const float AnimationFrameDuration = 0.05f; // 20 frames per second
    private const int CastAnimationFrameLength = 7;
    private const int ElbowAnimationFrameLength = 16;
    private const int ExtendedAnimationFrameLength = 2;
    private const int RetrieveAnimationFrameLength = 7;
    private const int DragonAnimationFrameLength = 16;
    private const int NeckAnimationFrameLength = 13;
    private const int NeckOppAnimationFrameLength = 37;

    // Combined input timing buffers
    // private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => TryTurn(direction));
    private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, null);
    private readonly InputBuffer _twineBuffer = new(TwineBufferDuration, () => TryTwineSimple());

    // Skill frame timing buffers
    private readonly InputBuffer _castEndBuffer = new((CastAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnCastEnd());
    private readonly InputBuffer _elbowEndBuffer = new((ElbowAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnCastEnd());
    private readonly InputBuffer _retrieveEndBuffer = new((RetrieveAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnRetrieveEnd());
    private readonly InputBuffer _dragonEndBuffer = new((DragonAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnDragonEnd());
    private readonly InputBuffer _neckEndBuffer = new((NeckAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnNeckEnd());
    private readonly InputBuffer _neckOppEndBuffer = new((NeckOppAnimationFrameLength + ExtendedAnimationFrameLength) * AnimationFrameDuration, () => RopeDartManager.Instance.OnNeckEnd());

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    // public void HandleSpinInput()
    // {
    //     BindingStack.Instance.TryPushBinding("Spin");
    // }

    public void HandleCastInput()
    {
        BindingStack.Instance.TryPushBinding("Cast");
    }

    public void HandleTwineInput()
    {
        if (_dartDirectionBuffer.Interrupt())
        {
            HelperTwineWithDirection(_dartDirectionBuffer.GetLastBufferedInput());
        }
        else
        {
            _twineBuffer.StartBuffer();
        }
    }

    private static void TryTwineSimple()
    {
        BindingStack.Instance.TryPushBinding("Twine Back");
    }

    public void HandleDartDirectionInput(Vector2 input)
    {
        if (input.magnitude < DirectionDeadzone)
        {
            return;
        }
        if (_twineBuffer.Interrupt())
        {
            HelperTwineWithDirection(input);
        }
        else
        {
            _dartDirectionBuffer.StartBuffer(input);
        }
    }

    private void HelperTwineWithDirection(Vector2 direction)
    {
        string bindingInput = "";

        // 0 = right, 90 = up, 180 = left, 270 = down
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // perfect 45 degree bindings default to east and west
        if (angle <= 45f || angle >= 315f) bindingInput = "Twine" + (RopeDartManager.Instance.IsLeadSide ? " Face" : " Back");
        else if (angle > 45f && angle < 135f) bindingInput = "Twine Up";
        else if (angle >= 135f && angle <= 225f) bindingInput = "Twine " + (RopeDartManager.Instance.IsLeadSide ? " Back" : " Face");
        else if (angle > 225f && angle < 315f) bindingInput = "Twine Down";

        BindingStack.Instance.TryPushBinding(bindingInput);
    }

    public void StartCastEndBuffer()
    {
        _castEndBuffer.StartBuffer();
    }

    public void StartElbowEndBuffer()
    {
        _elbowEndBuffer.StartBuffer();
    }

    public void StartRetrieveEndBuffer()
    {
        _retrieveEndBuffer.StartBuffer();
    }

    public void StartDragonEndBuffer()
    {
        _dragonEndBuffer.StartBuffer();
    }

    public void StartNeckEndBuffer()
    {
        _neckEndBuffer.StartBuffer();
    }

    public void StartNeckOppEndBuffer()
    {
        _neckOppEndBuffer.StartBuffer();
    }
}
