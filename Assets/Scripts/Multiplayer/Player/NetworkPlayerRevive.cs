/// <summary>

/// Reviver jogadores inconscientes: segurar Interact perto do corpo (servidor autoritativo).

/// </summary>



using Unity.Netcode;

using UnityEngine;



[RequireComponent(typeof(NetworkPlayerHealth), typeof(PlayerInputHandler))]

public class NetworkPlayerRevive : NetworkBehaviour

{

    [SerializeField] private DownedPlayerConfig downedConfig;



    private NetworkPlayerHealth _selfHealth;

    private PlayerInputHandler _input;



    private readonly NetworkVariable<bool> _networkIsReviving = new NetworkVariable<bool>(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private ulong _reviveTargetId = ulong.MaxValue;

    private float _serverReviveProgress;

    private bool _isHoldingInteract;



    public bool IsReviving => _networkIsReviving.Value;



    private void Awake()

    {

        _selfHealth = GetComponent<NetworkPlayerHealth>();

        _input = GetComponent<PlayerInputHandler>();

        if (downedConfig == null)
            downedConfig = _selfHealth.DownedConfig;

    }



    public override void OnNetworkSpawn()

    {

        if (IsOwner && _input != null)

            _input.OnInteractHoldChanged += HandleInteractHoldChanged;

    }



    public override void OnNetworkDespawn()

    {

        if (IsOwner && _input != null)

            _input.OnInteractHoldChanged -= HandleInteractHoldChanged;



        if (IsServer)

            ClearReviveState();

    }



    private void Update()

    {

        if (!IsServer || downedConfig == null) return;

        ServerUpdateRevive();

    }



    private void FixedUpdate()

    {

        if (!IsOwner || !_networkIsReviving.Value) return;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        if (_rb != null)

            _rb.linearVelocity = Vector2.zero;

    }



    private Rigidbody2D _rb;



    private void ServerUpdateRevive()

    {

        bool canRevive = _isHoldingInteract && _selfHealth.CanFight && _reviveTargetId != ulong.MaxValue;

        _networkIsReviving.Value = canRevive;



        if (!canRevive)

        {

            if (_reviveTargetId != ulong.MaxValue)

            {

                var previous = FindPlayerHealth(_reviveTargetId);

                previous?.ServerSetRevivePaused(false);

            }



            if (_serverReviveProgress > 0f && downedConfig.reviveProgressDecayPerSecond > 0f)

                _serverReviveProgress = Mathf.Max(0f, _serverReviveProgress - downedConfig.reviveProgressDecayPerSecond * Time.deltaTime);

            else

                _serverReviveProgress = 0f;



            SyncTargetProgress();

            return;

        }



        var target = FindPlayerHealth(_reviveTargetId);

        if (target == null || !target.CanBeRevived)

        {

            ClearReviveState();

            return;

        }



        float dist = Vector2.Distance(transform.position, target.transform.position);

        if (dist > downedConfig.reviveRange)

        {

            target.ServerSetRevivePaused(false);

            _serverReviveProgress = Mathf.Max(0f, _serverReviveProgress - downedConfig.reviveProgressDecayPerSecond * Time.deltaTime);

            SyncTargetProgress(target);

            return;

        }



        target.ServerSetRevivePaused(true);

        float hold = Mathf.Max(0.1f, downedConfig.reviveHoldDuration);

        _serverReviveProgress += Time.deltaTime / hold;

        target.ServerSetReviveProgress(_serverReviveProgress);



        if (_serverReviveProgress >= 1f)

        {

            target.ServerReviveFromUnconscious();

            ClearReviveState();

        }

    }



    private void SyncTargetProgress(NetworkPlayerHealth target = null)

    {

        target ??= FindPlayerHealth(_reviveTargetId);

        target?.ServerSetReviveProgress(_serverReviveProgress);

    }



    private void HandleInteractHoldChanged(bool holding)

    {

        _isHoldingInteract = holding;



        if (!_selfHealth.CanFight)

        {

            SetReviveIntentRpc(ulong.MaxValue);

            return;

        }



        ulong targetId = holding ? FindNearestDownedTeammate() : ulong.MaxValue;

        SetReviveIntentRpc(targetId);

    }



    [Rpc(SendTo.Server)]

    private void SetReviveIntentRpc(ulong targetClientId)

    {

        if (targetClientId != _reviveTargetId)

            _serverReviveProgress = 0f;



        bool holding = targetClientId != ulong.MaxValue;

        if (!holding)
        {
            ClearReviveState();
            return;
        }

        if (targetClientId != _reviveTargetId)
            _serverReviveProgress = 0f;

        _reviveTargetId = targetClientId;
        _isHoldingInteract = true;
    }



    private void ClearReviveState()

    {

        var previous = FindPlayerHealth(_reviveTargetId);

        previous?.ServerSetRevivePaused(false);

        previous?.ServerSetReviveProgress(0f);



        _reviveTargetId = ulong.MaxValue;

        _serverReviveProgress = 0f;

        _networkIsReviving.Value = false;

        _isHoldingInteract = false;

    }



    private ulong FindNearestDownedTeammate()

    {

        if (downedConfig == null) return ulong.MaxValue;



        float best = float.MaxValue;

        ulong bestId = ulong.MaxValue;

        Vector2 pos = transform.position;



        foreach (var netHealth in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))

        {

            if (!netHealth.IsSpawned || netHealth.OwnerClientId == OwnerClientId) continue;

            if (!netHealth.CanBeRevived) continue;



            float dist = Vector2.Distance(pos, netHealth.transform.position);

            if (dist <= downedConfig.reviveRange && dist < best)

            {

                best = dist;

                bestId = netHealth.OwnerClientId;

            }

        }



        return bestId;

    }



    private static NetworkPlayerHealth FindPlayerHealth(ulong clientId)

    {

        foreach (var h in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))

        {

            if (h.IsSpawned && h.OwnerClientId == clientId)

                return h;

        }



        return null;

    }

}


