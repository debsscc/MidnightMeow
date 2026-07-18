///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Controla o movimento do jogador, respeitando o estado de Dash.
// ---------------------------------------------------------------- */

using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    //-----DustParticle-------
    public ParticleSystem dustParticle; 

    [SerializeField] private PlayerStats stats;
    private PlayerStats _runtimeStats;
    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private PlayerAdrenaline _adrenaline;
    
    // Referência ao novo componente de Dash
    private PlayerDash _dash;
    private NixChargeAbilityExecutor _charge;
    private KnockbackReceiver _knockback;

    private Vector2 _moveDirection;

    public Vector2 MoveDirection => _moveDirection;
    public bool IsMoving => _isMoving;

    // Evento disparado quando o jogador começa a se mover
    public event Action OnMovement;
    // Evento disparado quando o jogador para
    public event Action OnStop;

    private bool _isMoving = false;
    private int _lastDustFlipSign;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
        _adrenaline = GetComponent<PlayerAdrenaline>();
        _dash = GetComponent<PlayerDash>(); // Busca o componente de Dash
        _charge = GetComponent<NixChargeAbilityExecutor>();
        _knockback = GetComponent<KnockbackReceiver>();

        if (_rb != null)
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        _input.OnMoveInput += HandleMoveInput;
    }

    private void OnDisable()
    {
        _input.OnMoveInput -= HandleMoveInput;
    }

    public void HandleMoveInput(Vector2 direction)
    {
        _moveDirection = direction;
        
        // Dispara eventos OnMovement / OnStop quando o estado de movimento muda
        bool movingNow = direction.sqrMagnitude > 0.0001f;
        if (movingNow && !_isMoving)
        {
            OnMovement?.Invoke();
            GameEvents.InvokeTutorialMoveExecuted();
            _isMoving = true;
        }
        else if (!movingNow && _isMoving)
        {
            OnStop?.Invoke();
            _isMoving = false;
        }

        // Poeira ao mudar direção horizontal do input
        if (_moveDirection.x > 0f && _lastDustFlipSign <= 0)
        {
            _lastDustFlipSign = 1;
            CreateDust();
        }
        else if (_moveDirection.x < 0f && _lastDustFlipSign >= 0)
        {
            _lastDustFlipSign = -1;
            CreateDust();
        }
        else if (_moveDirection.x == 0f)
            _lastDustFlipSign = 0;
    }

    public void FreezeForPause()
    {
        _moveDirection = Vector2.zero;
        _isMoving = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (GameEvents.IsPaused)
        {
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Regra de Ouro (Baixo Acoplamento): 
        // Se o Dash ou Knockback estiverem ocorrendo, o PlayerMovement cede o controle do Rigidbody.
        if (_dash != null && _dash.IsDashing)
            return;

        if (_charge != null && _charge.IsCharging)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (_knockback != null && _knockback.IsKnockedBack)
            return;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (TryGetComponent<PlayerMeleeCombat>(out var melee) && melee != null && melee.IsAttacking)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMultiplier = _adrenaline != null ? _adrenaline.GetSpeedMultiplier() : 1f;
        PlayerStats activeStats = _runtimeStats != null ? _runtimeStats : stats;
        if (activeStats == null)
            return;

        _rb.linearVelocity = _moveDirection * activeStats.moveSpeed * speedMultiplier;
    }

    public void ApplyRuntimeStats(PlayerStats runtimeStats)
    {
        _runtimeStats = runtimeStats;
    }

    void CreateDust()
    {
        if (dustParticle != null)
            dustParticle.Play();
    }
}