using UnityEngine;

public class RopeDartInputController : Singleton<RopeDartInputController>
{
    private const float DartDirectionBufferDuration = 0.2f;
    private const float TwineBufferDuration = 0.2f;
    private const float DirectionDeadzone = 0.5f;

    private readonly InputBuffer<Vector2> _dartDirectionBuffer = new(DartDirectionBufferDuration, (direction) => TryTurn(direction));
    private readonly InputBuffer _twineBuffer = new(TwineBufferDuration, () => TryTwineSimple());

    private bool _isDirectionConsumed = false;

    void Update()
    {
        InputBufferList.TickAll(Time.deltaTime);
    }

    public void HandleSpinRetrieveInput()
    {
        if (BindingStack.Instance.TryPushBinding("Spin") != null) return;

        BindingStack.Instance.TryPushBinding("Retrieve");
    }

    public void HandleCastInput()
    {
        BindingStack.Instance.TryPushBinding("Cast");
    }

    public void HandleTwineInput()
    {
        Debug.Log("Twine input received");
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
        BindingStack.Instance.TryPushBinding("Twine");
    }

    public void HandleWrapInput()
    {
        BindingStack.Instance.TryPushBinding("Wrap");
    }

    public void HandleDartDirectionInput(Vector2 input)
    {
        if (input.magnitude < DirectionDeadzone)
        {
            // TODO: this could be problematic
            _isDirectionConsumed = false;
            return;
        }

        if (_isDirectionConsumed)
            return;

        if (_twineBuffer.Interrupt())
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

        // 0 = right, 90 = up, 180 = left, 270 = down
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // perfect 45 degree bindings default to lead and anchor
        if (angle <= 45f || angle >= 315f) bindingInput = "Twine Lead";
        else if (angle > 45f && angle < 135f) bindingInput = "Twine Up";
        else if (angle >= 135f && angle <= 225f) bindingInput = "Twine Anchor";
        else if (angle > 225f && angle < 315f) bindingInput = "Twine Down";

        BindingStack.Instance.TryPushBinding(bindingInput);
    }

    private static void TryTurn(Vector2 direction)
    {
        // 0 = right, 90 = up, 180 = left, 270 = down
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if ((angle > 45f && angle < 135f) || (angle > 225f && angle < 315f))
        {
            // input is not east or west, do nothing
            return;
        }

        // after above check, input must be either east or west
        bool tryTurnEast = angle <= 45f || angle >= 315f;

        if (RopeDartManager.Instance.IsFacingEast == tryTurnEast)
        {
            BindingStack.Instance.TryPushBinding("Cross");
        }
        else
        {
            BindingStack.Instance.TryPushBinding("Turn");
        }
    }
}
