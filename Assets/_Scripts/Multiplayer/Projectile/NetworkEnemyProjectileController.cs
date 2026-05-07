/// <summary>
/// NetworkEnemyProjectileController.cs
/// NetworkBehaviour que gerencia projéteis de inimigos (EnemyProjectile) na rede.
/// Apenas o servidor executa a lógica de movimento e colisão do EnemyProjectile.
/// Clientes recebem posição via NetworkTransform sem executar física ou aplicar dano.
/// Spawned pelo NetworkEnemyController (via EnemyAttack_Ranged com SpawnDelegate) no servidor.
/// SRP: controla exclusivamente o ciclo de vida de rede do projétil inimigo.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyProjectile))]
public class NetworkEnemyProjectileController : NetworkBehaviour
{
    private EnemyProjectile _enemyProjectile;
    private Rigidbody2D _rb;
    private Collider2D _collider;

    private void Awake()
    {
        _enemyProjectile = GetComponent<EnemyProjectile>();
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Clientes: desabilitam física e lógica de colisão
            // Posição sincronizada via NetworkTransform
            if (_rb != null) _rb.simulated = false;
            if (_enemyProjectile != null) _enemyProjectile.enabled = false;
            if (_collider != null) _collider.enabled = false;
        }
        else
        {
            // Servidor: lógica de colisão tratada pelo EnemyProjectile existente
            // Reescreve TakeDamage do player para ir via ServerRpc
        }
    }

    /// <summary>
    /// Despawna o projétil inimigo da rede. Chamado pelo servidor após colisão ou lifetime.
    /// </summary>
    public void DespawnProjectile()
    {
        if (!IsServer) return;
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
