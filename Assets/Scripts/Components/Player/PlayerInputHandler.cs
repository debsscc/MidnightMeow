///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Manipula o input do jogador e dispara eventos que outros componentes podem assinar.
// ---------------------------------------------------------------- */
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

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
    private bool _mouseFireHeld;
    private bool _firePublishedHeld;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        ResolveInputActions();
        ActivateGameplayInput();
        SubscribeInputActions();
        GameEvents.OnPauseChanged += HandlePauseChanged;
        InputSystem.onDeviceChange += HandleDeviceChange;
        PairExistingControllers();
        EnsureKeyboardMousePaired();
    }

    private void Start()
    {
        EnsureKeyboardMousePaired();
        ResolveInputActions();
        SubscribeInputActions();
    }

    private void PairExistingControllers()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is Gamepad || device is Joystick)
                TryPairController(device);
        }
    }

    private void EnsureKeyboardMousePaired()
    {
        if (_playerInput == null || !_playerInput.user.valid)
            return;

        InputUser user = _playerInput.user;
        if (Keyboard.current != null)
            TryPairDevice(Keyboard.current, user);
        if (Mouse.current != null)
            TryPairDevice(Mouse.current, user);
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= HandleDeviceChange;
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

    private void ActivateGameplayInput()
    {
        if (_playerInput == null || !_playerInput.enabled)
            return;

        if (!_playerInput.inputIsActive)
            _playerInput.ActivateInput();

        if (_playerInput.currentActionMap == null || _playerInput.currentActionMap.name != "Gameplay")
            _playerInput.SwitchCurrentActionMap("Gameplay");
    }

    private void TryPairController(InputDevice device)
    {
        if (_playerInput == null || device == null || !_playerInput.user.valid)
            return;

        TryPairDevice(device, _playerInput.user);
    }

    private static void TryPairDevice(InputDevice device, InputUser user)
    {
        if (device == null || !user.valid)
            return;

        foreach (InputDevice paired in user.pairedDevices)
        {
            if (paired == device)
                return;
        }

        InputUser.PerformPairingWithDevice(device, user);
    }

    private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!isActiveAndEnabled || _playerInput == null)
            return;

        if (change is not (InputDeviceChange.Added or InputDeviceChange.Reconnected
            or InputDeviceChange.ConfigurationChanged))
            return;

        if (device is Gamepad || device is Joystick)
        {
            ActivateGameplayInput();
            TryPairController(device);
            return;
        }

        if (device is Keyboard or Mouse)
            EnsureKeyboardMousePaired();
    }

    private void Update()
    {
        if (_isPaused)
            return;

        EnsureKeyboardMousePaired();

        if (ShouldUseGenericPoll())
        {
            PollGenericController();
            return;
        }

        PollKeyboardMouseFire();
    }

    private bool ShouldUseGenericPoll()
    {
        if (IsKeyboardMouseSchemeActive())
            return false;

        if (!GenericControllerInput.HasHidFallbackDevice)
        {
            InputDevice active = GenericControllerInput.ResolveActiveDevice();
            if (active == null)
                return false;

            return active is not Gamepad
                && active is not Joystick
                && !IsDevicePairedWithPlayer(active);
        }

        InputDevice hidDevice = GenericControllerInput.ResolveActiveDevice();
        if (hidDevice == null || hidDevice is Gamepad || hidDevice is Joystick)
            return false;

        return !IsDevicePairedWithPlayer(hidDevice);
    }

    private bool IsKeyboardMouseSchemeActive()
    {
        if (Mouse.current == null)
            return false;

        if (IsDevicePairedWithPlayer(Mouse.current))
            return true;

        if (Keyboard.current != null && IsDevicePairedWithPlayer(Keyboard.current))
            return true;

        if (_playerInput != null && !string.IsNullOrEmpty(_playerInput.currentControlScheme))
        {
            string scheme = _playerInput.currentControlScheme;
            if (scheme.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0
                || scheme.IndexOf("Mouse", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        if (_playerInput == null || !_playerInput.user.valid)
            return true;

        bool hasPairedGamepad = false;
        foreach (InputDevice paired in _playerInput.user.pairedDevices)
        {
            if (paired is Gamepad or Joystick)
            {
                hasPairedGamepad = true;
                break;
            }
        }

        if (!hasPairedGamepad)
            return true;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        return mouseDelta.sqrMagnitude > 0.01f
            || Mouse.current.leftButton.isPressed
            || Mouse.current.rightButton.isPressed;
    }

    private bool IsDevicePairedWithPlayer(InputDevice device)
    {
        if (_playerInput == null || !_playerInput.user.valid || device == null)
            return false;

        foreach (InputDevice paired in _playerInput.user.pairedDevices)
        {
            if (paired == device)
                return true;
        }

        return false;
    }

    private void PollKeyboardMouseFire()
    {
        if (Mouse.current == null)
            return;

        bool held = Mouse.current.rightButton.isPressed || Mouse.current.leftButton.isPressed;
        if (held == _mouseFireHeld)
            return;

        _mouseFireHeld = held;
        PublishFireInput(held);
    }

    private void PublishFireInput(bool pressed)
    {
        if (_isPaused)
            return;

        if (_firePublishedHeld == pressed)
            return;

        _firePublishedHeld = pressed;
        OnFireInput?.Invoke(pressed);
    }

    private void PollGenericController()
    {
        if (GenericControllerInput.TryReadMove(out Vector2 move))
            OnMoveInput?.Invoke(move);

        if (GenericControllerInput.TryReadAim(out Vector2 aim))
            AimInput = aim;

        if (GenericControllerInput.WasFirePressedThisFrame())
            OnFireInput?.Invoke(true);
        else if (GenericControllerInput.WasFireReleasedThisFrame())
            OnFireInput?.Invoke(false);

        if (GenericControllerInput.WasDashPressedThisFrame())
            OnDashInput?.Invoke();

        if (GenericControllerInput.WasAbility1PressedThisFrame())
            OnAbility1Input?.Invoke();

        if (GenericControllerInput.WasAbility2PressedThisFrame())
            OnAbility2Input?.Invoke();

        if (GenericControllerInput.WasFrenzyPressedThisFrame())
            OnFrenzyInput?.Invoke();

        if (GenericControllerInput.WasInteractPressedThisFrame())
            OnInteractHoldChanged?.Invoke(true);
        else if (GenericControllerInput.WasInteractReleasedThisFrame())
            OnInteractHoldChanged?.Invoke(false);
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
        Vector2 stick = value.Get<Vector2>();
        AimInput = stick.sqrMagnitude >= 0.04f ? stick : Vector2.zero;
    }

    public void OnFire(InputValue value)
    {
        PublishFireInput(value.isPressed);
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.control?.device is Mouse)
            return;

        PublishFireInput(true);
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (ctx.control?.device is Mouse)
            return;

        PublishFireInput(false);
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
            OnMoveInput?.Invoke(Vector2.zero);
            _mouseFireHeld = false;
            if (_firePublishedHeld)
            {
                _firePublishedHeld = false;
                OnFireInput?.Invoke(false);
            }
            OnInteractHoldChanged?.Invoke(false);
        }
    }
}