///* ----------------------------------------------------------------
// DESCRIÇÃO: Controla a habilidade de Dash do jogador. Ativação via PlayerAbilityHandler.
// ---------------------------------------------------------------- */

using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public class PlayerDash : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerStats stats;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private float dashGizmoWidth = 0.6f;

    [Header("Collision Bypass")]
    [SerializeField] private LayerMask passThroughLayer;

    [Header("Distance")]
    [SerializeField] private float dashDistanceMultiplier = 1f;

    [Header("Failsafe")]
    [SerializeField] private float dashFailsafeExtraSeconds = 0.35f;

    [Header("Collision")]
    [SerializeField] private Collider2D dashCollider;
    [SerializeField] private float dashCollisionSkin = 0.04f;

    public event Action OnDashStarted;
    public event Action OnDashEnded;

    private Rigidbody2D _rb;
    private Shadow _dashGhosting;
    private NetworkObject _networkObject;
    private AbilityDebugVisualHost _debugHost;
    private Vector2 _currentMoveDirection = Vector2.up;
    private Vector2 _activeDashDirection = Vector2.up;
    private bool _isDashing;
    private float _dashTimeRemaining;
    private float _dashSpeedActive;
    private float _lastDashEndTime = -Mathf.Infinity;
    private float _dashFailsafeDeadline = -1f;
    private int[] _ignoredLayers = Array.Empty<int>();
    private int _playerLayer;
    private ContactFilter2D _blockFilter;
    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[16];
    private CollisionDetectionMode2D _defaultCollisionDetection;

    public bool IsDashing => _isDashing;

    public float GetCooldownDuration() =>
        _currentDashCooldown > 0f ? _currentDashCooldown : stats != null ? stats.dashCooldown : 1f;

    public float GetCooldownRemaining()
    {
        if (stats == null)
            return 0f;

        float cooldown = GetCooldownDuration();
        return Mathf.Max(0f, _lastDashEndTime + cooldown - Time.time);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _dashGhosting = GetComponent<Shadow>();
        _networkObject = GetComponent<NetworkObject>();
        _debugHost = GetComponent<AbilityDebugVisualHost>();
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        _playerLayer = gameObject.layer;
        if (dashCollider == null)
            dashCollider = GetComponent<Collider2D>();
        RebuildBlockFilter();
        RefreshDashStats();
        if (_rb != null)
            _defaultCollisionDetection = _rb.collisionDetectionMode;
    }

    private static int LayerMaskToBit(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? 1 << layer : 0;
    }

    private void OnEnable()
    {
        if (inputHandler != null)
            inputHandler.OnMoveInput += UpdateMoveDirection;
        RefreshDashStats();
    }

    private void OnDisable()
    {
        if (inputHandler != null)
            inputHandler.OnMoveInput -= UpdateMoveDirection;

        InterruptDash("OnDisable");
    }

    private void Update()
    {
        if (!_isDashing || _dashFailsafeDeadline < 0f) return;

        if (Time.unscaledTime > _dashFailsafeDeadline)
            InterruptDash("failsafe-timeout");
    }

    private void FixedUpdate()
    {
        if (!_isDashing || _rb == null) return;

        float step = _dashSpeedActive * Time.fixedDeltaTime;
        if (TryGetBlockingHit(step, out float allowedDistance))
        {
            float travel = Mathf.Max(0f, allowedDistance - dashCollisionSkin);
            if (travel > 0f)
                _rb.MovePosition(_rb.position + _activeDashDirection * travel);

            _rb.linearVelocity = Vector2.zero;
            InterruptDash("collision-block");
            return;
        }

        _rb.linearVelocity = Vector2.zero;
        _rb.MovePosition(_rb.position + _activeDashDirection * step);

        _dashGhosting?.Sombras_skill();

        _dashTimeRemaining -= Time.fixedDeltaTime;
        if (_dashTimeRemaining <= 0f)
            CompleteDash();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isDashing || collision == null || IsPassThroughCollision(collision))
            return;

        if (_rb != null && collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.GetContact(0);
            _rb.position = contact.point + contact.normal * dashCollisionSkin;
            _rb.linearVelocity = Vector2.zero;
        }

        InterruptDash("collision-block");
    }

    private void UpdateMoveDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
            _currentMoveDirection = direction.normalized;
    }

    /// <summary>
    /// Chamado pelo PlayerAbilityHandler após validar bloqueio e cooldown.
    /// </summary>
    public bool TryStartDash()
    {
        if (_isDashing || stats == null) return false;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return false;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return false;

        float cooldown = _currentDashCooldown > 0f ? _currentDashCooldown : stats.dashCooldown;
        if (Time.time < _lastDashEndTime + cooldown)
        {
            GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
                gameObject.name,
                "rejected-cooldown",
                cooldown,
                0f,
                _networkObject != null && _networkObject.IsOwner,
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
            return false;
        }

        float duration = (_currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration)
                         * Mathf.Max(1f, dashDistanceMultiplier);
        float speed = _currentDashSpeed > 0f ? _currentDashSpeed : stats.dashSpeed;
        if (duration <= 0f || speed <= 0f)
        {
            GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
                gameObject.name,
                "rejected-invalid-stats",
                duration,
                speed,
                _networkObject != null && _networkObject.IsOwner,
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
            return false;
        }

        _activeDashDirection = _currentMoveDirection.sqrMagnitude > 0.0001f
            ? _currentMoveDirection
            : Vector2.up;

        _dashTimeRemaining = duration;
        _dashSpeedActive = speed;
        _isDashing = true;
        _dashFailsafeDeadline = Time.unscaledTime + duration + dashFailsafeExtraSeconds;

        _ignoredLayers = GetLayersFromMask(passThroughLayer);
        foreach (int layer in _ignoredLayers)
            Physics2D.IgnoreLayerCollision(_playerLayer, layer, true);

        if (_rb != null)
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (_debugHost != null)
        {
            float distance = speed * duration;
            _debugHost.ShowDash((Vector2)transform.position, _activeDashDirection, distance, dashGizmoWidth);
        }

        OnDashStarted?.Invoke();
        GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
            gameObject.name,
            "start",
            duration,
            speed,
            _networkObject != null && _networkObject.IsOwner,
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
        return true;
    }

    public float GetDashLockDuration()
    {
        if (stats == null) return 0.2f;
        return _currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration;
    }

    private void CompleteDash()
    {
        if (!_isDashing) return;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        RestoreCollisions();
        _isDashing = false;
        _dashTimeRemaining = 0f;
        _dashFailsafeDeadline = -1f;
        _lastDashEndTime = Time.time;

        OnDashEnded?.Invoke();
        GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
            gameObject.name,
            "complete",
            _currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration,
            _dashSpeedActive,
            _networkObject != null && _networkObject.IsOwner,
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
    }

    private void InterruptDash(string reason)
    {
        if (!_isDashing)
        {
            _dashFailsafeDeadline = -1f;
            return;
        }

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        RestoreCollisions();
        _isDashing = false;
        _dashTimeRemaining = 0f;
        _dashFailsafeDeadline = -1f;
        _lastDashEndTime = Time.time;

        OnDashEnded?.Invoke();
        GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
            gameObject.name,
            reason,
            0f,
            0f,
            _networkObject != null && _networkObject.IsOwner,
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
    }

    private void RestoreCollisions()
    {
        foreach (int layer in _ignoredLayers)
            Physics2D.IgnoreLayerCollision(_playerLayer, layer, false);
        _ignoredLayers = Array.Empty<int>();

        if (_rb != null)
            _rb.collisionDetectionMode = _defaultCollisionDetection;
    }

    private static int[] GetLayersFromMask(LayerMask mask)
    {
        if (mask.value == 0) return Array.Empty<int>();

        var layers = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
                layers.Add(i);
        }

        return layers.ToArray();
    }

    public void InitializeBaseStats() => RefreshDashStats();

    public void ApplyRuntimeStats(PlayerStats runtimeStats)
    {
        stats = runtimeStats;
        RefreshDashStats();
    }

    public void ApplyPassThroughLayers(LayerMask layers)
    {
        passThroughLayer = layers;
        RebuildBlockFilter();
    }

    private void RebuildBlockFilter()
    {
        _blockFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false
        };
        _blockFilter.SetLayerMask(~passThroughLayer.value);
    }

    private bool TryGetBlockingHit(float distance, out float allowedDistance)
    {
        allowedDistance = distance;
        if (dashCollider == null || distance <= 0f)
            return false;

        float closest = float.MaxValue;

        int castCount = dashCollider.Cast(_activeDashDirection, _blockFilter, _castHits, distance);
        closest = Mathf.Min(closest, GetClosestValidCastDistance(castCount));

        if (_rb != null)
        {
            int bodyCastCount = _rb.Cast(_activeDashDirection, _castHits, distance);
            closest = Mathf.Min(closest, GetClosestValidCastDistance(bodyCastCount));
        }

        Bounds bounds = dashCollider.bounds;
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.85f);
        RaycastHit2D circleHit = Physics2D.CircleCast(
            bounds.center,
            radius,
            _activeDashDirection,
            distance,
            _blockFilter.layerMask);

        if (circleHit.collider != null && !IsPassThroughCollider(circleHit.collider) && circleHit.collider != dashCollider)
            closest = Mathf.Min(closest, circleHit.distance);

        if (closest >= float.MaxValue)
            return false;

        allowedDistance = closest;
        return true;
    }

    private float GetClosestValidCastDistance(int count)
    {
        float closest = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = _castHits[i].collider;
            if (hitCollider == null || hitCollider == dashCollider || IsPassThroughCollider(hitCollider))
                continue;

            if (_castHits[i].distance < closest)
                closest = _castHits[i].distance;
        }

        return closest;
    }

    private bool IsPassThroughCollider(Collider2D collider) =>
        collider != null && ((1 << collider.gameObject.layer) & passThroughLayer.value) != 0;

    private bool IsPassThroughCollision(Collision2D collision) =>
        collision != null && collision.collider != null && IsPassThroughCollider(collision.collider);

    public void ApplyFailsafeExtraSeconds(float seconds)
    {
        if (seconds > 0f)
            dashFailsafeExtraSeconds = seconds;
    }

    private void RefreshDashStats()
    {
        if (stats == null) return;
        _currentDashSpeed = stats.dashSpeed;
        _currentDashCooldown = stats.dashCooldown;
        _currentDashDuration = stats.dashDuration;
    }

    public void SetDashUpgrades(float extraSpeed, float cooldownReduction)
    {
        RefreshDashStats();
        _currentDashSpeed += extraSpeed;
        _currentDashCooldown = Mathf.Max(0.1f, _currentDashCooldown - cooldownReduction);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || stats == null) return;

        float duration = _currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration;
        float speed = _currentDashSpeed > 0f ? _currentDashSpeed : stats.dashSpeed;
        Vector2 direction = Application.isPlaying ? _activeDashDirection : _currentMoveDirection;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        AbilityDebugGizmoUtility.DrawDash(
            transform.position,
            direction,
            speed * duration,
            dashGizmoWidth,
            new Color(0.2f, 0.95f, 0.95f, 0.25f),
            new Color(0.6f, 1f, 1f, 0.9f));
    }

    private float _currentDashSpeed;
    private float _currentDashCooldown;
    private float _currentDashDuration;
}
