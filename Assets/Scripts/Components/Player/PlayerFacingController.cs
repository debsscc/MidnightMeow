using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Orientação híbrida do sprite: parado = mouse (eixo X); andando = direção horizontal do movimento.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class PlayerFacingController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private PlayerMeleeCombat playerMeleeCombat;
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
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (chargeExecutor == null) chargeExecutor = GetComponent<NixChargeAbilityExecutor>();
        if (animationHandler == null) animationHandler = GetComponent<PlayerAnimationHandler>();
        _networkObject = GetComponent<NetworkObject>();
        _networkHealth = GetComponent<NetworkPlayerHealth>();
        _healthComponent = GetComponent<HealthComponent>();
    }

    private void LateUpdate()
    {
        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        if (IsFacingLocked())
            return;

        if (playerMeleeCombat != null && playerMeleeCombat.IsAttacking)
            return;

        bool? desiredFacing = ResolveDesiredFacing();
        if (!desiredFacing.HasValue)
            return;

        PublishFacing(desiredFacing.Value);
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

        if (playerMovement != null && playerMovement.IsMoving)
        {
            Vector2 move = playerMovement.MoveDirection;
            if (Mathf.Abs(move.x) >= moveFacingThresholdX)
                return move.x > 0f;
        }

        if (playerAim != null && playerAim.TryGetAimDirection(out Vector2 aim, out _))
        {
            if (Mathf.Abs(aim.x) < aimDeadZoneX)
                return null;
            return aim.x > 0f;
        }

        return null;
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
