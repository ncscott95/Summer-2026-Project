using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float DartDirectionBufferDuration = 0.1f;
    private const float TwineBufferDuration = 0.1f;
    private const float DirectionDeadzone = 0.5f;

    // Combined input timing buffers
    // private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => TryTurn(direction));
    private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, null);
    private readonly InputBuffer _twineBuffer = new(TwineBufferDuration, () => TryTwineSimple());

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleCastInput()
    {
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

        RopeDartVisualManager.Instance.SetBufferedBinding(bindingInput);
    }
}
