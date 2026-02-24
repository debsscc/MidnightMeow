///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Manipula o input do jogador e dispara eventos que outros componentes podem assinar.
// ---------------------------------------------------------------- */
using UnityEngine;
using UnityEngine.InputSystem; 
using System; 

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    // Eventos que os outros scripts do Player ir�o assinar
    public event Action<Vector2> OnMoveInput;
    // Now reports whether the fire button is pressed (true) or released (false)
    public event Action<bool> OnFireInput;
    public event Action OnAbilityInput;
    public event Action OnFrenzyInput;

    // Methods called by PlayerInput are not relied on for fire state anymore.
    // We subscribe directly to the underlying InputAction to reliably detect started/canceled.
    private PlayerInput _playerInput;
    private InputAction _fireAction;
    private bool _isPaused = false;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            // Try to find the 'Fire' action in the current action map or asset
            if (_playerInput.actions != null)
            {
                _fireAction = _playerInput.actions.FindAction("Fire");
            }
        }
    }

    private void OnEnable()
    {
        if (_fireAction != null)
        {
            _fireAction.started += OnFireStarted;
            _fireAction.canceled += OnFireCanceled;
        }
        GameEvents.OnPauseChanged += HandlePauseChanged;
    }

    private void OnDisable()
    {
        if (_fireAction != null)
        {
            _fireAction.started -= OnFireStarted;
            _fireAction.canceled -= OnFireCanceled;
        }
        GameEvents.OnPauseChanged -= HandlePauseChanged;
    }

    public void OnMove(InputValue value)
    {
        if (_isPaused) return;
        OnMoveInput?.Invoke(value.Get<Vector2>());
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

    public void OnAbility(InputValue value)
    {
        if (_isPaused) return;

        if (value.isPressed)
        {
            OnAbilityInput?.Invoke();
        }
    }

    public void OnFrenzy(InputValue value)
    {
        if (_isPaused) return;

        if (value.isPressed)
        {
            OnFrenzyInput?.Invoke();
        }
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