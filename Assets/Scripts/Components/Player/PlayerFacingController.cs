using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Orientação do sprite: andando no X → movimento; parado → mira/cursor.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class PlayerFacingController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private PlayerMeleeCombat playerMeleeCombat;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private NixChargeAbilityExecutor chargeExecutor;
    [SerializeField] private PlayerAnimationHandler animationHandler;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Aim Facing")]
    [Tooltip("Em idle, só vira se |cursor.x - player.x| em pixels de tela passar deste valor.")]
    [SerializeField] private float aimDeadZoneScreenPx = 8f;

    [Tooltip("Fallback se não houver câmera: |aim.x| normalizado.")]
    [SerializeField] private float aimDeadZoneX = 0.08f;

    [Tooltip("Só trata como 'andando no X' acima deste |move.x|.")]
    [SerializeField] private float moveFacingThresholdX = 0.2f;

    [SerializeField] private bool debugFacingLogs;

    public event Action<bool> OnFacingChanged;

    public bool FacingRight { get; private set; }

    private NetworkObject _networkObject;
    private NetworkPlayerHealth _networkHealth;
    private HealthComponent _healthComponent;
    private float _nextDebugLogTime;

    private void Awake() => ResolveRefs();

    private void Start() => ResolveRefs();

    private void ResolveRefs()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerAim == null) playerAim = GetComponent<PlayerAim>();
        if (playerMeleeCombat == null) playerMeleeCombat = GetComponent<PlayerMeleeCombat>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (chargeExecutor == null) chargeExecutor = GetComponent<NixChargeAbilityExecutor>();
        if (animationHandler == null) animationHandler = GetComponent<PlayerAnimationHandler>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
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

        if (IsFacingLockedByGameplay())
            return;

        bool? desiredFacing = ResolveDesiredFacing();
        if (!desiredFacing.HasValue)
        {
            ApplyFacingVisual(FacingRight);
            return;
        }

        PublishFacing(desiredFacing.Value);
        LogFacingDebug(desiredFacing.Value);
    }

    private void SnapFacingToAimAtAttackStart()
    {
        if (IsFacingLockedByGameplay())
            return;

        bool? aimFacing = ResolveAimFacing();
        if (aimFacing.HasValue)
            PublishFacing(aimFacing.Value);
    }

    private bool IsFacingLockedByGameplay()
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

        // Andando no X: movimento manda.
        if (playerMovement != null
            && playerMovement.IsMoving
            && Mathf.Abs(playerMovement.MoveDirection.x) >= moveFacingThresholdX)
        {
            return playerMovement.MoveDirection.x > 0f;
        }

        // Idle / só vertical: cursor na tela (não depende de mira mundo quebrada).
        return ResolveAimFacing();
    }

    private bool? ResolveAimFacing()
    {
        if (PlayerPointerInput.TryGetScreenPosition(out Vector2 mouseScreen))
        {
            Camera cam = ResolveFacingCamera();
            if (cam != null)
            {
                Vector3 playerScreen = cam.WorldToScreenPoint(transform.position);
                float dx = mouseScreen.x - playerScreen.x;
                if (Mathf.Abs(dx) >= aimDeadZoneScreenPx)
                    return dx > 0f;
            }
        }

        if (playerAim == null)
            return null;

        playerAim.RefreshAim();
        if (!playerAim.TryGetAimDirection(out Vector2 aim, out _))
            return null;

        if (Mathf.Abs(aim.x) < aimDeadZoneX)
            return null;

        return aim.x > 0f;
    }

    private Camera ResolveFacingCamera()
    {
        Camera multiplayerCamera = MultiplayerCameraController.Instance != null
            ? MultiplayerCameraController.Instance.MainCamera
            : null;

        if (multiplayerCamera != null && multiplayerCamera.isActiveAndEnabled)
            return multiplayerCamera;

        return Camera.main;
    }

    private void LogFacingDebug(bool desired)
    {
        if (!debugFacingLogs || Time.unscaledTime < _nextDebugLogTime)
            return;

        _nextDebugLogTime = Time.unscaledTime + 0.5f;
        bool flipX = spriteRenderer != null && spriteRenderer.flipX;
        PlayerPointerInput.TryGetScreenPosition(out Vector2 mouse);
        Debug.Log(
            $"[PlayerFacing] desired={desired} FacingRight={FacingRight} flipX={flipX} mouseScreen={mouse} " +
            $"isMoving={playerMovement != null && playerMovement.IsMoving}",
            this);
    }

    private void PublishFacing(bool facingRight)
    {
        bool changed = FacingRight != facingRight;
        FacingRight = facingRight;

        if (changed)
            OnFacingChanged?.Invoke(facingRight);

        ApplyFacingVisual(facingRight);
    }

    private void ApplyFacingVisual(bool facingRight)
    {
        bool flipX = !facingRight;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.flipX = flipX;

        animationHandler?.ApplyNetworkFacing(facingRight);
    }
}
