using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

        Actions.Player.Spin.performed += ctx => RopeDartInputControllerNew.Instance.HandleSpinRetrieveInput();
        Actions.Player.Cast.performed += ctx => RopeDartInputControllerNew.Instance.HandleCastInput();
        Actions.Player.Twine.performed += ctx => RopeDartInputControllerNew.Instance.HandleTwineInput();
        Actions.Player.Wrap.performed += ctx => RopeDartInputControllerNew.Instance.HandleWrapInput();
        Actions.Player.DartDirection.performed += ctx => RopeDartInputControllerNew.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
    }

    public void OnDisable()
    {
        Actions.Player.Disable();

        Actions.Player.Spin.performed -= ctx => RopeDartInputControllerNew.Instance.HandleSpinRetrieveInput();
        Actions.Player.Cast.performed -= ctx => RopeDartInputControllerNew.Instance.HandleCastInput();
        Actions.Player.Twine.performed -= ctx => RopeDartInputControllerNew.Instance.HandleTwineInput();
        Actions.Player.Wrap.performed -= ctx => RopeDartInputControllerNew.Instance.HandleWrapInput();
        Actions.Player.DartDirection.performed -= ctx => RopeDartInputControllerNew.Instance.HandleDartDirectionInput(ctx.ReadValue<Vector2>());
    }
}
