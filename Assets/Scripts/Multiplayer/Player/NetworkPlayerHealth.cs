/// <summary>

/// Sincroniza vida do jogador; inconsciência (downed) e reviver; derrota só quando todos estão down.

/// </summary>



using Unity.Netcode;

using UnityEngine;



[RequireComponent(typeof(HealthComponent))]

public class NetworkPlayerHealth : NetworkBehaviour

{

    [SerializeField] private MultiplayerConfig multiplayerConfig;

    [SerializeField] private DownedPlayerConfig downedConfig;



    private HealthComponent _healthComponent;

    private NetworkPlayerSpectator _spectator;



    private NetworkVariable<float> _networkCurrentHealth = new NetworkVariable<float>(

        100f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<float> _networkMaxHealth = new NetworkVariable<float>(

        100f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<bool> _networkIsUnconscious = new NetworkVariable<bool>(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<float> _networkUnconsciousTimeRemaining = new NetworkVariable<float>(

        0f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<float> _networkReviveProgress = new NetworkVariable<float>(

        0f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<bool> _networkIsBleedingOut = new NetworkVariable<bool>(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private NetworkVariable<bool> _networkRevivePaused = new NetworkVariable<bool>(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private float _reviveProgressSendTimer;



    public float CurrentHealth => _networkCurrentHealth.Value;

    public float MaxHealth => _networkMaxHealth.Value;

    public bool IsUnconscious => _networkIsUnconscious.Value;

    public bool IsBleedingOut => _networkIsBleedingOut.Value;

    public bool IsDead => IsUnconscious;

    public bool CanBeRevived => IsUnconscious && !IsBleedingOut;

    public bool CanFight => IsSpawned && !IsUnconscious && CurrentHealth > 0f;

    public bool CanBeTargeted => CanFight;

    public float UnconsciousTimeRemaining => _networkUnconsciousTimeRemaining.Value;

    public float ReviveProgress => _networkReviveProgress.Value;

    public float UnconsciousDuration => downedConfig != null ? downedConfig.unconsciousDuration : 45f;

    public bool IsReviveTimerPaused => _networkRevivePaused.Value;

    public DownedPlayerConfig DownedConfig => downedConfig;

    public static event System.Action<ulong, float, float> OnNetworkHealthChanged;

    public static event System.Action<ulong> OnNetworkPlayerDowned;

    public static event System.Action<ulong> OnNetworkPlayerRevived;



    public static event System.Action<ulong> OnNetworkPlayerDied

    {

        add => OnNetworkPlayerDowned += value;

        remove => OnNetworkPlayerDowned -= value;

    }



    public static event System.Action<ulong> OnNetworkPlayerRespawned

    {

        add => OnNetworkPlayerRevived += value;

        remove => OnNetworkPlayerRevived -= value;

    }



    private void Awake()

    {

        _healthComponent = GetComponent<HealthComponent>();

        _spectator = GetComponent<NetworkPlayerSpectator>();

        if (downedConfig == null && multiplayerConfig != null)

            downedConfig = multiplayerConfig.downedPlayerConfig;

    }



    public override void OnNetworkSpawn()

    {

        _healthComponent.SetAllowDestroyOnDeath(false);



        if (IsServer)

        {

            _healthComponent.OnHealthChanged.AddListener(HandleHealthChangedOnServer);

            _healthComponent.OnDied.AddListener(HandleHealthReachedZeroOnServer);

        }



        _networkCurrentHealth.OnValueChanged += HandleNetworkHealthChanged;

        _networkIsUnconscious.OnValueChanged += HandleUnconsciousChanged;

    }



    public override void OnNetworkDespawn()

    {

        if (IsServer)

        {

            _healthComponent.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);

            _healthComponent.OnDied.RemoveListener(HandleHealthReachedZeroOnServer);

        }



        _networkCurrentHealth.OnValueChanged -= HandleNetworkHealthChanged;

        _networkIsUnconscious.OnValueChanged -= HandleUnconsciousChanged;

    }



    private void Update()

    {

        if (!IsServer) return;

        DownedReviveZoneSystem.TickServer(downedConfig);

        if (!_networkIsUnconscious.Value || _networkIsBleedingOut.Value) return;

        if (!_networkRevivePaused.Value)

        {

            float next = _networkUnconsciousTimeRemaining.Value - Time.deltaTime;

            _networkUnconsciousTimeRemaining.Value = Mathf.Max(0f, next);

            if (_networkUnconsciousTimeRemaining.Value <= 0f)

                _networkIsBleedingOut.Value = true;

        }

    }



    private void HandleHealthChangedOnServer(float current, float max)

    {

        if (_networkIsUnconscious.Value) return;

        _networkCurrentHealth.Value = current;

        _networkMaxHealth.Value = max;

    }



    private void HandleHealthReachedZeroOnServer()

    {

        if (!IsServer || _networkIsUnconscious.Value) return;

        EnterUnconsciousOnServer();

    }



    private void EnterUnconsciousOnServer()

    {

        float duration = downedConfig != null ? downedConfig.unconsciousDuration : 45f;



        _networkIsUnconscious.Value = true;

        _networkIsBleedingOut.Value = false;

        _networkCurrentHealth.Value = 0f;

        _networkUnconsciousTimeRemaining.Value = duration;

        _networkReviveProgress.Value = 0f;

        _networkRevivePaused.Value = false;



        MultiplayerGameManager.Instance?.RegisterPlayerDowned();

        TriggerUnconsciousClientRpc();

    }



    public void ServerSetRevivePaused(bool paused)

    {

        if (!IsServer || !_networkIsUnconscious.Value) return;

        _networkRevivePaused.Value = paused;

    }



    public void ServerSetReviveProgress(float normalized)

    {

        if (!IsServer || !_networkIsUnconscious.Value || _networkIsBleedingOut.Value) return;

        _networkReviveProgress.Value = Mathf.Clamp01(normalized);

    }



    public void ServerReviveFromUnconscious()

    {

        if (!IsServer || !_networkIsUnconscious.Value || _networkIsBleedingOut.Value) return;



        float fraction = downedConfig != null ? downedConfig.reviveHealthFraction : 0.5f;

        float restored = _networkMaxHealth.Value * Mathf.Clamp01(fraction);



        _healthComponent.Initialize(restored);

        _networkIsUnconscious.Value = false;

        _networkIsBleedingOut.Value = false;

        _networkCurrentHealth.Value = restored;

        _networkUnconsciousTimeRemaining.Value = 0f;

        _networkReviveProgress.Value = 0f;

        _networkRevivePaused.Value = false;



        MultiplayerGameManager.Instance?.RegisterPlayerRevived();

        TriggerReviveClientRpc();

    }



    private void HandleNetworkHealthChanged(float oldValue, float newValue)

    {

        NotifyHealthChanged(newValue, _networkMaxHealth.Value);

    }



    private void HandleUnconsciousChanged(bool was, bool isUnconscious)

    {

        if (isUnconscious)

            ApplyUnconsciousLocal();

        else

            ApplyReviveLocal();

    }



    [ClientRpc]

    private void TriggerUnconsciousClientRpc() => ApplyUnconsciousLocal();



    [ClientRpc]

    private void TriggerReviveClientRpc() => ApplyReviveLocal();



    private void ApplyUnconsciousLocal()

    {

        if (IsOwner)

        {

            DisableGameplayComponents();

            if (_spectator != null)

                _spectator.EnterSpectatorMode();

        }



        OnNetworkPlayerDowned?.Invoke(OwnerClientId);

        if (IsOwner)

            GameEvents.InvokePlayerDefeated();

    }



    private void ApplyReviveLocal()

    {

        if (IsOwner)

        {

            EnableGameplayComponents();

            if (_spectator != null)

                _spectator.ExitSpectatorMode();

        }



        OnNetworkPlayerRevived?.Invoke(OwnerClientId);

    }



    private void NotifyHealthChanged(float current, float max)

    {

        if (IsOwner && CanFight)

            GameEvents.InvokePlayerHealthChanged(current, max);



        OnNetworkHealthChanged?.Invoke(OwnerClientId, current, max);

    }



    [Rpc(SendTo.Server)]

    public void TakeDamageRpc(float amount, ulong instigatorClientId)

    {

        if (!IsServer || !CanFight || amount <= 0f) return;

        float before = _healthComponent.CurrentHealth;
        _healthComponent.TakeDamage(amount, gameObject);
        float dealt = Mathf.Max(0f, before - _healthComponent.CurrentHealth);
        if (dealt > 0f)
            ShowDamageNumberClientRpc(dealt);
    }

    [ClientRpc]
    private void ShowDamageNumberClientRpc(float amount)
    {
        GameEvents.InvokeDamageShown(amount, transform.position + Vector3.up * 0.5f);
    }



    private void DisableGameplayComponents() => SetGameplayEnabled(false);

    private void EnableGameplayComponents() => SetGameplayEnabled(true);



    private void SetGameplayEnabled(bool enabled)

    {

        if (TryGetComponent<PlayerInputHandler>(out var input)) input.enabled = enabled;

        if (TryGetComponent<PlayerMovement>(out var movement)) movement.enabled = enabled;

        if (TryGetComponent<PlayerShooting>(out var shooting)) shooting.enabled = enabled;

        if (TryGetComponent<PlayerMeleeCombat>(out var melee)) melee.enabled = enabled;

        if (TryGetComponent<PlayerDash>(out var dash)) dash.enabled = enabled;

        if (TryGetComponent<PlayerAbilityHandler>(out var ability)) ability.enabled = enabled;

    }

}


