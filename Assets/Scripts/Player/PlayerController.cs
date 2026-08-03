using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public Transform Camera { get; protected set; }
    public InputSystem_Actions Actions { get; private set; }

    public override void Awake()
    {
        base.Awake();

        Instance.Camera = UnityEngine.Camera.main.transform;
        Actions = new InputSystem_Actions();
    }

    public void OnEnable()
    {
        Actions.Player.Enable();

        Actions.Player.Spin.performed += ctx => RopeDartInputController.Instance.HandleSpinRetrieveInput();
        Actions.Player.Cast.performed += ctx => RopeDartInputController.Instance.HandleCastInput();
        Actions.Player.Twine.performed += ctx => RopeDartInputController.Instance.HandleTwineInput();
        Actions.Player.Wrap.performed += ctx => RopeDartInputController.Instance.HandleWrapInput();
        Actions.Player.DartDirection.performed += ctx => RopeDartInputController.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
        // Actions.Player.Turn.performed += ctx => RopeDartInputController.Instance.HandleTurnInput();
        // Actions.Player.Cross.performed += ctx => RopeDartInputController.Instance.HandleCrossInput();
    }

    public void OnDisable()
    {
        Actions.Player.Disable();

        Actions.Player.Spin.performed -= ctx => RopeDartInputController.Instance.HandleSpinRetrieveInput();
        Actions.Player.Cast.performed -= ctx => RopeDartInputController.Instance.HandleCastInput();
        Actions.Player.Twine.performed -= ctx => RopeDartInputController.Instance.HandleTwineInput();
        Actions.Player.Wrap.performed -= ctx => RopeDartInputController.Instance.HandleWrapInput();
        Actions.Player.DartDirection.performed -= ctx => RopeDartInputController.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
        // Actions.Player.Turn.performed -= ctx => RopeDartInputController.Instance.HandleTurnInput();
        // Actions.Player.Cross.performed -= ctx => RopeDartInputController.Instance.HandleCrossInput();
    }
}
