/// <summary>
/// Drops de ciência em rede: homing no servidor, coleta autoritativa e eventos de progressão.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Ciencia))]
public class NetworkCienciaController : NetworkBehaviour
{
    [SerializeField] private MultiplayerConfig config;
    [SerializeField] private CienciaPickupConfig pickupConfig;

    private Ciencia _ciencia;
    private CienciaHoming _homing;
    private CircleCollider2D _pickupCollider;
    private bool _collected;

    private void Awake()
    {
        _ciencia = GetComponent<Ciencia>();
        _homing = GetComponent<CienciaHoming>();
        _pickupCollider = GetComponent<CircleCollider2D>();
    }

    public override void OnNetworkSpawn()
    {
        _ciencia.enabled = false;

        if (_homing != null)
        {
            _homing.SetConfig(pickupConfig);
            _homing.enabled = IsServer;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || _collected) return;
        TryServerCollectOverlaps();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        var networkPlayer = other.GetComponentInParent<NetworkPlayerController>();
        if (networkPlayer == null) return;

        if (IsServer)
            CompleteCollect(networkPlayer.OwnerClientId);
        else if (networkPlayer.IsOwner)
            RequestCollectRpc(networkPlayer.OwnerClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestCollectRpc(ulong collectorClientId)
    {
        if (!IsServer || _collected) return;
        CompleteCollect(collectorClientId);
    }

    private void TryServerCollectOverlaps()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0) return;

        float radius = GetCollectRadius();
        var hits = Physics2D.OverlapCircleAll(transform.position, radius, 1 << playerLayer);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var networkPlayer = hit.GetComponentInParent<NetworkPlayerController>();
            if (networkPlayer == null) continue;

            CompleteCollect(networkPlayer.OwnerClientId);
            return;
        }
    }

    private float GetCollectRadius()
    {
        if (pickupConfig != null && pickupConfig.collectRadius > 0f)
            return pickupConfig.collectRadius;

        if (_pickupCollider != null)
        {
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return _pickupCollider.radius * scale;
        }

        return 0.5f;
    }

    private void CompleteCollect(ulong collectorClientId)
    {
        if (_collected) return;
        _collected = true;

        if (_homing != null)
            _homing.enabled = false;

        int amount = _ciencia.GetValue();
        if (amount <= 0)
        {
            DespawnPickup();
            return;
        }

        bool shared = config != null && config.sharedSciencePool;

        if (shared)
            GrantScienceToAllClientRpc(amount);
        else
            GrantScienceToCollectorClientRpc(amount, collectorClientId);

        DespawnPickup();
    }

    private void DespawnPickup()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    [ClientRpc]
    private void GrantScienceToAllClientRpc(int amount)
    {
        GameEvents.InvokeCienciaCollected(amount);
    }

    [ClientRpc]
    private void GrantScienceToCollectorClientRpc(int amount, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
            GameEvents.InvokeCienciaCollected(amount);
    }
}
