/// <summary>

/// Drops de ciência em rede: homing no servidor, coleta autoritativa e eventos de progressão.

/// </summary>



using Unity.Netcode;

using UnityEngine;



[RequireComponent(typeof(Ciencia))]

public class NetworkCienciaController : NetworkBehaviour

{

    private const string PickupConfigResourcePath = "CienciaPickupConfig";



    [SerializeField] private MultiplayerConfig config;

    [SerializeField] private CienciaPickupConfig pickupConfig;



    private Ciencia _ciencia;

    private CienciaHoming _homing;

    private CircleCollider2D _pickupCollider;

    private Rigidbody2D _rigidbody;

    private bool _collected;



    private void Awake()

    {

        _ciencia = GetComponent<Ciencia>();

        _homing = GetComponent<CienciaHoming>();

        _pickupCollider = GetComponent<CircleCollider2D>();

        _rigidbody = GetComponent<Rigidbody2D>();

        EnsurePickupSetup();

    }



    public override void OnNetworkSpawn()

    {

        EnsurePickupSetup();

        _ciencia.enabled = false;



        if (_homing != null)

        {

            _homing.SetConfig(pickupConfig);

            _homing.enabled = IsServer;

        }

    }



    private void EnsurePickupSetup()

    {

        if (pickupConfig == null)
            pickupConfig = CienciaPickupConfig.LoadCached();



        if (_pickupCollider == null)

        {

            _pickupCollider = gameObject.AddComponent<CircleCollider2D>();

            _pickupCollider.isTrigger = true;

            _pickupCollider.radius = pickupConfig != null && pickupConfig.collectRadius > 0f

                ? pickupConfig.collectRadius

                : 0.55f;

        }



        if (_rigidbody == null)

        {

            _rigidbody = gameObject.AddComponent<Rigidbody2D>();

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;

            _rigidbody.gravityScale = 0f;

            _rigidbody.simulated = true;

        }



        if (_homing == null)

            _homing = gameObject.AddComponent<CienciaHoming>();



        if (pickupConfig != null)

            _homing.SetConfig(pickupConfig);

    }



    private void FixedUpdate()

    {

        if (!IsServer || !IsSpawned || _collected) return;

        TryServerCollectOverlaps();

    }



    private void OnTriggerEnter2D(Collider2D other)

    {

        if (_collected || !IsSpawned) return;

        if (!IsPlayerCollider(other)) return;



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

        float radius = GetCollectRadius();

        var hits = Physics2D.OverlapCircleAll(transform.position, radius);



        foreach (var hit in hits)

        {

            if (hit == null || !IsPlayerCollider(hit)) continue;



            var networkPlayer = hit.GetComponentInParent<NetworkPlayerController>();

            if (networkPlayer == null) continue;



            CompleteCollect(networkPlayer.OwnerClientId);

            return;

        }



        TryCollectFromConnectedPlayers(radius);

    }



    private void TryCollectFromConnectedPlayers(float radius)

    {

        NetworkManager net = NetworkManager.Singleton;

        if (net == null) return;



        float radiusSqr = radius * radius;

        foreach (ulong clientId in net.ConnectedClientsIds)

        {

            if (!net.ConnectedClients.TryGetValue(clientId, out NetworkClient client))

                continue;



            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null) continue;



            Vector3 delta = playerObject.transform.position - transform.position;

            if (delta.sqrMagnitude > radiusSqr) continue;



            CompleteCollect(clientId);

            return;

        }

    }



    private static bool IsPlayerCollider(Collider2D collider)

    {

        if (collider == null) return false;



        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer >= 0 && collider.gameObject.layer == playerLayer)

            return true;



        return collider.GetComponentInParent<NetworkPlayerController>() != null;

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



        return 0.75f;

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


