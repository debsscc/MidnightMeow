/// <summary>
/// NetworkProjectileSpawner.cs
/// NetworkBehaviour que intercepta o disparo do PlayerShooting e o substitui por
/// uma chamada ServerRpc para que o servidor spawne o projétil como NetworkObject.
/// O servidor instancia o prefab de projétil de rede, o inicializa com posição/direção
/// e o spawna para todos os clientes via NetworkObject.Spawn().
/// No cliente owner: o PlayerShooting é reconfigurado para chamar o ServerRpc em vez de
/// usar Instantiate diretamente.
/// SRP: exclusivamente responsável por spawnar projéteis do jogador na rede.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerShooting))]
public class NetworkProjectileSpawner : NetworkBehaviour
{
    [Header("Prefab de Projétil de Rede")]
    [Tooltip("Prefab do projétil com NetworkObject e NetworkProjectileController.")]
    [SerializeField] private GameObject networkProjectilePrefab;

    private PlayerShooting _shooting;

    private void Awake()
    {
        _shooting = GetComponent<PlayerShooting>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Substitui o evento de projétil instanciado pelo disparo de rede
            _shooting.OnProjectileInstantiated += HandleProjectileInstantiatedLocally;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            _shooting.OnProjectileInstantiated -= HandleProjectileInstantiatedLocally;
    }

    /// <summary>
    /// Intercepta o projétil criado localmente pelo PlayerShooting (via Instantiate).
    /// Destrói o objeto local e solicita ao servidor que spawne a versão de rede.
    /// </summary>
    private void HandleProjectileInstantiatedLocally(GameObject localProjectile)
    {
        if (localProjectile == null) return;

        // Captura os dados antes de destruir o objeto local
        Vector3 position = localProjectile.transform.position;
        Quaternion rotation = localProjectile.transform.rotation;

        Projectile proj = localProjectile.GetComponent<Projectile>();
        float damageMultiplier = 1f;
        int bonusBounces = 0;

        // Extrai multiplicadores do projétil local antes de destruir
        // (esses valores já foram aplicados pelo PlayerShooting)
        var adrenaline = GetComponent<PlayerAdrenaline>();
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) damageMultiplier = shooting.DamageMultiplier;
        if (adrenaline != null && adrenaline.IsFrenzyActive) bonusBounces = adrenaline.GetBonusBounces();

        // Determina a direção com base na rotação do projétil
        Vector2 direction = rotation * Vector2.up;

        // Destrói o projétil instanciado localmente pelo PlayerShooting
        Destroy(localProjectile);

        // Solicita ao servidor o spawn do projétil de rede
        SpawnProjectileServerRpc(position, rotation, direction, damageMultiplier, bonusBounces);
    }

    /// <summary>
    /// Spawna um projétil de rede no servidor. Replicado para todos os clientes.
    /// </summary>
    [ServerRpc]
    private void SpawnProjectileServerRpc(
        Vector3 position,
        Quaternion rotation,
        Vector2 direction,
        float damageMultiplier,
        int bonusBounces)
    {
        if (networkProjectilePrefab == null)
        {
            Debug.LogError("[NetworkProjectileSpawner] networkProjectilePrefab não atribuído!");
            return;
        }

        GameObject projectileObj = Instantiate(networkProjectilePrefab, position, rotation);
        NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[NetworkProjectileSpawner] Prefab de projétil não tem NetworkObject!");
            Destroy(projectileObj);
            return;
        }

        netObj.Spawn(true);

        // Inicializa o projétil após o spawn na rede
        var networkProjectile = projectileObj.GetComponent<NetworkProjectileController>();
        if (networkProjectile != null)
        {
            networkProjectile.InitializeServerRpc(direction, damageMultiplier, bonusBounces, OwnerClientId);
        }
    }
}
