using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gerencia kill streak e timer da passiva (cleave Nix / ricochete Cora).
/// </summary>
[DisallowMultipleComponent]
public class PlayerPassiveHandler : MonoBehaviour
{
    [SerializeField] private CharacterAbilitySet abilitySet;

    private int _killCounter;
    private float _passiveEndTime;
    private NetworkObject _networkObject;

    public event Action<bool> OnPassiveStateChanged;

    public bool IsPassiveActive => abilitySet != null && abilitySet.passive != null && Time.time < _passiveEndTime;

    public int CleaveMaxTargets =>
        IsPassiveActive && abilitySet?.passive != null ? abilitySet.passive.cleaveMaxTargets : 1;

    public int BonusProjectileBounces =>
        IsPassiveActive && abilitySet?.passive != null ? abilitySet.passive.bonusBounces : 0;

    public float CleaveAreaMultiplier =>
        IsPassiveActive && abilitySet?.passive != null ? abilitySet.passive.cleaveAreaMultiplier : 1f;

    public int PassiveKillProgress => _killCounter;

    public int PassiveKillsRequired => abilitySet?.passive != null ? abilitySet.passive.killsRequired : 0;

    public float PassiveDuration => abilitySet?.passive != null ? abilitySet.passive.passiveDuration : 0f;

    public float PassiveTimeRemaining =>
        IsPassiveActive ? Mathf.Max(0f, _passiveEndTime - Time.time) : 0f;

    private void Awake()
    {
        _networkObject = GetComponent<NetworkObject>();
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyKilledByPlayer += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilledByPlayer -= HandleEnemyKilled;
    }

    private void Update()
    {
        if (_passiveEndTime > 0f && Time.time >= _passiveEndTime)
        {
            _passiveEndTime = 0f;
            _killCounter = 0;
            OnPassiveStateChanged?.Invoke(false);
        }
    }

    private void HandleEnemyKilled(ulong killerClientId)
    {
        if (!IsLocalOwner()) return;
        if (abilitySet == null || abilitySet.passive == null) return;

        if (NetworkManager.Singleton != null && killerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        if (IsPassiveActive)
            return;

        _killCounter++;
        if (_killCounter >= abilitySet.passive.killsRequired)
        {
            _passiveEndTime = Time.time + abilitySet.passive.passiveDuration;
            _killCounter = 0;
            OnPassiveStateChanged?.Invoke(true);
        }
    }

    private bool IsLocalOwner()
    {
        if (_networkObject == null || !_networkObject.IsSpawned)
            return true;
        return _networkObject.IsOwner;
    }

    public void Configure(CharacterAbilitySet set) => abilitySet = set;
}
