using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
// [RequireComponent(typeof(SkillSequencer))]
// public abstract class PlayerControllerBase : Singleton<PlayerControllerBase>, IDamageable
public abstract class PlayerControllerBase : Singleton<PlayerControllerBase>
{
    protected const float WALKING_THRESHOLD = 1f;
    protected const float MAX_WALK_MAGNITUDE = 4f;
    public Transform Camera { get; protected set; }

    public InputSystem_Actions Actions { get; private set; }
    protected Rigidbody2D _rb;
    protected bool _canAct = true;

    [Header("Ground Check")]
    [SerializeField] protected LayerMask _groundMask;
    protected bool _isGrounded;

    // [Header("Attributes")]
    // [SerializeField] private Attribute _healthAttribute;
    // [SerializeField] private AttributeUI _healthUI;
    // public Attribute HealthAttribute { get; private set; }
    // [SerializeField] private Attribute _staminaAttribute;
    // [SerializeField] private AttributeUI _staminaUI;
    // public Attribute StaminaAttribute { get; private set; }
    // protected bool _isInvincible = false;
    // protected bool _isDead = false;

    [Header("Movement")]
    [SerializeField] protected float _maxSpeed;
    [SerializeField] protected float _acceleration;
    [SerializeField] protected float _groundDrag;
    protected List<float> _speedModifiers = new();
    public float Speed { get { return _maxSpeed * _speedModifiers.Aggregate(1f, (acc, val) => acc * val); } }
    protected Vector2 _moveInput;
    protected bool _canMove = true;

    // [Header("Combat")]
    // public SkillSequencer SkillSequencer { get; private set; }
    // public AbilityGridUIManager AbilityGridUIManager;

    // [Header("Interacting")]
    // [SerializeField] private InteractHitbox _interactHitbox;

    public override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        // SkillSequencer = GetComponent<SkillSequencer>();
        Instance.Camera = UnityEngine.Camera.main.transform;
        Actions = new InputSystem_Actions();

        // HealthAttribute = Instantiate(_healthAttribute);
        // StaminaAttribute = Instantiate(_staminaAttribute);

        // if (_healthUI != null) _healthUI.SetAttribute(HealthAttribute);
        // if (_staminaUI != null) _staminaUI.SetAttribute(StaminaAttribute);
    }

    void Start()
    {
        // HealthAttribute.Initialize();
        // StaminaAttribute.Initialize();
    }

    public virtual void OnEnable()
    {
        Actions.Player.Enable();

        Actions.Player.Move.performed += OnMove;
        Actions.Player.Move.canceled += OnMove;
        // Actions.Player.ShowGrid.performed += ctx => ToggleGridUI();
    }

    public virtual void OnDisable()
    {
        Actions.Player.Disable();

        Actions.Player.Move.performed -= OnMove;
        Actions.Player.Move.canceled -= OnMove;
        // Actions.Player.ShowGrid.performed -= ctx => ToggleGridUI();
    }

    public virtual void Update()
    {
        // Ground check
        _isGrounded = Physics2D.Raycast(transform.position + Vector3.up * 0.01f, Vector3.down, 0.3f, _groundMask);
    }

    private void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public virtual void FixedUpdate() { }
    public abstract void Dodge();
    public virtual void SetCameraControlActive(bool active) { }
    public virtual void ToggleMovement(bool canMove) { _canMove = canMove; Debug.Log($"Can move: {_canMove}"); }
    public void AddSpeedModifier(float modifier) { _speedModifiers.Add(modifier); }
    public void RemoveSpeedModifier(float modifier) { _speedModifiers.Remove(modifier); }

    // public virtual void Interact()
    // {
    //     if (GameManager.Instance.IsInState(GameManager.GameState.Dialogue))
    //     {
    //         DialogueManager.Instance.TryAdvanceDialogue();
    //     }
    //     else if (GameManager.Instance.IsInState(GameManager.GameState.Walking))
    //     {
    //         _interactHitbox.TryInteract();
    //     }
    //     else if (GameManager.Instance.IsInState(GameManager.GameState.Combat))
    //     {
    //         _interactHitbox.TryInteract();
    //     }
    // }

    // public virtual void ToggleGridUI()
    // {
    //     AbilityGridUIManager.gameObject.SetActive(!AbilityGridUIManager.gameObject.activeSelf);
    // }

    // public virtual void TakeDamage(int damage)
    // {
    //     if (_isInvincible) return;

    //     HealthAttribute.ModifyCurrentValue(-damage, false);
    //     if (HealthAttribute.CurrentValue <= 0)
    //     {
    //         _isDead = true;
    //         StopAllCoroutines();
    //         SetCameraControlActive(false);
    //         // GameManager.Instance.OnDeath();
    //     }
    // }

    // public virtual void ConsumeStamina(float amount)
    // {
    //     StaminaAttribute.ModifyCurrentValue(-amount, true);
    // }

    // public virtual bool CanUseStamina(float amount)
    // {
    //     return StaminaAttribute.CurrentValue >= amount;
    // }

    public virtual void SpawnPlayer(Transform point)
    {
        transform.SetPositionAndRotation(point.position, point.rotation);
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        // _isDead = false;

        // HealthAttribute.Initialize();
        // StaminaAttribute.Initialize();
    }
}
