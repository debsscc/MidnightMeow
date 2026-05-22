///* ----------------------------------------------------------------
// DESCRIÇÃO: Controla a habilidade de Dash do jogador, incluindo cooldown,
// movimento físico e travessia temporária de layers específicos.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerDash : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerStats stats;

    [Header("Collision Bypass")]
    [SerializeField] private LayerMask passThroughLayer;

    [Header("Failsafe")]
    [SerializeField] private float dashFailsafeExtraSeconds = 0.35f;

    public event Action OnDashStarted;
    public event Action OnDashEnded;

    private Rigidbody2D _rb;
    private NetworkObject _networkObject;
    private Vector2 _currentMoveDirection = Vector2.up;
    private bool _isDashing;
    private float _lastDashEndTime = -Mathf.Infinity;
    private Coroutine _dashRoutine;
    private float _dashFailsafeDeadline = -1f;

    public bool IsDashing => _dashRoutine != null;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _networkObject = GetComponent<NetworkObject>();
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        RefreshDashStats();
    }

    private void OnEnable()
    {
        if (inputHandler == null) return;
        inputHandler.OnMoveInput += UpdateMoveDirection;
        inputHandler.OnDashInput += HandleDashInput;
        RefreshDashStats();
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnMoveInput -= UpdateMoveDirection;
            inputHandler.OnDashInput -= HandleDashInput;
        }

        InterruptDash("OnDisable");
    }

    private void Update()
    {
        if (!IsDashing || _dashFailsafeDeadline < 0f) return;

        if (Time.unscaledTime > _dashFailsafeDeadline)
            InterruptDash("failsafe-timeout");
    }

    private void UpdateMoveDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
            _currentMoveDirection = direction.normalized;
    }

    private void HandleDashInput()
    {
        if (IsDashing || stats == null) return;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

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
            return;
        }

        float duration = _currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration;
        if (duration <= 0f || (_currentDashSpeed <= 0f && stats.dashSpeed <= 0f))
        {
            GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
                gameObject.name,
                "rejected-invalid-stats",
                duration,
                _currentDashSpeed,
                _networkObject != null && _networkObject.IsOwner,
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
            return;
        }

        _dashFailsafeDeadline = Time.unscaledTime + duration + dashFailsafeExtraSeconds;
        _dashRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;

        float duration = _currentDashDuration > 0f ? _currentDashDuration : stats.dashDuration;
        float speed = _currentDashSpeed > 0f ? _currentDashSpeed : stats.dashSpeed;

        OnDashStarted?.Invoke();
        GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
            gameObject.name,
            "start",
            duration,
            speed,
            _networkObject != null && _networkObject.IsOwner,
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));

        int playerLayer = gameObject.layer;
        int[] ignoredLayers = GetLayersFromMask(passThroughLayer);

        foreach (int layer in ignoredLayers)
            Physics2D.IgnoreLayerCollision(playerLayer, layer, true);

        Vector2 dashDirection = _currentMoveDirection.sqrMagnitude > 0.0001f
            ? _currentMoveDirection
            : Vector2.up;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_rb != null)
                _rb.linearVelocity = dashDirection * speed;

            Shadow.me?.Sombras_skill();
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        foreach (int layer in ignoredLayers)
            Physics2D.IgnoreLayerCollision(playerLayer, layer, false);

        _isDashing = false;
        _dashRoutine = null;
        _dashFailsafeDeadline = -1f;
        _lastDashEndTime = Time.time;

        OnDashEnded?.Invoke();
        GameplayDiagnosticHub.EmitPlayerDash(new PlayerDashDiagnostic(
            gameObject.name,
            "complete",
            duration,
            speed,
            _networkObject != null && _networkObject.IsOwner,
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer));
    }

    private void InterruptDash(string reason)
    {
        if (_dashRoutine != null)
        {
            StopCoroutine(_dashRoutine);
            _dashRoutine = null;
        }

        if (!_isDashing)
        {
            _dashFailsafeDeadline = -1f;
            return;
        }

        int playerLayer = gameObject.layer;
        foreach (int layer in GetLayersFromMask(passThroughLayer))
            Physics2D.IgnoreLayerCollision(playerLayer, layer, false);

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _isDashing = false;
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

    private float _currentDashSpeed;
    private float _currentDashCooldown;
    private float _currentDashDuration;
}
