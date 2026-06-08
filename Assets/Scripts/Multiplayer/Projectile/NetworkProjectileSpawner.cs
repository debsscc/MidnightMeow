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

using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerShooting))]
public class NetworkProjectileSpawner : NetworkBehaviour
{
    public readonly struct OwnerShotSnapshot
    {
        public readonly Vector3 Position;
        public readonly Vector2 Direction;
        public readonly Quaternion Rotation;
        public readonly float DamageMultiplier;
        public readonly int BonusBounces;

        public OwnerShotSnapshot(Vector3 position, Vector2 direction, Quaternion rotation, float damageMultiplier, int bonusBounces)
        {
            Position = position;
            Direction = direction;
            Rotation = rotation;
            DamageMultiplier = damageMultiplier;
            BonusBounces = bonusBounces;
        }
    }

    public readonly struct ServerShotSnapshot
    {
        public readonly ulong OwnerClientId;
        public readonly bool Accepted;
        public readonly int AmmoBefore;
        public readonly int AmmoAfter;
        public readonly Vector2 Direction;
        public readonly string Reason;

        public ServerShotSnapshot(ulong ownerClientId, bool accepted, int ammoBefore, int ammoAfter, Vector2 direction, string reason)
        {
            OwnerClientId = ownerClientId;
            Accepted = accepted;
            AmmoBefore = ammoBefore;
            AmmoAfter = ammoAfter;
            Direction = direction;
            Reason = reason;
        }
    }

    public readonly struct ServerProjectileSpawnSnapshot
    {
        public readonly ulong OwnerClientId;
        public readonly ulong NetworkObjectId;
        public readonly Vector3 RpcPosition;
        public readonly Vector3 SpawnedPosition;
        public readonly Vector3 RotationEuler;
        public readonly Vector2 Direction;
        public readonly string PrefabName;

        public ServerProjectileSpawnSnapshot(
            ulong ownerClientId,
            ulong networkObjectId,
            Vector3 rpcPosition,
            Vector3 spawnedPosition,
            Vector3 rotationEuler,
            Vector2 direction,
            string prefabName)
        {
            OwnerClientId = ownerClientId;
            NetworkObjectId = networkObjectId;
            RpcPosition = rpcPosition;
            SpawnedPosition = spawnedPosition;
            RotationEuler = rotationEuler;
            Direction = direction;
            PrefabName = prefabName;
        }
    }

    public event Action<OwnerShotSnapshot> OnOwnerShotPrepared;
    public event Action<ServerShotSnapshot> OnServerShotValidated;
    public event Action<ServerProjectileSpawnSnapshot> OnServerProjectileSpawned;
    public event Action<ulong, int> OnAmmoSyncSentToOwner;

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
    private void HandleProjectileInstantiatedLocally(
        GameObject localProjectile,
        Vector3 shotPosition,
        Quaternion shotRotation,
        Vector2 fireDirection)
    {
        if (localProjectile == null) return;

        Vector2 direction = fireDirection.sqrMagnitude > Mathf.Epsilon
            ? fireDirection.normalized
            : (Vector2)localProjectile.transform.up;

        Quaternion rotation = direction.sqrMagnitude > Mathf.Epsilon
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f)
            : shotRotation;

        float damageMultiplier = 1f;
        int bonusBounces = 0;

        var adrenaline = GetComponent<PlayerAdrenaline>();
        var passive = GetComponent<PlayerPassiveHandler>();
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) damageMultiplier = shooting.DamageMultiplier;
        if (adrenaline != null && adrenaline.IsFrenzyActive)
            bonusBounces = adrenaline.GetBonusBounces();
        if (passive != null)
            bonusBounces += passive.BonusProjectileBounces;

        OnOwnerShotPrepared?.Invoke(new OwnerShotSnapshot(shotPosition, direction, rotation, damageMultiplier, bonusBounces));

        Destroy(localProjectile);

        SpawnProjectileRpc(shotPosition, rotation, direction, damageMultiplier, bonusBounces);
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
        Debug.Log($"[MP-SHOT-RPC] Received owner={OwnerClientId} pos={position} rotZ={rotation.eulerAngles.z:0.###} dir={direction} dmgMul={damageMultiplier:0.###} bonusBounces={bonusBounces}");

        if (networkProjectilePrefab == null)
        {
            Debug.LogError("[NetworkProjectileSpawner] networkProjectilePrefab não atribuído!");
            return;
        }

        PlayerAmmo playerAmmo = GetComponent<PlayerAmmo>();
        int ammoBeforeValidation = playerAmmo != null ? playerAmmo.CurrentAmmo : -1;
        if (playerAmmo == null || !playerAmmo.HasAmmo())
        {
            Debug.LogWarning("[NetworkProjectileSpawner] Spawn ignorado: sem munição no servidor.");
            OnServerShotValidated?.Invoke(new ServerShotSnapshot(
                OwnerClientId,
                false,
                ammoBeforeValidation,
                ammoBeforeValidation,
                direction,
                playerAmmo == null ? "PlayerAmmo ausente no servidor" : "Sem munição no servidor"));
            if (playerAmmo != null)
            {
                var rejectParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                };
                SyncAmmoToOwnerClientRpc(playerAmmo.CurrentAmmo, rejectParams);
                OnAmmoSyncSentToOwner?.Invoke(OwnerClientId, playerAmmo.CurrentAmmo);
            }
            return;
        }

        int ammoBeforeUse = playerAmmo.CurrentAmmo;
        playerAmmo.UseAmmo(1);
        int ammoAfterUse = playerAmmo.CurrentAmmo;
        OnServerShotValidated?.Invoke(new ServerShotSnapshot(
            OwnerClientId,
            true,
            ammoBeforeUse,
            ammoAfterUse,
            direction,
            "Spawn aceito no servidor"));

        GameObject projectileObj = Instantiate(networkProjectilePrefab, position, rotation);
        NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[NetworkProjectileSpawner] Prefab de projétil não tem NetworkObject!");
            Destroy(projectileObj);
            return;
        }

        netObj.Spawn(true);
        OnServerProjectileSpawned?.Invoke(new ServerProjectileSpawnSnapshot(
            OwnerClientId,
            netObj.NetworkObjectId,
            position,
            projectileObj.transform.position,
            projectileObj.transform.eulerAngles,
            direction,
            networkProjectilePrefab != null ? networkProjectilePrefab.name : "null"
        ));
        Debug.Log($"[MP-SHOT-SPAWN] Spawned owner={OwnerClientId} netObj={netObj.NetworkObjectId} prefab={networkProjectilePrefab.name} pos={projectileObj.transform.position} rot={projectileObj.transform.eulerAngles} dir={direction}");

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
        OnAmmoSyncSentToOwner?.Invoke(OwnerClientId, playerAmmo.CurrentAmmo);
    }

    [ClientRpc]
    private void SyncAmmoToOwnerClientRpc(int currentAmmo, ClientRpcParams clientRpcParams = default)
    {
        var ammo = GetComponent<PlayerAmmo>();
        if (ammo != null)
            ammo.ApplySyncedAmmo(currentAmmo);
    }
}
