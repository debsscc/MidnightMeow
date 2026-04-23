/// <summary>
/// NetworkProjectileController.cs
/// NetworkBehaviour que gerencia o comportamento de projéteis do jogador na rede.
/// No servidor: ativa a física (Rigidbody2D) e a lógica de colisão do Projectile existente.
///   Ao detectar colisão com inimigo, aplica dano via ServerRpc e despawna se necessário.
/// Nos clientes: desativa física e lógica de colisão; apenas recebe posição via NetworkTransform.
/// Projéteis coletáveis são despawnados via ServerRpc quando um jogador os coleta.
/// SRP: controla exclusivamente o ciclo de vida de rede do projétil do jogador.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Projectile), typeof(Rigidbody2D))]
public class NetworkProjectileController : NetworkBehaviour
{
    private Projectile _projectile;
    private Rigidbody2D _rb;
    private Collider2D _collider;

    // Dados de inicialização (preenchidos pelo NetworkProjectileSpawner via ServerRpc)
    private Vector2 _direction;
    private float _damageMultiplier = 1f;
    private int _bonusBounces = 0;
    private ulong _ownerClientId;
    private bool _initialized = false;

    private void Awake()
    {
        _projectile = GetComponent<Projectile>();
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Clientes desativam física; posição vem via NetworkTransform
            _rb.simulated = false;
            _projectile.enabled = false;

            // Mantém o collider desabilitado em clientes para evitar dano duplicado
            if (_collider != null) _collider.enabled = false;
        }
    }

    /// <summary>
    /// Inicializa o projétil no servidor com direção, multiplicadores e identificação do dono.
    /// Chamado pelo NetworkProjectileSpawner logo após o spawn.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void InitializeServerRpc(Vector2 direction, float damageMultiplier, int bonusBounces, ulong ownerClientId)
    {
        if (!IsServer) return;

        _direction = direction;
        _damageMultiplier = damageMultiplier;
        _bonusBounces = bonusBounces;
        _ownerClientId = ownerClientId;
        _initialized = true;

        // Inicializa o Projectile existente no servidor
        _projectile.InitializeDirection(direction);
        _projectile.SetDamageMultiplier(damageMultiplier);
        if (bonusBounces > 0) _projectile.AddBonusBounces(bonusBounces);

        // Ativa física no servidor
        _rb.simulated = true;
        _projectile.enabled = true;
        if (_collider != null) _collider.enabled = true;
    }

    /// <summary>
    /// Chamado pelo Projectile (server) quando colide com um inimigo.
    /// Aplica dano via NetworkEnemyController.TakeDamageServerRpc.
    /// Este método deve ser conectado ao callback de dano do Projectile na cena de rede.
    /// </summary>
    public void ApplyDamageToEnemy(NetworkEnemyController enemy, float damage)
    {
        if (!IsServer) return;
        enemy.TakeDamageServerRpc(damage * _damageMultiplier, _ownerClientId);
    }

    /// <summary>
    /// Solicita ao servidor a coleta deste projétil como munição.
    /// Chamado pelo jogador ao entrar em contato com o projétil coletável.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CollectAmmoServerRpc(ulong collectorClientId)
    {
        if (!IsServer) return;
        GrantAmmoClientRpc(collectorClientId);
        DespawnProjectile();
    }

    [ClientRpc]
    private void GrantAmmoClientRpc(ulong targetClientId)
    {
        // Apenas o cliente coletor recebe a munição
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            GameEvents.InvokeAmmoCollected();
        }
    }

    /// <summary>
    /// Despawna o projétil da rede (servidor). Chamado após colisão ou coleta.
    /// </summary>
    public void DespawnProjectile()
    {
        if (!IsServer) return;
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
