using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public InputSystem_Actions Actions { get; private set; }

    void Awake()
    {
        Actions = new InputSystem_Actions();
    }

    public void OnEnable()
    {
        Actions.Player.Enable();

        // Actions.Player.Spin.performed += ctx => RopeDartInputController.Instance.HandleSpinInput();
        Actions.Player.Cast.performed += ctx => RopeDartInputController.Instance.HandleCastInput();
        Actions.Player.Twine.performed += ctx => RopeDartInputController.Instance.HandleTwineInput();
        Actions.Player.DartDirection.performed += ctx => RopeDartInputController.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
    }

    public void OnDisable()
    {
        Actions.Player.Disable();

        // Actions.Player.Spin.performed -= ctx => RopeDartInputController.Instance.HandleSpinInput();
        Actions.Player.Cast.performed -= ctx => RopeDartInputController.Instance.HandleCastInput();
        Actions.Player.Twine.performed -= ctx => RopeDartInputController.Instance.HandleTwineInput();
        Actions.Player.DartDirection.performed -= ctx => RopeDartInputController.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
    }
}
