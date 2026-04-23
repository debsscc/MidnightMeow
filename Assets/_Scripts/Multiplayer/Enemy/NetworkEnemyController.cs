/// <summary>
/// NetworkEnemyController.cs
/// NetworkBehaviour server-autoritativo para inimigos no multiplayer.
/// No servidor/host: toda a lógica de IA (NavMeshAgent, ataques, pathfinding) roda normalmente.
/// Nos clientes: NavMeshAgent e componentes de ataque são desativados; a posição é recebida
/// via NetworkTransform. A saúde é sincronizada via NetworkVariable, e TakeDamage só é
/// processado no servidor para evitar dano duplicado por múltiplos clientes.
/// Ouve o HealthComponent no servidor para detectar a morte e desaparecer do objeto de rede.
/// SRP: apenas controla a autoridade de IA de inimigos na rede.
/// </summary>

using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class NetworkEnemyController : NetworkBehaviour
{
    // Componentes de IA que só devem rodar no servidor
    private EnemyMovement _movement;
    private EnemyTargetFinder _targetFinder;
    private EnemyAttack_Melee _meleAttack;
    private EnemyAttack_Ranged _rangedAttack;
    private EnemyAnimationHandler _animationHandler;
    private EnemyDropHandler _dropHandler;
    private HealthComponent _health;
    private NavMeshAgent _agent;

    private NetworkVariable<float> _networkHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _networkIsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _targetFinder = GetComponent<EnemyTargetFinder>();
        _meleAttack = GetComponent<EnemyAttack_Melee>();
        _rangedAttack = GetComponent<EnemyAttack_Ranged>();
        _animationHandler = GetComponent<EnemyAnimationHandler>();
        _dropHandler = GetComponent<EnemyDropHandler>();
        _health = GetComponent<HealthComponent>();
        _agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // No servidor todos os componentes de IA ficam ativos
            SetAIComponentsActive(true);

            if (_health != null)
            {
                _health.OnHealthChanged.AddListener(HandleHealthChangedOnServer);
                _health.OnDied.AddListener(HandleDiedOnServer);
            }
        }
        else
        {
            // Nos clientes, desativa IA e física para evitar conflito com NetworkTransform
            SetAIComponentsActive(false);

            // Animação visual permanece ativa em todos os clientes
            if (_animationHandler != null)
                _animationHandler.enabled = true;
        }

        _networkIsDead.OnValueChanged += HandleNetworkDeathChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _health != null)
        {
            _health.OnHealthChanged.RemoveListener(HandleHealthChangedOnServer);
            _health.OnDied.RemoveListener(HandleDiedOnServer);
        }
        _networkIsDead.OnValueChanged -= HandleNetworkDeathChanged;
    }

    private void HandleHealthChangedOnServer(float current, float max)
    {
        _networkHealth.Value = current;
    }

    private void HandleDiedOnServer()
    {
        if (!IsServer) return;
        _networkIsDead.Value = true;

        // Pequeno delay para a animação de morte tocar nos clientes antes de desaparecer
        Invoke(nameof(DespawnEnemy), 0.3f);
    }

    private void DespawnEnemy()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    private void HandleNetworkDeathChanged(bool wasAlive, bool isDead)
    {
        if (isDead && !IsServer)
        {
            // Dispara animação de morte visualmente no cliente
            if (_animationHandler != null)
                _animationHandler.enabled = true;
        }
    }

    /// <summary>
    /// Aplica dano ao inimigo. Deve ser chamado apenas no servidor.
    /// Projéteis do jogador devem verificar IsServer antes de chamar este método.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, ulong instigatorClientId)
    {
        if (!IsServer || _networkIsDead.Value) return;
        _health?.TakeDamage(amount, gameObject);
    }

    private void SetAIComponentsActive(bool active)
    {
        if (_movement != null) _movement.enabled = active;
        if (_targetFinder != null) _targetFinder.enabled = active;
        if (_meleAttack != null) _meleAttack.enabled = active;
        if (_rangedAttack != null) _rangedAttack.enabled = active;
        if (_dropHandler != null) _dropHandler.enabled = active;

        // Desativa o NavMeshAgent inteiramente nos clientes para evitar conflito
        if (_agent != null) _agent.enabled = active;
    }
}
