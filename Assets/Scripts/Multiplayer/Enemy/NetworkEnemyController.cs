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
    private Rigidbody2D _rigidbody;
    private EnemyPhysicsBody _physicsBody;
    private bool _agentHadUpdatePosition = true;

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

    private readonly NetworkVariable<bool> _networkIsCombatStunned = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> _animSpellSequence = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> _animChargeSequence = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _animIsCharging = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int HashOnAttack = Animator.StringToHash("OnAttack");
    private static readonly int HashOnTakeDamage = Animator.StringToHash("OnTakeDamage");
    private static readonly int HashOnDie = Animator.StringToHash("OnDie");
    private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int HashIsStunned = Animator.StringToHash("IsStunned");
    private static readonly int HashOnSpell = Animator.StringToHash("OnSpell");
    private static readonly int HashOnCharge = Animator.StringToHash("OnCharge");
    private static readonly int HashIsCharging = Animator.StringToHash("IsCharging");

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private byte _lastClientAttackSequence;
    private byte _lastClientSpellSequence;
    private byte _lastClientChargeSequence;
    private RatKingController _ratKing;

    public bool DrivesAnimatorOnClient => IsSpawned && !IsServer;

    public bool HasDeathVisualsPlayed => _deathVisualsPlayed;

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
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _ratKing = GetComponent<RatKingController>();

        _health.SetAllowDestroyOnDeath(false);
        _health.OnDied.AddListener(HandleDied);

        if (GetComponent<EnemyHealthBarDisplay>() == null)
            gameObject.AddComponent<EnemyHealthBarDisplay>();

        if (GetComponent<EnemySlowEffect>() == null)
            gameObject.AddComponent<EnemySlowEffect>();

        _physicsBody = GetComponent<EnemyPhysicsBody>();
        if (_physicsBody == null)
            _physicsBody = gameObject.AddComponent<EnemyPhysicsBody>();

        if (GetComponent<EnemySpawnPresentation>() == null)
            gameObject.AddComponent<EnemySpawnPresentation>();

        if (GetComponent<EnemySwordHitFlash>() == null)
            gameObject.AddComponent<EnemySwordHitFlash>();
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
        _animSpellSequence.OnValueChanged += HandleAnimSpellSequenceChanged;
        _animChargeSequence.OnValueChanged += HandleAnimChargeSequenceChanged;
        _animIsCharging.OnValueChanged += HandleAnimIsChargingChanged;
        _networkIsCombatStunned.OnValueChanged += HandleCombatStunChanged;

        if (IsServer)
        {
            SetAIComponentsActive(true);
            WireTelegraphedAttackerProjectileSpawn();
            WireAnimationPublishers();
            _ratKing?.ServerEnsureBrain();

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
            ApplyBossAnimatorFlags(_animIsCharging.Value, false, false);
        }

        _networkHealth.OnValueChanged += HandleNetworkHealthChanged;
        _networkMaxHealth.OnValueChanged += HandleNetworkMaxHealthChanged;
        _networkIsDead.OnValueChanged += HandleNetworkDeathChanged;

        if (!IsServer)
            ApplyClientMirror(_networkHealth.Value, _networkMaxHealth.Value, _networkIsDead.Value);

        HandleCombatStunChanged(false, _networkIsCombatStunned.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _health != null)
            _health.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);

        _networkHealth.OnValueChanged -= HandleNetworkHealthChanged;
        _networkMaxHealth.OnValueChanged -= HandleNetworkMaxHealthChanged;
        _networkIsDead.OnValueChanged -= HandleNetworkDeathChanged;
        _animMoveSpeed.OnValueChanged -= HandleAnimMoveSpeedChanged;
        _animFacingFlipX.OnValueChanged -= HandleAnimFacingChanged;
        _animAttackSequence.OnValueChanged -= HandleAnimAttackSequenceChanged;
        _animIsAttacking.OnValueChanged -= HandleAnimIsAttackingChanged;
        _animSpellSequence.OnValueChanged -= HandleAnimSpellSequenceChanged;
        _animChargeSequence.OnValueChanged -= HandleAnimChargeSequenceChanged;
        _animIsCharging.OnValueChanged -= HandleAnimIsChargingChanged;
        _networkIsCombatStunned.OnValueChanged -= HandleCombatStunChanged;

        if (TryGetComponent<EnemyHealthBarDisplay>(out var healthBarDisplay))
            healthBarDisplay.HideImmediately();

        UnwireAnimationPublishers();
        CancelDeathDespawnRoutine();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || _movement == null)
            return;

        if (_deathFinalized || IsDeadOnNetwork)
            return;

        bool stunned = _hitStun != null && _hitStun.IsStunned;
        if (_networkIsCombatStunned.Value != stunned)
            _networkIsCombatStunned.Value = stunned;

        float speed = _movement.GetCurrentSpeed();
        if (!Mathf.Approximately(_animMoveSpeed.Value, speed))
            _animMoveSpeed.Value = speed;

        if (_animationHandler != null)
        {
            bool isAttacking = _animationHandler.IsAttackingForAnimator
                              || (_ratKing != null && _ratKing.IsAttackBusy);
            if (_animIsAttacking.Value != isAttacking)
                _animIsAttacking.Value = isAttacking;
        }
        else if (_ratKing != null)
        {
            if (_animIsAttacking.Value != _ratKing.IsAttackBusy)
                _animIsAttacking.Value = _ratKing.IsAttackBusy;
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

        // Host/dedicated: dispara já no servidor. Clientes remotos recebem no ClientRpc.
        GameEvents.InvokeEnemyKilledByPlayer(_lastInstigatorClientId);

        _animMoveSpeed.Value = 0f;
        _animIsAttacking.Value = false;

        PlayDeathVisualClientRpc(_lastInstigatorClientId);
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

    private void HandleNetworkMaxHealthChanged(float oldValue, float newValue)
    {
        if (IsServer) return;
        ApplyClientMirror(_networkHealth.Value, newValue, _networkIsDead.Value);
    }

    private void HandleNetworkDeathChanged(bool wasAlive, bool isDead)
    {
        if (!isDead || IsServer) return;

        ApplyClientMirror(0f, _networkMaxHealth.Value, true);
        SetAIComponentsActive(false);
        ApplyDeathPresentation();

        if (TryGetComponent<EnemyHealthBarDisplay>(out var healthBarDisplay))
            healthBarDisplay.HideImmediately();

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
        _animator.ResetTrigger(HashOnTakeDamage);
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
            float dealtForVisual = Mathf.Max(0f, before - _health.CurrentHealth);
            PlayTakeDamageVisualClientRpc(dealtForVisual, damageType);

            NotifyTargetFinderDamagedByPlayer(instigatorClientId);
        }

        EmitDamageDiagnostic(amount, before, _health.CurrentHealth, _health.IsDead,
            _health.IsDead ? "OK lethal" : $"OK instigator={instigatorClientId}");

        float dealt = Mathf.Max(0f, before - _health.CurrentHealth);
        if (dealt > 0f)
            ShowDamageNumberClientRpc(dealt);

        return true;
    }

    private void NotifyTargetFinderDamagedByPlayer(ulong instigatorClientId)
    {
        if (_targetFinder == null || !IsServer)
            return;

        Transform attacker = ResolvePlayerTransformByClientId(instigatorClientId);
        if (attacker != null)
            _targetFinder.NotifyDamagedBy(attacker);
    }

    private static Transform ResolvePlayerTransformByClientId(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return null;

        if (networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
            && client.PlayerObject != null)
            return client.PlayerObject.transform;

        return null;
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
        ServerApplyCombatStun(duration);
    }

    [Rpc(SendTo.Server)]
    public void ApplyKnockbackRpc(Vector2 direction, float distance, float duration)
    {
        if (!IsServer || IsDeadOnNetwork || distance <= 0f || duration <= 0f) return;

        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(ServerKnockbackRoutine(direction, distance, duration, 0f));
    }

    /// <summary>
    /// Knockback autoritativo; ao terminar, aplica stun de combate (passiva Nix).
    /// </summary>
    [Rpc(SendTo.Server)]
    public void ApplyKnockbackThenStunRpc(Vector2 direction, float distance, float knockbackDuration, float stunDuration)
    {
        if (!IsServer || IsDeadOnNetwork || distance <= 0f || knockbackDuration <= 0f) return;

        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(
            ServerKnockbackRoutine(direction, distance, knockbackDuration, stunDuration));
    }

    private void ServerApplyCombatStun(float duration)
    {
        if (duration <= 0f) return;

        _hitStun?.ApplyStun(duration);
        _telegraphedAttacker?.FreezeForPause();
        _networkIsCombatStunned.Value = true;
    }

    /// <summary>
    /// Knockback autoritativo via Rigidbody2D (Impulse). Não usa transform.position —
    /// teleporte ignora Continuous Collision e causa tunneling nas paredes.
    /// </summary>
    private IEnumerator ServerKnockbackRoutine(Vector2 direction, float distance, float duration, float stunAfter)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

        LockLocomotionForKnockback();

        // Velocidade média equivalente à antiga distância/duração do Lerp.
        float speed = distance / Mathf.Max(0.01f, duration);

        if (_physicsBody != null)
            _physicsBody.BeginExternalPhysics();

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            // Impulse: Δv = impulse / mass → impulso = speed * mass.
            _rigidbody.AddForce(direction * speed * _rigidbody.mass, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(duration);

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector2.zero;

        if (_physicsBody != null)
            _physicsBody.EndExternalPhysics();

        if (stunAfter > 0f && !IsDeadOnNetwork)
            ServerApplyCombatStun(stunAfter);

        if (!IsDeadOnNetwork)
            UnlockLocomotionAfterKnockback();

        _knockbackCoroutine = null;
    }

    private void LockLocomotionForKnockback()
    {
        if (_movement != null)
            _movement.enabled = false;

        if (_agent == null)
            return;

        _agentHadUpdatePosition = _agent.updatePosition;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        _agent.ResetPath();
        // Impede o Agent de sobrescrever a posição enquanto o Rigidbody empurra.
        _agent.updatePosition = false;
    }

    private void UnlockLocomotionAfterKnockback()
    {
        if (_agent != null)
        {
            if (_agent.enabled && _agent.isOnNavMesh)
                _agent.Warp(transform.position);

            _agent.updatePosition = _agentHadUpdatePosition;
            _agent.isStopped = false;
        }

        if (_movement != null)
            _movement.enabled = true;
    }

    private void HandleCombatStunChanged(bool previous, bool current)
    {
        if (_animator == null)
            return;

        // Parâmetro opcional no Animator — se não existir, Unity ignora sem erro crítico.
        foreach (var parameter in _animator.parameters)
        {
            if (parameter.nameHash == HashIsStunned && parameter.type == AnimatorControllerParameterType.Bool)
            {
                _animator.SetBool(HashIsStunned, current);
                break;
            }
        }
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
    private void PlayTakeDamageVisualClientRpc(float dealtDamage, DamageType damageType)
    {
        if (_deathVisualsPlayed || _health == null || !_health.IsAlive || _health.CurrentHealth <= 0f)
            return;

        if (_animationHandler != null)
            _animationHandler.PlayTakeDamageAnimation();

        if (damageType == DamageType.Melee && TryGetComponent<EnemySwordHitFlash>(out var swordFlash))
            swordFlash.PlayFlash();
        else if (TryGetComponent<SpriteBlink>(out var blink)
                 && BossPhaseUtility.ShouldPlayBossBlink(gameObject, dealtDamage))
            blink.Blink();

        if (TryGetComponent<EnemyAudioController>(out var audio))
            audio.PlayDamageSfx();
    }

    [ClientRpc]
    private void PlayDeathVisualClientRpc(ulong killerClientId)
    {
        PlayDeathVisuals();

        if (TryGetComponent<EnemyAudioController>(out var audio))
            audio.PlayDeathSfx();

        // Host já recebeu o evento no FinalizeDeathOnServer; só clientes remotos.
        if (IsServer)
            return;

        GameEvents.InvokeEnemyKilledByPlayer(killerClientId);
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

    /// <summary>Dispara trigger OnSpell no Animator (servidor + clientes via NV).</summary>
    public void ServerNotifySpellCast()
    {
        if (!IsServer)
            return;

        if (_animator != null && HasAnimatorTrigger(HashOnSpell))
            _animator.SetTrigger(HashOnSpell);

        if (IsSpawned)
            _animSpellSequence.Value++;
    }

    /// <summary>Início da investida: OnCharge + IsCharging.</summary>
    public void ServerNotifyChargeStart()
    {
        if (!IsServer)
            return;

        if (_animator != null)
        {
            if (HasAnimatorTrigger(HashOnCharge))
                _animator.SetTrigger(HashOnCharge);
            if (HasAnimatorBool(HashIsCharging))
                _animator.SetBool(HashIsCharging, true);
        }

        if (IsSpawned)
        {
            _animIsCharging.Value = true;
            _animChargeSequence.Value++;
        }
    }

    public void ServerNotifyChargeEnd()
    {
        if (!IsServer)
            return;

        if (_animator != null && HasAnimatorBool(HashIsCharging))
            _animator.SetBool(HashIsCharging, false);

        if (IsSpawned)
            _animIsCharging.Value = false;
    }

    /// <summary>Follow-up melee após o dash (usa OnAttack).</summary>
    public void ServerNotifyMeleeAttack()
    {
        if (!IsServer)
            return;

        if (_animator != null)
            _animator.SetTrigger(HashOnAttack);

        if (IsSpawned)
            _animAttackSequence.Value++;
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

    private void HandleServerAnimAttack()
    {
        _animAttackSequence.Value++;

        if (TryGetComponent<EnemyAudioController>(out var audio))
            audio.PlayAttackSfx();
    }

    private void HandleAnimMoveSpeedChanged(float _, float current) =>
        ApplyClientAnimationState(current, _animFacingFlipX.Value, _animAttackSequence.Value, _animIsAttacking.Value, false);

    private void HandleAnimFacingChanged(bool _, bool current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, current, _animAttackSequence.Value, _animIsAttacking.Value, false);

    private void HandleAnimAttackSequenceChanged(byte _, byte current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, _animFacingFlipX.Value, current, _animIsAttacking.Value, true);

    private void HandleAnimIsAttackingChanged(bool _, bool current) =>
        ApplyClientAnimationState(_animMoveSpeed.Value, _animFacingFlipX.Value, _animAttackSequence.Value, current, false);

    private void HandleAnimSpellSequenceChanged(byte _, byte current)
    {
        if (!IsSpawned || IsServer || _deathVisualsPlayed)
            return;

        if (current == _lastClientSpellSequence)
            return;

        _lastClientSpellSequence = current;
        if (_animator != null && HasAnimatorTrigger(HashOnSpell))
            _animator.SetTrigger(HashOnSpell);
    }

    private void HandleAnimChargeSequenceChanged(byte _, byte current)
    {
        if (!IsSpawned || IsServer || _deathVisualsPlayed)
            return;

        if (current == _lastClientChargeSequence)
            return;

        _lastClientChargeSequence = current;
        if (_animator != null && HasAnimatorTrigger(HashOnCharge))
            _animator.SetTrigger(HashOnCharge);
    }

    private void HandleAnimIsChargingChanged(bool _, bool current)
    {
        if (!IsSpawned || IsServer || _deathVisualsPlayed)
            return;

        ApplyBossAnimatorFlags(current, false, false);
    }

    private void ApplyBossAnimatorFlags(bool isCharging, bool triggerSpell, bool triggerCharge)
    {
        if (_animator == null)
            return;

        if (HasAnimatorBool(HashIsCharging))
            _animator.SetBool(HashIsCharging, isCharging);

        if (triggerSpell && HasAnimatorTrigger(HashOnSpell))
            _animator.SetTrigger(HashOnSpell);

        if (triggerCharge && HasAnimatorTrigger(HashOnCharge))
            _animator.SetTrigger(HashOnCharge);
    }

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
            if (HasAnimatorBool(HashIsCharging))
                _animator.SetBool(HashIsCharging, _animIsCharging.Value);
        }

        if (_spriteRenderer != null)
            _spriteRenderer.flipX = !facingRight;

        if (triggerAttack && attackSequence != _lastClientAttackSequence)
        {
            _lastClientAttackSequence = attackSequence;
            _animator?.SetTrigger(HashOnAttack);

            if (TryGetComponent<EnemyAudioController>(out var audio))
                audio.PlayAttackSfx();
        }
    }

    private static bool HasAnimatorTrigger(Animator animator, int hash)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger && parameters[i].nameHash == hash)
                return true;
        }

        return false;
    }

    private bool HasAnimatorTrigger(int hash) => HasAnimatorTrigger(_animator, hash);

    private bool HasAnimatorBool(int hash)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Bool && parameters[i].nameHash == hash)
                return true;
        }

        return false;
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

        bool hasBossBrain = _ratKing != null;
        bool useTelegraph = !hasBossBrain
                            && _telegraphedAttacker != null
                            && _telegraphedAttacker.HasActivePattern;
        if (_meleAttack != null) _meleAttack.enabled = active && !useTelegraph && !hasBossBrain;
        if (_rangedAttack != null) _rangedAttack.enabled = active && !useTelegraph && !hasBossBrain;
        if (_telegraphedAttacker != null) _telegraphedAttacker.enabled = active && useTelegraph;

        if (_dropHandler != null) _dropHandler.enabled = active;
        if (_hitStun != null) _hitStun.enabled = active;
        if (_ratKing != null) _ratKing.enabled = active;
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
