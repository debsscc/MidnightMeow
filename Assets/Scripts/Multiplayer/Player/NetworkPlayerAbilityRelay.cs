using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Replica animações de habilidade, movimento, ataque e aplica dano autoritativo da Investida (Nix).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerAbilityRelay : NetworkBehaviour
{
    private readonly NetworkVariable<float> _moveSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<byte> _attackSequence = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> _networkIsDashing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    [SerializeField] private LayerMask chargeEnemyLayers;

    private Rigidbody2D _rb;
    private PlayerShooting _shooting;
    private PlayerMeleeCombat _melee;
    private PlayerDash _dash;
    private PlayerAnimationHandler _animationHandler;
    private PlayerAbilityHandler _abilityHandler;
    private AbilityDebugVisualHost _debugHost;
    private byte _lastRemoteAttackSequence;
    private readonly HashSet<ulong> _chargeDamagedEnemyIds = new HashSet<ulong>();

    public bool NetworkIsDashing => _networkIsDashing.Value;

    public void ApplyEnemyLayers(LayerMask layers)
    {
        if (layers.value != 0)
            chargeEnemyLayers = layers;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _shooting = GetComponent<PlayerShooting>();
        _melee = GetComponent<PlayerMeleeCombat>();
        _dash = GetComponent<PlayerDash>();
        _animationHandler = GetComponent<PlayerAnimationHandler>();
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        _debugHost = GetComponent<AbilityDebugVisualHost>();

        if (chargeEnemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                chargeEnemyLayers = 1 << enemyLayer;
        }
    }

    public override void OnNetworkSpawn()
    {
        _moveSpeed.OnValueChanged += HandleMoveSpeedChanged;
        _attackSequence.OnValueChanged += HandleAttackSequenceChanged;

        if (IsOwner)
        {
            if (_shooting != null)
                _shooting.OnProjectileInstantiated += HandleOwnerProjectileFired;
            if (_melee != null)
                _melee.OnMeleeAttackStarted += HandleOwnerMeleeAttackStarted;
        }
        else if (_animationHandler != null)
        {
            _animationHandler.SetUseNetworkMoveSpeed(true);
            _animationHandler.ApplyNetworkMoveSpeed(_moveSpeed.Value);
            TryPlayRemoteAttack(_attackSequence.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (_shooting != null)
                _shooting.OnProjectileInstantiated -= HandleOwnerProjectileFired;
            if (_melee != null)
                _melee.OnMeleeAttackStarted -= HandleOwnerMeleeAttackStarted;
        }

        _moveSpeed.OnValueChanged -= HandleMoveSpeedChanged;
        _attackSequence.OnValueChanged -= HandleAttackSequenceChanged;

        if (_animationHandler != null)
            _animationHandler.SetUseNetworkMoveSpeed(false);
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned || _rb == null)
            return;

        float speed = _rb.linearVelocity.magnitude;
        if (!Mathf.Approximately(_moveSpeed.Value, speed))
            _moveSpeed.Value = speed;

        if (_dash != null && _networkIsDashing.Value != _dash.IsDashing)
            _networkIsDashing.Value = _dash.IsDashing;
    }

    public void ReportAbilityActivated(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        if (!IsSpawned || !IsOwner) return;
        ReportAbilityActivatedServerRpc(abilityType, position, direction);
    }

    public void ReportDashStarted()
    {
        if (!IsSpawned || !IsOwner) return;
        ReportAbilityActivatedServerRpc(CharacterAbilityType.Dash, transform.position, Vector2.zero);
    }

    public void ResetChargeSession()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (IsServer)
            _chargeDamagedEnemyIds.Clear();
        else
            ResetChargeSessionServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void ResetChargeSessionServerRpc() => _chargeDamagedEnemyIds.Clear();

    public void ReportChargeDamageFrame(
        Vector2 origin,
        Vector2 direction,
        float depth,
        float halfWidth,
        float damage,
        ulong instigatorClientId)
    {
        if (!IsSpawned || !IsOwner || damage <= 0f || depth <= 0f)
            return;

        if (IsServer)
            ServerApplyChargeDamage(origin, direction, depth, halfWidth, damage, instigatorClientId);
        else
            ReportChargeDamageServerRpc(origin, direction, depth, halfWidth, damage);
    }

    [Rpc(SendTo.Server)]
    private void ReportChargeDamageServerRpc(
        Vector2 origin,
        Vector2 direction,
        float depth,
        float halfWidth,
        float damage)
    {
        ServerApplyChargeDamage(origin, direction, depth, halfWidth, damage, OwnerClientId);
    }

    private void ServerApplyChargeDamage(
        Vector2 origin,
        Vector2 direction,
        float depth,
        float halfWidth,
        float damage,
        ulong instigatorClientId)
    {
        if (!IsServer || damage <= 0f || depth <= 0f)
            return;

        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        float searchRadius = depth + halfWidth + 0.5f;
        var hits = Physics2D.OverlapCircleAll(origin + direction * (depth * 0.5f), searchRadius, chargeEnemyLayers);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var networkEnemy = hit.GetComponentInParent<NetworkEnemyController>();
            ulong enemyKey = networkEnemy != null && networkEnemy.NetworkObject != null
                ? networkEnemy.NetworkObject.NetworkObjectId
                : (ulong)hit.GetInstanceID();

            var damageable = hit.GetComponentInParent<HealthComponent>();
            if (damageable == null || !damageable.IsAlive)
                continue;

            Vector2 targetPoint = hit.bounds.center;
            if (!RectHitUtility.IsInsideOrientedRect(origin, direction, depth, halfWidth, targetPoint))
                continue;

            if (!_chargeDamagedEnemyIds.Add(enemyKey))
                continue;

            EnemyCombatUtility.ApplyDamage(damageable.gameObject, damage, instigatorClientId, gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    private void ReportAbilityActivatedServerRpc(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        PlayAbilityVisualClientRpc(abilityType, position, direction);
    }

    [ClientRpc]
    private void PlayAbilityVisualClientRpc(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        if (IsOwner)
            return;

        ApplyRemoteAbilityPresentation(abilityType, position, direction);
    }

    private void ApplyRemoteAbilityPresentation(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        if (_animationHandler != null)
            _animationHandler.PlayAbilityAnimation(abilityType);

        if (_debugHost == null || _abilityHandler == null)
            return;

        AbilityTierData tierData = ResolveTierDataForAbility(abilityType);
        if (tierData.range <= 0f && tierData.damage <= 0f && tierData.areaWidth <= 0f)
            return;

        Vector2 origin = transform.position;
        _debugHost.ShowAbility(abilityType, origin, direction, position, tierData);
    }

    private AbilityTierData ResolveTierDataForAbility(CharacterAbilityType abilityType)
    {
        if (_abilityHandler?.AbilitySet == null)
            return default;

        CharacterAbilityDefinition definition = abilityType switch
        {
            CharacterAbilityType.CoraBarrier => _abilityHandler.AbilitySet.ability1,
            CharacterAbilityType.CoraPool => _abilityHandler.AbilitySet.ability2,
            CharacterAbilityType.NixPush => _abilityHandler.AbilitySet.ability1,
            CharacterAbilityType.NixCharge => _abilityHandler.AbilitySet.ability2,
            _ => null
        };

        if (definition == null)
            return default;

        int tier = abilityType switch
        {
            CharacterAbilityType.CoraBarrier or CharacterAbilityType.NixPush => _abilityHandler.Progression.ability1Tier,
            CharacterAbilityType.CoraPool or CharacterAbilityType.NixCharge => _abilityHandler.Progression.ability2Tier,
            _ => 1
        };

        return definition.GetTierData(tier);
    }

    private void HandleOwnerProjectileFired(
        GameObject _,
        Vector3 __,
        Quaternion ___,
        Vector2 ____)
    {
        if (!IsSpawned || !IsOwner)
            return;

        _attackSequence.Value++;
    }

    private void HandleOwnerMeleeAttackStarted()
    {
        if (_dash != null && _dash.IsDashing)
        {
            if (IsServer)
                PlayDashAttackVisualClientRpc();
            else
                ReportDashAttackVisualServerRpc();

            return;
        }

        _attackSequence.Value++;
    }

    [Rpc(SendTo.Server)]
    private void ReportDashAttackVisualServerRpc() => PlayDashAttackVisualClientRpc();

    [ClientRpc]
    private void PlayDashAttackVisualClientRpc()
    {
        if (IsOwner || _animationHandler == null)
            return;

        _animationHandler.PlayRemoteDashAttackAnimation();
    }

    private void HandleMoveSpeedChanged(float _, float current)
    {
        if (IsOwner || _animationHandler == null)
            return;

        _animationHandler.ApplyNetworkMoveSpeed(current);
    }

    private void HandleAttackSequenceChanged(byte _, byte current) => TryPlayRemoteAttack(current);

    private void TryPlayRemoteAttack(byte sequence)
    {
        if (IsOwner || sequence == _lastRemoteAttackSequence || _animationHandler == null)
            return;

        _lastRemoteAttackSequence = sequence;
        _animationHandler.PlayRemoteAttackAnimation();
    }
}
