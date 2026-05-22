/// <summary>
/// Autoridade de rede para inimigos: IA só no servidor, estado de vida replicado, despawn via NGO.
/// </summary>

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HealthComponent))]
public class NetworkEnemyController : NetworkBehaviour
{
    [SerializeField] private float deathDespawnDelay = 0.4f;

    private EnemyMovement _movement;
    private EnemyTargetFinder _targetFinder;
    private EnemyAttack_Melee _meleAttack;
    private EnemyAttack_Ranged _rangedAttack;
    private EnemyAnimationHandler _animationHandler;
    private EnemyDropHandler _dropHandler;
    private EnemyHitStun _hitStun;
    private HealthComponent _health;
    private NavMeshAgent _agent;

    private NetworkVariable<float> _networkHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<float> _networkMaxHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> _networkIsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float _lastSyncedHealth = -1f;
    private bool _deathFinalized;
    private Coroutine _despawnCoroutine;
    private Coroutine _knockbackCoroutine;

    public bool IsDeadOnNetwork =>
        _health != null && (_deathFinalized || _health.IsDead || (IsSpawned && _networkIsDead.Value));

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _targetFinder = GetComponent<EnemyTargetFinder>();
        _meleAttack = GetComponent<EnemyAttack_Melee>();
        _rangedAttack = GetComponent<EnemyAttack_Ranged>();
        _animationHandler = GetComponent<EnemyAnimationHandler>();
        _dropHandler = GetComponent<EnemyDropHandler>();
        _hitStun = GetComponent<EnemyHitStun>();
        _health = GetComponent<HealthComponent>();
        _agent = GetComponent<NavMeshAgent>();

        _health.SetAllowDestroyOnDeath(false);
        _health.OnDied.AddListener(HandleDied);
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDied.RemoveListener(HandleDied);
    }

    public override void OnNetworkSpawn()
    {
        _deathFinalized = false;

        if (_animationHandler != null)
            _animationHandler.enabled = true;

        if (IsServer)
        {
            SetAIComponentsActive(true);

            _health.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);
            _health.OnHealthChanged.AddListener(HandleHealthChangedOnServer);
            StartCoroutine(SyncHealthAfterConfigsRoutine());
        }
        else
        {
            SetAIComponentsActive(false);
        }

        _networkHealth.OnValueChanged += HandleNetworkHealthChanged;
        _networkIsDead.OnValueChanged += HandleNetworkDeathChanged;

        if (!IsServer)
            ApplyClientMirror(_networkHealth.Value, _networkMaxHealth.Value, _networkIsDead.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _health != null)
            _health.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);

        _networkHealth.OnValueChanged -= HandleNetworkHealthChanged;
        _networkIsDead.OnValueChanged -= HandleNetworkDeathChanged;

        CancelDeathDespawnRoutine();
    }

    public void NotifyHealthInitialized()
    {
        if (!IsServer || _health == null) return;
        PublishHealthSnapshot();
    }

    private void HandleDied()
    {
        if (IsSpawned)
        {
            if (IsServer)
                FinalizeDeathOnServer();
            return;
        }

        ApplyDeathPresentation();
        if (_animationHandler != null)
            _animationHandler.PlayDeathAnimation();

        ScheduleDeathDespawn(deathDespawnDelay);
    }

    private void FinalizeDeathOnServer()
    {
        if (!IsServer || _deathFinalized) return;
        _deathFinalized = true;

        if (_health != null && _health.IsAlive)
        {
            EmitDamageDiagnostic(0f, _health.CurrentHealth, 0f, true,
                "FinalizeDeath: corrigindo estado vivo com morte pendente");
        }

        SetAIComponentsActive(false);
        ApplyDeathPresentation();

        _networkIsDead.Value = true;
        _networkHealth.Value = 0f;
        _lastSyncedHealth = 0f;

        if (_dropHandler != null)
            _dropHandler.TrySpawnDrop();

        PlayDeathVisualClientRpc();
        ScheduleDeathDespawn(GetDeathDespawnDelay());
    }

    private float GetDeathDespawnDelay()
    {
        if (_dropHandler != null)
        {
            float fromStats = _dropHandler.DeathDespawnDelay;
            if (fromStats > 0f)
                return fromStats;
        }

        return Mathf.Max(0.05f, deathDespawnDelay);
    }

    private void ScheduleDeathDespawn(float delay)
    {
        CancelDeathDespawnRoutine();
        _despawnCoroutine = StartCoroutine(DeathDespawnRoutine(delay));
    }

    private void CancelDeathDespawnRoutine()
    {
        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }

        CancelInvoke(nameof(DespawnEnemy));
    }

    private IEnumerator DeathDespawnRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        HideVisualsClientRpc();
        DespawnEnemy();
        _despawnCoroutine = null;
    }

    private void DespawnEnemy()
    {
        if (IsSpawned)
        {
            if (!IsServer)
                return;

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
                return;
            }
        }

        Destroy(gameObject);
    }

    private void ApplyDeathPresentation()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        foreach (var rb in GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private void PublishHealthSnapshot()
    {
        _networkMaxHealth.Value = _health.MaxHealth;
        _networkHealth.Value = _health.CurrentHealth;
        _lastSyncedHealth = _health.CurrentHealth;
    }

    private void HandleHealthChangedOnServer(float current, float max)
    {
        if (_health != null && _health.IsDead)
            current = 0f;

        _networkMaxHealth.Value = max;
        _networkHealth.Value = current;
        _lastSyncedHealth = current;

        if (_health != null && _health.IsDead)
            FinalizeDeathOnServer();
    }

    private void HandleNetworkHealthChanged(float oldValue, float newValue)
    {
        if (IsServer) return;
        ApplyClientMirror(newValue, _networkMaxHealth.Value, _networkIsDead.Value);
    }

    private void HandleNetworkDeathChanged(bool wasAlive, bool isDead)
    {
        if (!isDead || IsServer) return;

        ApplyClientMirror(0f, _networkMaxHealth.Value, true);
        SetAIComponentsActive(false);
        ApplyDeathPresentation();

        if (_animationHandler != null)
            _animationHandler.PlayDeathAnimation();
    }

    private void ApplyClientMirror(float current, float max, bool isDead)
    {
        if (_health == null) return;
        _health.ApplyNetworkMirror(current, max, isDead);
    }

    public bool ServerApplyDamage(float amount, ulong instigatorClientId)
    {
        if (!IsServer)
        {
            EmitDamageDiagnostic(amount, 0f, 0f, false, "REJECTED: not server");
            return false;
        }

        if (_deathFinalized || _health == null)
        {
            EmitDamageDiagnostic(amount, 0f, 0f, false, "REJECTED: already finalized or no health");
            return false;
        }

        if (_networkIsDead.Value && _health.IsAlive)
            _networkIsDead.Value = false;

        if (_health.IsDead || _deathFinalized)
        {
            EmitDamageDiagnostic(amount, _health.CurrentHealth, _health.CurrentHealth, true,
                "REJECTED: already dead");
            return false;
        }

        if (amount <= 0f)
        {
            EmitDamageDiagnostic(amount, _health.CurrentHealth, _health.CurrentHealth, false,
                "REJECTED: zero damage");
            return false;
        }

        float before = _health.CurrentHealth;
        _health.TakeDamage(amount, gameObject);

        if (_health.IsDead)
            FinalizeDeathOnServer();

        if (Mathf.Approximately(before, _health.CurrentHealth) && !_health.IsDead)
        {
            EmitDamageDiagnostic(amount, before, _health.CurrentHealth, false,
                "REJECTED: TakeDamage did not change health");
            return false;
        }

        if (!_health.IsDead)
        {
            _hitStun?.ApplyStun();
            PlayTakeDamageVisualClientRpc();
        }

        EmitDamageDiagnostic(amount, before, _health.CurrentHealth, _health.IsDead,
            _health.IsDead ? "OK lethal" : $"OK instigator={instigatorClientId}");

        float dealt = Mathf.Max(0f, before - _health.CurrentHealth);
        if (dealt > 0f)
            ShowDamageNumberClientRpc(dealt);

        return true;
    }

    [Rpc(SendTo.Server)]
    public void ApplyKnockbackRpc(Vector2 direction, float distance, float duration)
    {
        if (!IsServer || IsDeadOnNetwork || distance <= 0f || duration <= 0f) return;

        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(ServerKnockbackRoutine(direction, distance, duration));
    }

    private IEnumerator ServerKnockbackRoutine(Vector2 direction, float distance, float duration)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

        if (_agent != null)
            _agent.isStopped = true;
        if (_movement != null)
            _movement.enabled = false;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * distance);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (_agent != null && !IsDeadOnNetwork)
            _agent.isStopped = false;
        if (_movement != null && !IsDeadOnNetwork)
            _movement.enabled = true;

        _knockbackCoroutine = null;
    }

    [ClientRpc]
    private void ShowDamageNumberClientRpc(float amount)
    {
        GameEvents.InvokeDamageShown(amount, transform.position + Vector3.up * 0.5f);
    }

    private void EmitDamageDiagnostic(float amount, float before, float after, bool isDead, string source)
    {
        GameplayDiagnosticHub.Emit(new EnemyDamageDiagnostic(
            gameObject.name,
            NetworkObject != null ? NetworkObject.NetworkObjectId : 0,
            true,
            amount,
            before,
            after,
            isDead,
            source));
    }

    [ClientRpc]
    private void PlayTakeDamageVisualClientRpc()
    {
        if (_animationHandler != null)
            _animationHandler.PlayTakeDamageAnimation();

        if (TryGetComponent<SpriteBlink>(out var blink))
            blink.Blink();
    }

    [ClientRpc]
    private void PlayDeathVisualClientRpc()
    {
        if (_animationHandler != null)
            _animationHandler.PlayDeathAnimation();
    }

    [ClientRpc]
    private void HideVisualsClientRpc()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;
    }

    private IEnumerator SyncHealthAfterConfigsRoutine()
    {
        yield return null;
        if (_health == null) yield break;
        PublishHealthSnapshot();
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(float amount, ulong instigatorClientId)
    {
        ServerApplyDamage(amount, instigatorClientId);
    }

    private void SetAIComponentsActive(bool active)
    {
        if (_movement != null) _movement.enabled = active;
        if (_targetFinder != null) _targetFinder.enabled = active;
        if (_meleAttack != null) _meleAttack.enabled = active;
        if (_rangedAttack != null) _rangedAttack.enabled = active;
        if (_dropHandler != null) _dropHandler.enabled = active;
        if (_hitStun != null) _hitStun.enabled = active;
        if (_agent != null && active) _agent.enabled = true;
    }
}
