///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o movimento do jogador com base no input recebido.
// ---------------------------------------------------------------- */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
// Usei RequireComponent para garantir que os componentes necessários estejam presentes
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    private Rigidbody2D _rb;
    private PlayerInputHandler _input;

    private Vector2 _moveDirection;

    public event Action<bool> OnFlipSprite;

    private bool facingRight = false;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    // Assina e desassina eventos de input
    private void OnEnable()
    {
        _input.OnMoveInput += HandleMoveInput;
    }

    private void OnDisable()
    {
        _input.OnMoveInput -= HandleMoveInput;
    }

    private void HandleMoveInput(Vector2 direction)
    {
        _moveDirection = direction;
        if (_moveDirection.x > 0 && !facingRight)
        {
            print($"Flipping Sprite to Right");
            OnFlipSprite?.Invoke(true);
            facingRight = true;
        }
        else if (_moveDirection.x < 0 && facingRight)
        {
            OnFlipSprite?.Invoke(false);
            facingRight = false;
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _moveDirection * stats.moveSpeed;
    }
}