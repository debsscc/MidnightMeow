/// <summary>

/// Sincroniza vida do jogador; inconsciência (downed) e reviver; derrota só quando todos estão down.

/// </summary>



using System.Collections;
using Unity.Netcode;

using UnityEngine;



[RequireComponent(typeof(HealthComponent))]

public class NetworkPlayerHealth : NetworkBehaviour

{

    [SerializeField] private MultiplayerConfig multiplayerConfig;

    [SerializeField] private DownedPlayerConfig downedConfig;



    private HealthComponent _healthComponent;

    private NetworkPlayerSpectator _spectator;

    private PlayerDeathPresentation _deathPresentation;



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



    private readonly NetworkVariable<bool> _networkReviveZoneActive = new NetworkVariable<bool>(

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

    public bool IsReviveZoneActive => _networkReviveZoneActive.Value;

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

        _deathPresentation = GetComponent<PlayerDeathPresentation>();

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
        _networkMaxHealth.OnValueChanged += HandleNetworkMaxHealthChanged;

        _networkIsUnconscious.OnValueChanged += HandleUnconsciousChanged;
        _networkIsBleedingOut.OnValueChanged += HandleBleedingOutChanged;

        if (IsServer)
            StartCoroutine(SyncHealthToNetworkAfterInitRoutine());

        if (IsOwner)
            StartCoroutine(NotifyOwnerHealthAfterInitRoutine());

    }



    public override void OnNetworkDespawn()

    {

        if (IsServer)

        {

            _healthComponent.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);

            _healthComponent.OnDied.RemoveListener(HandleHealthReachedZeroOnServer);

        }



        _networkCurrentHealth.OnValueChanged -= HandleNetworkHealthChanged;
        _networkMaxHealth.OnValueChanged -= HandleNetworkMaxHealthChanged;

        _networkIsUnconscious.OnValueChanged -= HandleUnconsciousChanged;
        _networkIsBleedingOut.OnValueChanged -= HandleBleedingOutChanged;

    }



    private void Update()
    {
        if (!IsServer) return;

        if (!_networkIsUnconscious.Value || _networkIsBleedingOut.Value)
            return;

        if (_networkRevivePaused.Value)
            return;

        float remaining = _networkUnconsciousTimeRemaining.Value - Time.deltaTime;
        _networkUnconsciousTimeRemaining.Value = Mathf.Max(0f, remaining);

        if (_networkUnconsciousTimeRemaining.Value <= 0f)
            EnterBleedingOutOnServer();
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

        _networkReviveZoneActive.Value = false;



        MultiplayerGameManager.Instance?.RegisterPlayerDowned();
        NetworkDownedReviveManager.Instance?.RegisterDownedPlayer(OwnerClientId);
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

    public void ServerSetReviveZoneActive(bool active)
    {
        if (!IsServer)
            return;

        _networkReviveZoneActive.Value = active;
    }

    public void ServerClearReviveZone()
    {
        if (!IsServer)
            return;

        _networkReviveZoneActive.Value = false;
        _networkReviveProgress.Value = 0f;
        _networkRevivePaused.Value = false;
        NetworkDownedReviveManager.Instance?.UnregisterDownedPlayer(OwnerClientId);
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

        _networkReviveZoneActive.Value = false;



        MultiplayerGameManager.Instance?.RegisterPlayerRevived();
        NetworkDownedReviveManager.Instance?.UnregisterDownedPlayer(OwnerClientId);
    }

    private void EnterBleedingOutOnServer()
    {
        if (!IsServer || !_networkIsUnconscious.Value || _networkIsBleedingOut.Value)
            return;

        _networkIsBleedingOut.Value = true;
        _networkReviveProgress.Value = 0f;
        _networkRevivePaused.Value = false;
        _networkReviveZoneActive.Value = false;
        NetworkDownedReviveManager.Instance?.UnregisterDownedPlayer(OwnerClientId);
    }



    private void HandleNetworkHealthChanged(float oldValue, float newValue)
    {
        if (!IsServer && _healthComponent != null && !_networkIsUnconscious.Value)
            _healthComponent.ApplyNetworkMirror(newValue, _networkMaxHealth.Value, false);

        NotifyHealthChanged(newValue, _networkMaxHealth.Value);
    }

    private void HandleNetworkMaxHealthChanged(float oldValue, float newValue)
    {
        if (!IsServer && _healthComponent != null && !_networkIsUnconscious.Value)
            _healthComponent.ApplyNetworkMirror(_networkCurrentHealth.Value, newValue, false);

        NotifyHealthChanged(_networkCurrentHealth.Value, newValue);
    }

    private IEnumerator SyncHealthToNetworkAfterInitRoutine()
    {
        for (int i = 0; i < 30; i++)
        {
            yield return null;

            if (_healthComponent == null || _networkIsUnconscious.Value)
                yield break;

            float current = _healthComponent.CurrentHealth;
            float max = _healthComponent.MaxHealth;
            if (max <= 0f || current <= 0f)
                continue;

            _networkCurrentHealth.Value = current;
            _networkMaxHealth.Value = max;
            yield break;
        }
    }

    private IEnumerator NotifyOwnerHealthAfterInitRoutine()
    {
        for (int i = 0; i < 30; i++)
        {
            yield return null;

            if (!IsOwner)
                yield break;

            if (!IsSpawned || !CanFight)
                continue;

            NotifyHealthChanged(CurrentHealth, MaxHealth);
            yield break;
        }
    }



    private void HandleUnconsciousChanged(bool was, bool isUnconscious)

    {

        if (isUnconscious)

            ApplyUnconsciousLocal();

        else

            ApplyReviveLocal();

    }



    private void ApplyUnconsciousLocal()
    {
        if (IsOwner)
            DisableGameplayComponents();

        OnNetworkPlayerDowned?.Invoke(OwnerClientId);

        if (_deathPresentation != null)
        {
            if (ShouldUseDownedPresentation())
                _deathPresentation.BeginDownedPresentation();
            else
                _deathPresentation.BeginDeathPresentation(ShouldDissolveAfterDeathHold());
            return;
        }

        if (TryGetComponent<PlayerAnimationHandler>(out var animationHandler))
            animationHandler.HandleDeath();
    }

    private bool ShouldUseDownedPresentation()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return false;

        return HasAliveAlly();
    }

    private bool HasAliveAlly()
    {
        NetworkPlayerHealth[] players =
            Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth other = players[i];
            if (other == null || other == this || !other.IsSpawned)
                continue;

            if (other.CanFight)
                return true;
        }

        return false;
    }

    private bool ShouldDissolveAfterDeathHold()
    {
        return HasAliveAlly();
    }

    private void HandleBleedingOutChanged(bool wasBleedingOut, bool isBleedingOut)
    {
        if (!isBleedingOut || wasBleedingOut)
            return;

        if (!HasAliveAlly())
            return;

        if (_deathPresentation != null)
            _deathPresentation.BeginDeathPresentation(dissolveAfterHold: true);
    }



    private void ApplyReviveLocal()
    {
        if (IsOwner)
        {
            EnableGameplayComponents();

            if (_spectator != null)
                _spectator.ExitSpectatorMode();
        }

        _deathPresentation?.CancelPresentation();

        if (TryGetComponent<PlayerAnimationHandler>(out var animationHandler))
            animationHandler.RestoreFromDowned();

        OnNetworkPlayerRevived?.Invoke(OwnerClientId);
    }



    private void NotifyHealthChanged(float current, float max)

    {

        if (IsOwner && CanFight)

            GameEvents.InvokePlayerHealthChanged(current, max);



        OnNetworkHealthChanged?.Invoke(OwnerClientId, current, max);

    }



    /// <summary>Dano autoritativo no servidor (telegraph/projétil inimigo). Replica vida e feedback visual.</summary>
    public bool ServerApplyExternalDamage(float amount, GameObject instigator)
    {
        if (!IsServer || !CanFight || amount <= 0f)
            return false;

        if (TryGetComponent<NetworkPlayerAbilityRelay>(out var relay) && relay.NetworkIsDashing)
            return false;

        float before = _healthComponent.CurrentHealth;
        _healthComponent.TakeDamage(amount, instigator != null ? instigator : gameObject);
        float dealt = Mathf.Max(0f, before - _healthComponent.CurrentHealth);
        if (dealt <= 0f)
            return false;

        ShowDamageNumberClientRpc(dealt);
        PlayTakeDamageVisualClientRpc();
        return true;
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(float amount, ulong instigatorClientId)
    {
        if (!IsServer || !CanFight || amount <= 0f) return;
        ServerApplyExternalDamage(amount, gameObject);
    }

    [ClientRpc]
    private void ShowDamageNumberClientRpc(float amount)
    {
        GameEvents.InvokeDamageShown(amount, transform.position + Vector3.up * 0.5f);
    }

    [ClientRpc]
    private void PlayTakeDamageVisualClientRpc()
    {
        if (TryGetComponent<SpriteBlink>(out var blink))
            blink.Blink();

        if (TryGetComponent<Animator>(out var animator))
            animator.SetTrigger(Animator.StringToHash("OnDamage"));

        if (IsOwner)
            PlayerCameraFeedback.ShakeOnLocalPlayerDamage();
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

        if (TryGetComponent<PlayerFacingController>(out var facing)) facing.enabled = enabled;

        if (TryGetComponent<PlayerAim>(out var aim)) aim.enabled = enabled;

    }

    public static bool TryGetLastDownedFocusTarget(out Transform focusTarget)
    {
        focusTarget = null;

        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth player = players[i];
            if (player == null || !player.IsSpawned || !player.IsUnconscious)
                continue;

            focusTarget = player.transform;
        }

        return focusTarget != null;
    }

}
