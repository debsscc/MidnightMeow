///* ----------------------------------------------------------------
// DESCRIÇÃO: Orquestra habilidades do jogador — bloqueio mútuo, cooldowns, inputs Q/R/Dash.
// ---------------------------------------------------------------- */

using System;
using System.Collections.Generic;
// ReSharper disable UnusedMember.Global
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAbilityHandler : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterAbilitySet abilitySet;

    [Header("Spawn Prefabs (Cora)")]
    [SerializeField] private GameObject barrierPrefab;
    [SerializeField] private GameObject poolPrefab;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerDash dash;

    [Header("Sandbox")]
    [Tooltip("Desbloqueia Q/R desde o início (apenas sandbox/debug).")]
    [SerializeField] private bool unlockAllAbilitySlotsOnStart;

    private PlayerInputHandler _input;
    private NetworkObject _networkObject;
    private NetworkPlayerAbilityRelay _abilityRelay;
    private PlayerPassiveHandler _passiveHandler;
    private AbilityDebugVisualHost _debugHost;
    private readonly Dictionary<CharacterAbilityType, IAbilityExecutor> _executors = new();
    private readonly Dictionary<AbilitySlot, float> _cooldownTimers = new();
    private readonly Dictionary<AbilitySlot, float> _cooldownTotals = new();
    [SerializeField] private AbilityProgressionState _progression = new();

    private AbilitySlot? _activeSlot;
    private float _actionLockEndTime;

    public Transform FirePoint => firePoint;
    public CharacterAbilitySet AbilitySet => abilitySet;
    public AbilityProgressionState Progression => _progression;
    public bool IsActionLocked =>
        _activeSlot.HasValue ||
        Time.time < _actionLockEndTime ||
        (dash != null && dash.IsDashing) ||
        (TryGetComponent<PlayerMeleeCombat>(out var melee) && melee.IsAttacking);

    public event Action<CharacterAbilityType> OnAbilityActivated;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _networkObject = GetComponent<NetworkObject>();
        _abilityRelay = GetComponent<NetworkPlayerAbilityRelay>();
        _passiveHandler = GetComponent<PlayerPassiveHandler>();
        _debugHost = GetComponent<AbilityDebugVisualHost>();
        if (aim == null) aim = GetComponent<PlayerAim>();
        if (dash == null) dash = GetComponent<PlayerDash>();

        ApplySandboxUnlock();
        CacheExecutors();
    }

    private void OnEnable()
    {
        _input.OnAbility1Input += HandleAbility1Input;
        _input.OnAbility2Input += HandleAbility2Input;
        _input.OnDashInput += HandleDashInput;
        GameEvents.OnWaveStatusChanged += HandleWaveStatusChanged;

        if (dash != null)
            dash.OnDashEnded += HandleDashEnded;

        if (_passiveHandler != null && abilitySet != null)
            _passiveHandler.Configure(abilitySet);
    }

    private void OnDisable()
    {
        _input.OnAbility1Input -= HandleAbility1Input;
        _input.OnAbility2Input -= HandleAbility2Input;
        _input.OnDashInput -= HandleDashInput;
        GameEvents.OnWaveStatusChanged -= HandleWaveStatusChanged;

        if (dash != null)
            dash.OnDashEnded -= HandleDashEnded;
    }

    private void Update()
    {
        TickCooldowns();
        ReleaseFinishedLock();
    }

    private void ApplySandboxUnlock()
    {
        if (!unlockAllAbilitySlotsOnStart) return;

        _progression.phaseIndex = 3;
        _progression.ability1Unlocked = true;
        _progression.ability2Unlocked = true;
    }

    private void CacheExecutors()
    {
        _executors.Clear();
        var validTypes = CollectAbilityTypesFromSet();
        foreach (var executor in GetComponents<IAbilityExecutor>())
        {
            if (validTypes.Count == 0 || validTypes.Contains(executor.AbilityType))
                _executors[executor.AbilityType] = executor;
        }
    }

    private HashSet<CharacterAbilityType> CollectAbilityTypesFromSet()
    {
        var types = new HashSet<CharacterAbilityType>();
        if (abilitySet?.ability1 != null)
            types.Add(abilitySet.ability1.abilityType);
        if (abilitySet?.ability2 != null)
            types.Add(abilitySet.ability2.abilityType);
        return types;
    }

    private bool IsAbilityInCurrentSet(CharacterAbilityType abilityType)
    {
        if (abilitySet == null) return false;
        return (abilitySet.ability1 != null && abilitySet.ability1.abilityType == abilityType)
               || (abilitySet.ability2 != null && abilitySet.ability2.abilityType == abilityType);
    }

    public bool CanExecute(AbilitySlot slot)
    {
        if (IsActionLocked) return false;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return false;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return false;

        if (!_progression.IsSlotUnlocked(slot))
            return false;

        if (GetCooldownRemaining(slot) > 0f)
            return false;

        return true;
    }

    public bool TryRequestPrimaryAttack()
    {
        return CanExecute(AbilitySlot.PrimaryAttack);
    }

    private void HandleAbility1Input() => TryActivateSlot(AbilitySlot.Ability1);
    private void HandleAbility2Input() => TryActivateSlot(AbilitySlot.Ability2);

    private void HandleDashInput()
    {
        if (!CanExecute(AbilitySlot.Dash) || dash == null) return;

        if (dash.TryStartDash())
        {
            BeginActionLock(AbilitySlot.Dash, dash.GetDashLockDuration());
            OnAbilityActivated?.Invoke(CharacterAbilityType.Dash);
            _abilityRelay?.ReportDashStarted();
        }
    }

    private void HandleDashEnded()
    {
        if (_activeSlot == AbilitySlot.Dash)
            ClearActionLock();
    }

    private void TryActivateSlot(AbilitySlot slot)
    {
        if (!CanExecute(slot)) return;

        CharacterAbilityDefinition definition = GetDefinitionForSlot(slot);
        if (definition == null || !IsAbilityInCurrentSet(definition.abilityType)) return;

        int tier = _progression.GetTierForSlot(slot);
        AbilityTierData tierData = definition.GetTierData(tier);

        if (!_executors.TryGetValue(definition.abilityType, out var executor))
            return;

        Vector2 aimDirection = ResolveAimDirection();
        Vector2 placement = ResolvePlacement(tierData.range, aimDirection);
        Vector2 origin = definition.abilityType == CharacterAbilityType.NixCharge
            ? (Vector2)transform.position
            : ResolveAbilityOrigin();

        ulong ownerId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        var context = new AbilityExecutionContext(gameObject, aimDirection, placement, tier, ownerId);

        float lockDuration = Mathf.Max(definition.executionLockDuration, executor.Execute(tierData, context));
        BeginActionLock(slot, lockDuration);

        float cooldown = tierData.cooldown > 0f ? tierData.cooldown : definition.GetTierData(1).cooldown;
        if (cooldown > 0f)
        {
            _cooldownTimers[slot] = cooldown;
            _cooldownTotals[slot] = cooldown;
        }

        _debugHost?.ShowAbility(definition.abilityType, origin, aimDirection, placement, tierData);
        OnAbilityActivated?.Invoke(definition.abilityType);
        _abilityRelay?.ReportAbilityActivated(definition.abilityType, placement, aimDirection);
    }

    private Vector2 ResolveAbilityOrigin()
    {
        if (firePoint != null)
            return firePoint.position;
        return transform.position;
    }

    private void BeginActionLock(AbilitySlot slot, float duration)
    {
        _activeSlot = slot;
        if (duration > 0f)
            _actionLockEndTime = Time.time + duration;
    }

    private void ClearActionLock()
    {
        _activeSlot = null;
        _actionLockEndTime = 0f;
    }

    private void ReleaseFinishedLock()
    {
        if (!_activeSlot.HasValue) return;
        if (Time.time < _actionLockEndTime) return;

        if (_activeSlot == AbilitySlot.Dash && dash != null && dash.IsDashing)
            return;

        ClearActionLock();
    }

    private void TickCooldowns()
    {
        if (_cooldownTimers.Count == 0) return;

        var keys = new List<AbilitySlot>(_cooldownTimers.Keys);
        foreach (var key in keys)
        {
            _cooldownTimers[key] -= Time.deltaTime;
            if (_cooldownTimers[key] <= 0f)
                _cooldownTimers.Remove(key);
        }
    }

    public float GetCooldownRemaining(AbilitySlot slot)
    {
        return _cooldownTimers.TryGetValue(slot, out float remaining) ? remaining : 0f;
    }

    public float GetCooldownTotal(AbilitySlot slot)
    {
        if (_cooldownTotals.TryGetValue(slot, out float total) && total > 0f)
            return total;

        return GetConfiguredCooldown(slot);
    }

    public bool IsSlotUnlocked(AbilitySlot slot) => _progression.IsSlotUnlocked(slot);

    public string GetSlotDisplayName(AbilitySlot slot)
    {
        CharacterAbilityDefinition definition = GetDefinitionForSlot(slot);
        return definition != null ? definition.displayName : string.Empty;
    }

    private float GetConfiguredCooldown(AbilitySlot slot)
    {
        if (slot == AbilitySlot.Dash && dash != null)
            return dash.GetCooldownDuration();

        CharacterAbilityDefinition definition = GetDefinitionForSlot(slot);
        if (definition == null)
            return 0f;

        int tier = _progression.GetTierForSlot(slot);
        AbilityTierData tierData = definition.GetTierData(tier);
        if (tierData.cooldown > 0f)
            return tierData.cooldown;

        return definition.GetTierData(1).cooldown;
    }

    private CharacterAbilityDefinition GetDefinitionForSlot(AbilitySlot slot)
    {
        if (abilitySet == null) return null;
        return slot switch
        {
            AbilitySlot.Ability1 => abilitySet.ability1,
            AbilitySlot.Ability2 => abilitySet.ability2,
            _ => null
        };
    }

    private Vector2 ResolveAimDirection()
    {
        if (aim != null && aim.TryGetAimDirection(out Vector2 direction, out _))
            return direction;
        return firePoint != null ? (Vector2)firePoint.up : Vector2.up;
    }

    private Vector2 ResolvePlacement(float maxRange, Vector2 fallbackDirection)
    {
        if (aim != null)
        {
            var result = AbilityPlacementUtility.TryGetPlacement(transform, null, maxRange, fallbackDirection);
            if (result.Success)
                return result.WorldPosition;
        }

        var placement = AbilityPlacementUtility.TryGetPlacement(transform, Camera.main, maxRange, fallbackDirection);
        return placement.Success ? placement.WorldPosition : (Vector2)transform.position;
    }

    private void HandleWaveStatusChanged(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        if (unlockAllAbilitySlotsOnStart) return;
        _progression.SyncPhaseFromWaveIndex(currentWave - 1);
    }

    public GameObject GetSpawnPrefab(CharacterAbilityType type)
    {
        return type switch
        {
            CharacterAbilityType.CoraBarrier => barrierPrefab,
            CharacterAbilityType.CoraPool => poolPrefab,
            _ => null
        };
    }

    public void ConfigureAbilitySet(CharacterAbilitySet set)
    {
        abilitySet = set;
        CacheExecutors();
        if (_passiveHandler != null)
            _passiveHandler.Configure(set);
    }

    public void ApplyAbilitySet(CharacterAbilitySet set) => ConfigureAbilitySet(set);

    public void SetProgression(AbilityProgressionState state)
    {
        if (state != null)
            _progression = state;
    }

    [Obsolete("Use ConfigureAbilitySet. Mantido para compatibilidade com SO legado.")]
    public void EquipAbility(Ability legacyAbility)
    {
        // Legado — habilidades agora são CharacterAbilitySet + executores.
    }
}
