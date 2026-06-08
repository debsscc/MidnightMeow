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

    public event Action OnDashStarted;
    public event Action OnDashEnded;

    private Rigidbody2D _rb;
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
        _networkObject = GetComponent<NetworkObject>();
        _debugHost = GetComponent<AbilityDebugVisualHost>();
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        _playerLayer = gameObject.layer;
        RefreshDashStats();
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
        if (!_isDashing) return;

        if (_rb != null)
            _rb.linearVelocity = _activeDashDirection * _dashSpeedActive;

        Shadow.me?.Sombras_skill();

        _dashTimeRemaining -= Time.fixedDeltaTime;
        if (_dashTimeRemaining <= 0f)
            CompleteDash();
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
