using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float DartDirectionBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    // Animation timing values
    // private const float AnimationFrameDuration = 0.05f; // 20 frames per second
    // private const int SpinAnimationFrameLength = 16 - 1;
    // private const int CastAnimationFrameLength = 6 + ExtendedAnimationFrameLength - 1;
    // private const int ElbowAnimationFrameLength = 15 + ExtendedAnimationFrameLength - 1;
    // private const int ExtendedAnimationFrameLength = 1;
    // private const int RetrieveAnimationFrameLength = 5 - 1;
    // private const int DragonAnimationFrameLength = 16 - 1;
    // private const int NeckAnimationFrameLength = 13 - 1;
    // private const int NeckOppAnimationFrameLength = 37 - 1;

    // Combined input timing buffers
    // private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => TryTurn(direction));
    private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, null);
    private readonly InputBuffer _twineBuffer = new(TwineBufferDuration, () => TryTwineSimple());

    // Skill frame timing buffers
    // private readonly InputBuffer _spinEndBuffer = new(SpinAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnSpinEnd());
    // private readonly InputBuffer _castEndBuffer = new(CastAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnCastEnd());
    // private readonly InputBuffer _elbowEndBuffer = new(ElbowAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnCastEnd());
    // private readonly InputBuffer _retrieveEndBuffer = new(RetrieveAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnRetrieveEnd());
    // private readonly InputBuffer _dragonEndBuffer = new(DragonAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnDragonEnd());
    // private readonly InputBuffer _neckEndBuffer = new(NeckAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnNeckEnd());
    // private readonly InputBuffer _neckOppEndBuffer = new(NeckOppAnimationFrameLength * AnimationFrameDuration, () => RopeDartManager.Instance.OnNeckEnd());

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
        // BindingStack.Instance.TryPushBinding("Cast");
        RopeDartVisualManager.Instance.SetBufferedBinding("Cast");
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
        // BindingStack.Instance.TryPushBinding("Twine Back");
        RopeDartVisualManager.Instance.SetBufferedBinding("Twine Back");
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

        // BindingStack.Instance.TryPushBinding(bindingInput);
        RopeDartVisualManager.Instance.SetBufferedBinding(bindingInput);
    }

    // public void StartSpinEndBuffer()
    // {
    //     _spinEndBuffer.StartBuffer();
    // }

    // public void StartCastEndBuffer()
    // {
    //     _castEndBuffer.StartBuffer();
    // }

    // public void StartElbowEndBuffer()
    // {
    //     _elbowEndBuffer.StartBuffer();
    // }

    // public void StartRetrieveEndBuffer()
    // {
    //     _retrieveEndBuffer.StartBuffer();
    // }

    // public void StartDragonEndBuffer()
    // {
    //     _dragonEndBuffer.StartBuffer();
    // }

    // public void StartNeckEndBuffer()
    // {
    //     _neckEndBuffer.StartBuffer();
    // }

    // public void StartNeckOppEndBuffer()
    // {
    //     _neckOppEndBuffer.StartBuffer();
    // }
}
