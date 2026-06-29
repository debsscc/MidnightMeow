///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Manipula o input do jogador e dispara eventos que outros componentes podem assinar.
// ---------------------------------------------------------------- */
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    // Eventos que os outros scripts do Player ir�o assinar
    public event Action<Vector2> OnMoveInput;
    // Now reports whether the fire button is pressed (true) or released (false)
    public event Action<bool> OnFireInput;
    public event Action OnDashInput;
    public event Action OnAbility1Input;
    public event Action OnAbility2Input;
    public event Action OnFrenzyInput;
    public event Action<bool> OnInteractHoldChanged;

    /// <summary>Direção bruta da mira por analógico direito (zero quando solto/neutro).</summary>
    public Vector2 AimInput { get; private set; }

    // Methods called by PlayerInput are not relied on for fire state anymore.
    // We subscribe directly to the underlying InputAction to reliably detect started/canceled.
    private PlayerInput _playerInput;
    private InputAction _fireAction;
    private InputAction _interactAction;
    private bool _isPaused = false;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        ResolveInputActions();
        SubscribeInputActions();
        GameEvents.OnPauseChanged += HandlePauseChanged;
    }

    private void OnDisable()
    {
        UnsubscribeInputActions();
        GameEvents.OnPauseChanged -= HandlePauseChanged;
    }

    private void ResolveInputActions()
    {
        if (_playerInput == null)
            _playerInput = GetComponent<PlayerInput>();

        if (_playerInput?.actions == null)
            return;

        if (!_playerInput.actions.enabled)
            _playerInput.actions.Enable();

        _fireAction = _playerInput.actions.FindAction("Fire", throwIfNotFound: false);
        _interactAction = _playerInput.actions.FindAction("Interact", throwIfNotFound: false);
    }

    private void SubscribeInputActions()
    {
        if (_fireAction != null)
        {
            _fireAction.started += OnFireStarted;
            _fireAction.canceled += OnFireCanceled;
        }

        if (_interactAction != null)
        {
            _interactAction.started += OnInteractStarted;
            _interactAction.performed += OnInteractPerformed;
            _interactAction.canceled += OnInteractCanceled;
        }
    }

    private void UnsubscribeInputActions()
    {
        if (_fireAction != null)
        {
            _fireAction.started -= OnFireStarted;
            _fireAction.canceled -= OnFireCanceled;
        }

        if (_interactAction != null)
        {
            _interactAction.started -= OnInteractStarted;
            _interactAction.performed -= OnInteractPerformed;
            _interactAction.canceled -= OnInteractCanceled;
        }
    }

    public void OnMove(InputValue value)
    {
        if (_isPaused) return;
        OnMoveInput?.Invoke(value.Get<Vector2>());
    }

    public void OnAim(InputValue value)
    {
        // Sempre atualiza (inclusive quando volta a zero) para o PlayerAim decidir entre stick e mouse.
        AimInput = value.Get<Vector2>();
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (_isPaused) return;

        OnFireInput?.Invoke(true);
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (_isPaused) return;

        OnFireInput?.Invoke(false);
    }

    public void OnAbility1(InputValue value)
    {
        if (_isPaused) return;
        if (value.isPressed)
            OnAbility1Input?.Invoke();
    }

    public void OnAbility2(InputValue value)
    {
        if (_isPaused) return;
        if (value.isPressed)
            OnAbility2Input?.Invoke();
    }

    [Obsolete("Use OnAbility1.")]
    public void OnAbility(InputValue value) => OnAbility1(value);

    public void OnFrenzy(InputValue value)
    {
        if (_isPaused) return;

        if (value.isPressed)
        {
            OnFrenzyInput?.Invoke();
        }
    }

    public void OnDash(InputValue value)
    {
        if (_isPaused) return;

        if (value.isPressed)
        {
            OnDashInput?.Invoke();
        }
    }

    public void OnInteract(InputValue value)
    {
        // Evita evento duplicado: Interact usa apenas InputAction started/canceled abaixo.
    }

    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        if (_isPaused) return;
        OnInteractHoldChanged?.Invoke(true);
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (_isPaused) return;
        OnInteractHoldChanged?.Invoke(true);
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        if (_isPaused) return;
        OnInteractHoldChanged?.Invoke(false);
    }

    private void HandlePauseChanged(bool paused)
    {
        _isPaused = paused;
        if (paused)
        {
            // Force a release signal to stop continuous actions like firing
            OnFireInput?.Invoke(false);
        }
    }
}