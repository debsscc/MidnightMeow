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
    public event Action OnFireInput;
    public event Action OnAbilityInput;
    public event Action OnFrenzyInput;

    // M�todos chamados pelo componente PlayerInput 
    // O nome deve bater com o nome da Action Map (ex: "Move", "Fire")
    public void OnMove(InputValue value)
    {
        OnMoveInput?.Invoke(value.Get<Vector2>());
    }

    public void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            OnFireInput?.Invoke();
        }
    }
    public void OnAbility(InputValue value)
    {
        if (value.isPressed)
        {
            OnAbilityInput?.Invoke();
        }
    }
    public void OnFrenzy(InputValue value)
    {
        if (value.isPressed)
        {
            OnFrenzyInput?.Invoke();
        }
    }
}