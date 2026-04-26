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
    private void HandleProjectileInstantiatedLocally(GameObject localProjectile, Vector2 fireDirection)
    {
        if (localProjectile == null) return;

        Vector3 position = localProjectile.transform.position;
        Vector2 direction = fireDirection.sqrMagnitude > Mathf.Epsilon
            ? fireDirection.normalized
            : (Vector2)localProjectile.transform.up;

        float fireAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, fireAngle);

        float damageMultiplier = 1f;
        int bonusBounces = 0;

        var adrenaline = GetComponent<PlayerAdrenaline>();
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) damageMultiplier = shooting.DamageMultiplier;
        if (adrenaline != null && adrenaline.IsFrenzyActive) bonusBounces = adrenaline.GetBonusBounces();

        Destroy(localProjectile);

        SpawnProjectileRpc(position, rotation, direction, damageMultiplier, bonusBounces);
    }

    /// <summary>
    /// Spawna um projétil de rede no servidor. Replicado para todos os clientes.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SpawnProjectileRpc(
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

        PlayerAmmo playerAmmo = GetComponent<PlayerAmmo>();
        if (playerAmmo == null || !playerAmmo.HasAmmo())
        {
            Debug.LogWarning("[NetworkProjectileSpawner] Spawn ignorado: sem munição no servidor.");
            if (playerAmmo != null)
            {
                var rejectParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                };
                SyncAmmoToOwnerClientRpc(playerAmmo.CurrentAmmo, rejectParams);
            }
            return;
        }

        playerAmmo.UseAmmo(1);

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
            networkProjectile.ServerApplySpawnData(direction, damageMultiplier, bonusBounces, OwnerClientId);
        }

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
        SyncAmmoToOwnerClientRpc(playerAmmo.CurrentAmmo, clientRpcParams);
    }

    [ClientRpc]
    private void SyncAmmoToOwnerClientRpc(int currentAmmo, ClientRpcParams clientRpcParams = default)
    {
        var ammo = GetComponent<PlayerAmmo>();
        if (ammo != null)
            ammo.ApplySyncedAmmo(currentAmmo);
    }
}
