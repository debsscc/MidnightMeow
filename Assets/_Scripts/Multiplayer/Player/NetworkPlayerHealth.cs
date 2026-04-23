/// <summary>
/// NetworkPlayerHealth.cs
/// NetworkBehaviour que sincroniza a saúde do jogador em toda a rede.
/// Envolve o HealthComponent existente (que continua sendo a fonte de verdade local no servidor)
/// e replica o valor via NetworkVariable para todos os clientes.
/// No servidor: ouve eventos do HealthComponent local e atualiza a NetworkVariable.
/// Nos clientes: ouve mudanças da NetworkVariable e atualiza a UI do HUD.
/// Ao morrer: desabilita componentes de gameplay e ativa o modo spectator no owner.
/// SRP: exclusivamente sincronização de saúde do jogador pela rede.
/// </summary>

using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class NetworkPlayerHealth : NetworkBehaviour
{
    [SerializeField] private MultiplayerConfig config;

    private HealthComponent _healthComponent;
    private NetworkPlayerSpectator _spectator;

    // Saúde atual replicada para todos os clientes
    private NetworkVariable<float> _networkCurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _networkMaxHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _networkIsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => _networkCurrentHealth.Value;
    public float MaxHealth => _networkMaxHealth.Value;
    public bool IsDead => _networkIsDead.Value;

    // Eventos locais disparados em todos os clientes
    public static event System.Action<ulong, float, float> OnNetworkHealthChanged;
    public static event System.Action<ulong> OnNetworkPlayerDied;
    public static event System.Action<ulong> OnNetworkPlayerRespawned;

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
        _spectator = GetComponent<NetworkPlayerSpectator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // No servidor, espelha eventos do HealthComponent para a NetworkVariable
            _healthComponent.OnHealthChanged.AddListener(HandleHealthChangedOnServer);
            _healthComponent.OnDied.AddListener(HandleDiedOnServer);
        }

        // Todos os clientes ouvem mudanças da NetworkVariable
        _networkCurrentHealth.OnValueChanged += HandleNetworkHealthChanged;
        _networkIsDead.OnValueChanged += HandleNetworkDeathChanged;

        // Sincroniza o HUD imediatamente ao entrar
        NotifyHealthChanged(_networkCurrentHealth.Value, _networkMaxHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            _healthComponent.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);
            _healthComponent.OnDied.RemoveListener(HandleDiedOnServer);
        }

        _networkCurrentHealth.OnValueChanged -= HandleNetworkHealthChanged;
        _networkIsDead.OnValueChanged -= HandleNetworkDeathChanged;
    }

    // --- Servidor ---

    private void HandleHealthChangedOnServer(float current, float max)
    {
        _networkCurrentHealth.Value = current;
        _networkMaxHealth.Value = max;
    }

    private void HandleDiedOnServer()
    {
        _networkIsDead.Value = true;
        MultiplayerGameManager.Instance?.RegisterPlayerDeath();
        TriggerDeathClientRpc();
    }

    // --- Clientes ---

    private void HandleNetworkHealthChanged(float oldValue, float newValue)
    {
        NotifyHealthChanged(newValue, _networkMaxHealth.Value);
    }

    private void HandleNetworkDeathChanged(bool wasAlive, bool isDead)
    {
        if (isDead)
        {
            TriggerLocalDeath();
        }
        else
        {
            TriggerLocalRespawn();
        }
    }

    private void NotifyHealthChanged(float current, float max)
    {
        // Evento global para a UI deste cliente (barra de saúde local)
        if (IsOwner)
            GameEvents.InvokePlayerHealthChanged(current, max);

        // Evento de rede para o HUD multiplayer (mostra saúde de TODOS os jogadores)
        OnNetworkHealthChanged?.Invoke(OwnerClientId, current, max);
    }

    [ClientRpc]
    private void TriggerDeathClientRpc()
    {
        TriggerLocalDeath();
    }

    private void TriggerLocalDeath()
    {
        if (IsOwner)
        {
            // Desabilita gameplay do jogador local
            DisableGameplayComponents();

            // Ativa o modo spectator se disponível
            if (_spectator != null)
                _spectator.EnterSpectatorMode();
        }

        OnNetworkPlayerDied?.Invoke(OwnerClientId);
        GameEvents.InvokePlayerDefeated();
        Debug.Log($"[NetworkPlayerHealth] Jogador {OwnerClientId} morreu.");
    }

    private void TriggerLocalRespawn()
    {
        if (IsOwner)
        {
            EnableGameplayComponents();

            if (_spectator != null)
                _spectator.ExitSpectatorMode();
        }

        OnNetworkPlayerRespawned?.Invoke(OwnerClientId);
        Debug.Log($"[NetworkPlayerHealth] Jogador {OwnerClientId} reviveu.");
    }

    /// <summary>
    /// API pública: Server RPC para aplicar dano a este jogador.
    /// Chamado por sistemas de colisão de projéteis/inimigos no servidor.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, ulong instigatorClientId)
    {
        if (!IsServer || _networkIsDead.Value) return;
        // O instigador pode ser qualquer NetworkObject; usa gameObject do servidor como proxy
        _healthComponent.TakeDamage(amount, gameObject);
    }

    /// <summary>
    /// Respawn do jogador no servidor. Define saúde máxima e reativa estado vivo.
    /// Chamado pelo MultiplayerGameManager ou por lógica de respawn futura.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RespawnServerRpc()
    {
        if (!IsServer) return;
        if (!_networkIsDead.Value) return;

        _healthComponent.Initialize(_networkMaxHealth.Value);
        _networkIsDead.Value = false;
        MultiplayerGameManager.Instance?.RegisterPlayerRespawn();
    }

    private void DisableGameplayComponents()
    {
        var input = GetComponent<PlayerInputHandler>();
        var movement = GetComponent<PlayerMovement>();
        var shooting = GetComponent<PlayerShooting>();
        var dash = GetComponent<PlayerDash>();
        var ability = GetComponent<PlayerAbilityHandler>();

        if (input != null) input.enabled = false;
        if (movement != null) movement.enabled = false;
        if (shooting != null) shooting.enabled = false;
        if (dash != null) dash.enabled = false;
        if (ability != null) ability.enabled = false;
    }

    private void EnableGameplayComponents()
    {
        var input = GetComponent<PlayerInputHandler>();
        var movement = GetComponent<PlayerMovement>();
        var shooting = GetComponent<PlayerShooting>();
        var dash = GetComponent<PlayerDash>();
        var ability = GetComponent<PlayerAbilityHandler>();

        if (input != null) input.enabled = true;
        if (movement != null) movement.enabled = true;
        if (shooting != null) shooting.enabled = true;
        if (dash != null) dash.enabled = true;
        if (ability != null) ability.enabled = true;
    }
}
