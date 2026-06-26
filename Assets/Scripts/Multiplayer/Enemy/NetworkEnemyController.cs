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
    [SerializeField] private float deathPresentationFallbackSeconds = 8f;

    private EnemyMovement _movement;
    private EnemyTargetFinder _targetFinder;
    private EnemyAttack_Melee _meleAttack;
    private EnemyAttack_Ranged _rangedAttack;
    private EnemyTelegraphedAttacker _telegraphedAttacker;
    private EnemyAnimationHandler _animationHandler;
    private EnemyDropHandler _dropHandler;
    private EnemyHitStun _hitStun;
    private DissolveEffect _dissolveEffect;
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

    private readonly NetworkVariable<float> _animMoveSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _animFacingFlipX = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> _animAttackSequence = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _animIsAttacking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int HashOnAttack = Animator.StringToHash("OnAttack");
    private static readonly int HashOnDie = Animator.StringToHash("OnDie");
    private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private byte _lastClientAttackSequence;

    public bool DrivesAnimatorOnClient => IsSpawned && !IsServer;

    private float _lastSyncedHealth = -1f;
    private ulong _lastInstigatorClientId;
    private bool _deathFinalized;
    private bool _deathVisualsPlayed;
    private bool _deathPresentationCompleted;
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
        _telegraphedAttacker = GetComponent<EnemyTelegraphedAttacker>();
        _animationHandler = GetComponent<EnemyAnimationHandler>();
        _dropHandler = GetComponent<EnemyDropHandler>();
        _hitStun = GetComponent<EnemyHitStun>();
        _dissolveEffect = GetComponent<DissolveEffect>();
        _health = GetComponent<HealthComponent>();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _health.SetAllowDestroyOnDeath(false);
        _health.OnDied.AddListener(HandleDied);

        if (GetComponent<EnemyHealthBarDisplay>() == null)
            gameObject.AddComponent<EnemyHealthBarDisplay>();

        if (GetComponent<EnemySlowEffect>() == null)
            gameObject.AddComponent<EnemySlowEffect>();

        if (GetComponent<EnemyPhysicsBody>() == null)
            gameObject.AddComponent<EnemyPhysicsBody>();

        if (GetComponent<EnemySpawnPresentation>() == null)
            gameObject.AddComponent<EnemySpawnPresentation>();
    }

    public override void OnDestroy()
    {
        if (_health != null)
            _health.OnDied.RemoveListener(HandleDied);
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _deathFinalized = false;
        _deathVisualsPlayed = false;
        _deathPresentationCompleted = false;

        if (_animationHandler != null)
            _animationHandler.enabled = true;

        EnsureTelegraphClientComponents();

        _animMoveSpeed.OnValueChanged += HandleAnimMoveSpeedChanged;
        _animFacingFlipX.OnValueChanged += HandleAnimFacingChanged;
        _animAttackSequence.OnValueChanged += HandleAnimAttackSequenceChanged;
        _animIsAttacking.OnValueChanged += HandleAnimIsAttackingChanged;

        if (IsServer)
        {
            SetAIComponentsActive(true);
            WireTelegraphedAttackerProjectileSpawn();
            WireAnimationPublishers();

            _health.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);
            _health.OnHealthChanged.AddListener(HandleHealthChangedOnServer);
            StartCoroutine(SyncHealthAfterConfigsRoutine());
        }
        else
        {
            SetAIComponentsActive(false);
            ApplyClientAnimationState(
                _animMoveSpeed.Value,
                _animFacingFlipX.Value,
                _animAttackSequence.Value,
                _animIsAttacking.Value,
                false);
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
        _animMoveSpeed.OnValueChanged -= HandleAnimMoveSpeedChanged;
        _animFacingFlipX.OnValueChanged -= HandleAnimFacingChanged;
        _animAttackSequence.OnValueChanged -= HandleAnimAttackSequenceChanged;
        _animIsAttacking.OnValueChanged -= HandleAnimIsAttackingChanged;

        UnwireAnimationPublishers();
        CancelDeathDespawnRoutine();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || _movement == null)
            return;

        if (_deathFinalized || IsDeadOnNetwork)
            return;

        float speed = _movement.GetCurrentSpeed();
        if (!Mathf.Approximately(_animMoveSpeed.Value, speed))
            _animMoveSpeed.Value = speed;

        if (_animationHandler != null)
        {
            bool isAttacking = _animationHandler.IsAttackingForAnimator;
            if (_animIsAttacking.Value != isAttacking)
                _animIsAttacking.Value = isAttacking;
        }
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
        PlayDeathVisuals();

        ScheduleDeathPresentationFallback();
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

        GameEvents.InvokeEnemyKilledByPlayer(_lastInstigatorClientId);

        _animMoveSpeed.Value = 0f;
        _animIsAttacking.Value = false;

        PlayDeathVisualClientRpc();
        ScheduleDeathPresentationFallback();
    }

    public void NotifyDeathPresentationFinished()
    {
        HideAllVisualsLocal();

        if (!IsSpawned)
        {
            Destroy(gameObject, 0.05f);
            return;
        }

        if (IsServer)
            FinalizeDeathPresentation();
        else
            NotifyDeathPresentationFinishedServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotifyDeathPresentationFinishedServerRpc()
    {
        if (!IsServer || !_deathFinalized || _deathPresentationCompleted)
            return;

        FinalizeDeathPresentation();
    }

    private void ScheduleDeathPresentationFallback()
    {
        CancelDeathDespawnRoutine();
        _despawnCoroutine = StartCoroutine(DeathPresentationFallbackRoutine());
    }

    private float GetDeathPresentationFallbackDelay()
    {
        if (_dissolveEffect != null)
            return Mathf.Max(deathPresentationFallbackSeconds, _dissolveEffect.EstimatedTotalDuration + 1f);

        if (_dropHandler != null && _dropHandler.DeathDespawnDelay > 0f)
            return _dropHandler.DeathDespawnDelay;

        return Mathf.Max(deathDespawnDelay, 2f);
    }

    private IEnumerator DeathPresentationFallbackRoutine()
    {
        yield return new WaitForSeconds(GetDeathPresentationFallbackDelay());
        FinalizeDeathPresentation();
        _despawnCoroutine = null;
    }

    private void FinalizeDeathPresentation()
    {
        if (_deathPresentationCompleted)
            return;

        _deathPresentationCompleted = true;
        CancelDeathDespawnRoutine();
        HideAllVisualsLocal();
        DespawnEnemy();
    }

    private void HideAllVisualsLocal()
    {
        if (_dissolveEffect != null)
        {
            if (_dissolveEffect.IsPlaying)
                return;

            _dissolveEffect.HideVisuals();
        }
        else
        {
            DeathVisualHider.Hide(transform);
        }
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

        PlayDeathVisuals();
    }

    private void PlayDeathVisuals()
    {
        if (_deathVisualsPlayed)
            return;

        _deathVisualsPlayed = true;
        PrepareAnimatorForDeathPresentation();

        if (_animationHandler != null)
            _animationHandler.PlayDeathAnimation();
        else if (_animator != null)
            _animator.SetTrigger(HashOnDie);

        if (_dissolveEffect != null)
        {
            _dissolveEffect.HandleDeath();
            return;
        }
    }

    private void PrepareAnimatorForDeathPresentation()
    {
        if (_animator == null)
            return;

        _animator.enabled = true;
        _animator.speed = 1f;
        _animator.SetFloat(HashMoveSpeed, 0f);
        _animator.SetBool(HashIsAttacking, false);
    }

    private void ApplyClientMirror(float current, float max, bool isDead)
    {
        if (_health == null) return;
        _health.ApplyNetworkMirror(current, max, isDead);
    }

    public bool ServerApplyDamage(float amount, ulong instigatorClientId, DamageType damageType = DamageType.Generic)
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
        _lastInstigatorClientId = instigatorClientId;
        _health.TakeDamage(amount, gameObject, damageType);

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
    public void ApplySlowRpc(float speedMultiplier, float duration)
    {
        if (!IsServer || IsDeadOnNetwork || duration <= 0f) return;

        var slow = GetComponent<EnemySlowEffect>() ?? gameObject.AddComponent<EnemySlowEffect>();
        slow.ApplySlow(speedMultiplier, duration);
    }

    [Rpc(SendTo.Server)]
    public void ApplyStunRpc(float duration)
    {
        if (!IsServer || IsDeadOnNetwork || duration <= 0f) return;
        _hitStun?.ApplyStun(duration);
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
        PlayDeathVisuals();
    }

    public void BroadcastTelegraphToClients(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        Vector2 travelSpawnPosition)
    {
        if (!IsServer || !IsSpawned)
            return;

        TelegraphClientSnapshot snapshot = TelegraphClientSnapshot.From(
            strike, style, worldPosition, rotationDegrees, travelSpawnPosition);
        PlayTelegraphVisualClientRpc(snapshot);
    }

    [ClientRpc]
    private void PlayTelegraphVisualClientRpc(TelegraphClientSnapshot snapshot)
    {
        if (IsServer)
            return;

        TelegraphStrikeDefinition strike = snapshot.ToStrikeDefinition();
        EnemyTelegraphVisualStyle style = snapshot.ToVisualStyle();

        EnemyTelegraphZoneFactory.SpawnClientVisual(
            strike,
            style,
            snapshot.WorldPosition,
            snapshot.RotationDegrees,
            gameObject,
            snapshot.TravelSpawnPosition,
            snapshot.HasTravelVisual == 1,
            snapshot.TravelSpeed);
    }

    private void EnsureTelegraphClientComponents()
    {
        if (GetComponent<EnemyTelegraphZoneFactory>() == null)
            gameObject.AddComponent<EnemyTelegraphZoneFactory>();

        if (GetComponent<NetworkEnemyTelegraphRelay>() == null)
            gameObject.AddComponent<NetworkEnemyTelegraphRelay>();

        if (_telegraphedAttacker != null)
            _telegraphedAttacker.EnsureTelegraphWiring();
    }

    private void WireAnimationPublishers()
    {
        if (_movement != null)
            _movement.OnFlipSprite += HandleServerAnimFlip;

        if (_telegraphedAttacker != null)
            _telegraphedAttacker.OnAttackWindup += HandleServerAnimAttack;
        if (_meleAttack != null)
            _meleAttack.OnAttack += HandleServerAnimAttack;
        if (_rangedAttack != null)
            _rangedAttack.OnAttack += HandleServerAnimAttack;
    }

    private void UnwireAnimationPublishers()
    {
        if (_movement != null)
            _movement.OnFlipSprite -= HandleServerAnimFlip;

        if (_telegraphedAttacker != null)
            _telegraphedAttacker.OnAttackWindup -= HandleServerAnimAttack;
        if (_meleAttack != null)
            _meleAttack.OnAttack -= HandleServerAnimAttack;
        if (_rangedAttack != null)
            _rangedAttack.OnAttack -= HandleServerAnimAttack;
    }

    private void HandleServerAnimFlip(bool facingRight) => _animFacingFlipX.Value = facingRight;

    private void HandleServerAnimAttack() => _animAttackSequence.Value++;

    private void HandleAnimMoveSpeedChanged(float _, float current) =>
        ApplyClientAnimationState(current, _animFacingFlipX.Value, _animAttackSequence.Value, _animIsAttacking.Value, false);

    private void HandleAnimFacingChanged(bool _, bool current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, current, _animAttackSequence.Value, _animIsAttacking.Value, false);

    private void HandleAnimAttackSequenceChanged(byte _, byte current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, _animFacingFlipX.Value, current, _animIsAttacking.Value, true);

    private void HandleAnimIsAttackingChanged(bool _, bool current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, _animFacingFlipX.Value, _animAttackSequence.Value, current, false);

    private void ApplyClientAnimationState(
        float moveSpeed,
        bool facingRight,
        byte attackSequence,
        bool isAttacking,
        bool triggerAttack)
    {
        if (!IsSpawned || IsServer)
            return;

        if (_deathVisualsPlayed)
            return;

        if (_animator != null)
        {
            _animator.SetFloat(HashMoveSpeed, moveSpeed);
            _animator.SetBool(HashIsAttacking, isAttacking);
        }

        if (_spriteRenderer != null)
            _spriteRenderer.flipX = !facingRight;

        if (triggerAttack && attackSequence != _lastClientAttackSequence)
        {
            _lastClientAttackSequence = attackSequence;
            _animator?.SetTrigger(HashOnAttack);
        }
    }

    private IEnumerator SyncHealthAfterConfigsRoutine()
    {
        yield return null;
        if (_health == null) yield break;
        PublishHealthSnapshot();
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(float amount, ulong instigatorClientId, DamageType damageType = DamageType.Generic)
    {
        ServerApplyDamage(amount, instigatorClientId, damageType);
    }

    private void SetAIComponentsActive(bool active)
    {
        if (_movement != null) _movement.enabled = active;
        if (_targetFinder != null) _targetFinder.enabled = active;

        bool useTelegraph = _telegraphedAttacker != null && _telegraphedAttacker.HasActivePattern;
        if (_meleAttack != null) _meleAttack.enabled = active && !useTelegraph;
        if (_rangedAttack != null) _rangedAttack.enabled = active && !useTelegraph;
        if (_telegraphedAttacker != null) _telegraphedAttacker.enabled = active && useTelegraph;

        if (_dropHandler != null) _dropHandler.enabled = active;
        if (_hitStun != null) _hitStun.enabled = active;
        if (_agent != null && active) _agent.enabled = true;
    }

    private void WireTelegraphedAttackerProjectileSpawn()
    {
        if (_telegraphedAttacker == null) return;
        _telegraphedAttacker.ProjectileSpawnDelegate = SpawnEnemyProjectileNetworked;
    }

    private GameObject SpawnEnemyProjectileNetworked(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        var instance = Instantiate(prefab, position, rotation);
        var networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned)
            networkObject.Spawn();

        return instance;
    }
}
