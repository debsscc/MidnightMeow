using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orientação do sprite: parado = mira; andando = movimento.
/// Durante o clip de ataque: trava no flip da mira (snap no início); libera ao terminar a anim.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class PlayerFacingController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private PlayerMeleeCombat playerMeleeCombat;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private NixChargeAbilityExecutor chargeExecutor;
    [SerializeField] private PlayerAnimationHandler animationHandler;

    [Header("Aim Facing")]
    [Tooltip("Enquanto parado, só vira se |aim.x| passar deste valor.")]
    [SerializeField] private float aimDeadZoneX = 0.15f;

    [Tooltip("Enquanto correndo, só usa input horizontal se |move.x| passar deste valor.")]
    [SerializeField] private float moveFacingThresholdX = 0.01f;

    public event Action<bool> OnFacingChanged;

    public bool FacingRight { get; private set; }

    private NetworkObject _networkObject;
    private NetworkPlayerHealth _networkHealth;
    private HealthComponent _healthComponent;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerAim == null) playerAim = GetComponent<PlayerAim>();
        if (playerMeleeCombat == null) playerMeleeCombat = GetComponent<PlayerMeleeCombat>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (chargeExecutor == null) chargeExecutor = GetComponent<NixChargeAbilityExecutor>();
        if (animationHandler == null) animationHandler = GetComponent<PlayerAnimationHandler>();
        _networkObject = GetComponent<NetworkObject>();
        _networkHealth = GetComponent<NetworkPlayerHealth>();
        _healthComponent = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot += SnapFacingToAimAtAttackStart;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnMeleeAttackStarted += SnapFacingToAimAtAttackStart;
    }

    private void OnDisable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot -= SnapFacingToAimAtAttackStart;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnMeleeAttackStarted -= SnapFacingToAimAtAttackStart;
    }

    private void LateUpdate()
    {
        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        if (IsFacingLocked())
            return;

        if (animationHandler != null && animationHandler.IsPrimaryAttackAnimationPlaying())
            return;

        bool? desiredFacing = ResolveDesiredFacing();
        if (!desiredFacing.HasValue)
            return;

        PublishFacing(desiredFacing.Value);
    }

    private void SnapFacingToAimAtAttackStart()
    {
        if (IsFacingLocked())
            return;

        TryPublishAimFacing();
    }

    private bool IsFacingLocked()
    {
        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return true;

        if (_networkHealth != null && _networkHealth.IsSpawned && _networkHealth.IsUnconscious)
            return true;

        if (_healthComponent != null && _healthComponent.IsDead)
            return true;

        return false;
    }

    private bool? ResolveDesiredFacing()
    {
        if (chargeExecutor != null && chargeExecutor.IsCharging)
        {
            Vector2 chargeDir = chargeExecutor.ActiveChargeDirection;
            if (Mathf.Abs(chargeDir.x) >= moveFacingThresholdX)
                return chargeDir.x > 0f;
            return null;
        }

        if (ShouldPreferMouseAimFacing())
        {
            bool? aimFacing = ResolveAimFacing();
            if (aimFacing.HasValue)
                return aimFacing;
        }

        if (playerMovement != null && playerMovement.IsMoving)
        {
            Vector2 move = playerMovement.MoveDirection;
            if (Mathf.Abs(move.x) >= moveFacingThresholdX)
                return move.x > 0f;
        }

        return ResolveAimFacing();
    }

    private bool ShouldPreferMouseAimFacing()
    {
        if (Mouse.current == null || playerAim == null)
            return false;

        if (playerMovement != null && playerMovement.IsMoving)
            return false;

        return true;
    }

    private bool? ResolveAimFacing()
    {
        if (playerAim != null && playerAim.TryGetAimDirection(out Vector2 aim, out _))
        {
            if (Mathf.Abs(aim.x) < aimDeadZoneX)
                return null;
            return aim.x > 0f;
        }

        return null;
    }

    private void TryPublishAimFacing()
    {
        bool? aimFacing = ResolveAimFacing();
        if (aimFacing.HasValue)
            PublishFacing(aimFacing.Value);
    }

    private void PublishFacing(bool facingRight)
    {
        if (FacingRight == facingRight)
            return;

        FacingRight = facingRight;
        OnFacingChanged?.Invoke(facingRight);

        if (_networkObject == null || !_networkObject.IsSpawned)
            animationHandler?.ApplyNetworkFacing(facingRight);
    }
}
