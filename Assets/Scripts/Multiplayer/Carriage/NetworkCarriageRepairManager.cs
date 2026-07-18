using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Conserto cooperativo da carruagem — autoridade no servidor, espelhando o fluxo de
/// <see cref="NetworkRatHoleSealManager"/> / <see cref="NetworkDownedReviveManager"/>.
/// Arquivo próprio (NetworkBehaviour isolado) para o source-gen de RPCs do NGO funcionar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CarriageController), typeof(NetworkCarriageHealth), typeof(NetworkObject))]
public class NetworkCarriageRepairManager : NetworkBehaviour
{
    [SerializeField] private CarriageConfig config;

    private readonly NetworkList<CarriageRepairSession> _sessions = new NetworkList<CarriageRepairSession>();

    private readonly NetworkVariable<bool> _repairActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _repairProgress = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CarriageController _carriage;
    private NetworkCarriageHealth _health;

    public CarriageConfig Config => config;
    public NetworkList<CarriageRepairSession> Sessions => _sessions;
    public bool RepairActive => _repairActive.Value;
    public float RepairProgress => _repairProgress.Value;
    public NetworkVariable<float> RepairProgressVariable => _repairProgress;
    public NetworkVariable<bool> RepairActiveVariable => _repairActive;

    private void Awake()
    {
        _carriage = GetComponent<CarriageController>();
        _health = GetComponent<NetworkCarriageHealth>();
        config = CarriageConfigUtility.Resolve(config);
    }

    public override void OnNetworkSpawn()
    {
        _sessions.OnListChanged += HandleSessionsListChanged;
        CarriageRepairZoneVisualHost.EnsureAttached(this);
    }

    public override void OnNetworkDespawn()
    {
        _sessions.OnListChanged -= HandleSessionsListChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null || _health == null)
            return;

        if (!_health.IsBroken)
        {
            if (_repairActive.Value || _sessions.Count > 0)
                ClearRepairState();
            return;
        }

        EnsureSessionSlot();

        bool dirty = false;
        for (int i = 0; i < _sessions.Count; i++)
        {
            CarriageRepairSession before = _sessions[i];
            CarriageRepairSession session = before;
            CarriageRepairZoneSystem.TickSession(ref session, config, Time.deltaTime);

            if (session.Equals(before))
                continue;

            if (!before.IsCompleted && session.IsCompleted)
            {
                _sessions[i] = session;
                PublishProgress(session);
                CompleteRepair();
                return;
            }

            if (before.IsActive && !session.IsActive)
            {
                _sessions[i] = session;
                PublishProgress(session);
                HandleSessionDeactivated();
                dirty = true;
                continue;
            }

            _sessions[i] = session;
            PublishProgress(session);
            dirty = true;
        }

        if (dirty)
            BroadcastActiveSessionToClients();
    }

    /// <summary>Cliente → servidor: inicia conserto (mesmo padrão de RequestStartSealRpc / RequestStartReviveRpc).</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartRepairRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        if (config == null)
            config = CarriageConfigUtility.Resolve();

        if (config == null || _health == null || _carriage == null)
        {
            Debug.LogWarning("[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado: config/health/carriage nulos.");
            return;
        }

        if (!_health.IsBroken)
        {
            Debug.LogWarning("[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado: carruagem não está Broken.");
            return;
        }

        ulong callerId = rpcParams.Receive.SenderClientId;
        NetworkPlayerHealth ally = FindFightingHealth(callerId);
        if (ally == null)
        {
            Debug.LogWarning($"[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado: caller {callerId} não pode lutar.");
            return;
        }

        float dist = Vector2.Distance(_carriage.transform.position, ally.transform.position);
        float promptRadius = Mathf.Max(0.5f, config.repairPromptRadius);
        if (dist > promptRadius)
        {
            Debug.LogWarning(
                $"[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado: distância {dist:F2} > raio {promptRadius:F2}.");
            return;
        }

        if (TryGetActiveSession(out _))
            return;

        int zoneCount = ResolveConnectedPlayerZoneCount();
        CooperativeZonePlacementUtility.PlacementResult placement =
            CooperativeZonePlacementUtility.TryPlaceZones(
                _carriage.transform.position,
                zoneCount,
                config.repairZoneRadius,
                config.repairMinDistance,
                config.repairMaxDistance,
                config.repairMinZoneSeparation);

        if (!placement.Success || placement.Positions == null || placement.Positions.Length == 0)
        {
            Debug.LogWarning("[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado: falha ao posicionar zonas.");
            return;
        }

        CarriageRepairSession session = new CarriageRepairSession
        {
            Flags = CarriageRepairSession.FlagActive,
            Progress = 0f,
            AbandonTimer = 0f,
            ZoneA = placement.Positions[0],
            ZoneB = placement.Positions.Length > 1 ? placement.Positions[1] : placement.Positions[0],
            ZoneC = placement.Positions.Length > 2 ? placement.Positions[2] : default,
            ZoneD = placement.Positions.Length > 3 ? placement.Positions[3] : default,
            ZoneCount = (byte)Mathf.Clamp(placement.Positions.Length, 1, CarriageRepairSession.MaxZones)
        };

        UpsertSession(session);
        PublishProgress(session);
        BroadcastSessionVisual(session);
    }

    [ClientRpc]
    private void NotifyRepairZoneVisualClientRpc(
        Vector2 zoneA,
        Vector2 zoneB,
        Vector2 zoneC,
        Vector2 zoneD,
        byte zoneCount,
        byte flags,
        float progress)
    {
        if ((flags & CarriageRepairSession.FlagActive) == 0)
            return;

        CarriageRepairZoneVisualHost.EnsureAttached(this)?.ShowSession(
            new CarriageRepairSession
            {
                Flags = flags,
                Progress = progress,
                ZoneA = zoneA,
                ZoneB = zoneB,
                ZoneC = zoneC,
                ZoneD = zoneD,
                ZoneCount = zoneCount
            },
            zoneC,
            zoneD);
    }

    [ClientRpc]
    private void NotifyRepairSessionEndedClientRpc() =>
        CarriageRepairZoneVisualHost.EnsureAttached(this)?.HideSession();

    private void CompleteRepair()
    {
        float restoreAmount = config.maxHealth * Mathf.Clamp01(config.repairRestoreHealthFraction);
        _health.ServerRestoreAfterRepair(restoreAmount);
        ClearRepairState();
        NotifyRepairCompletedClientRpc();
    }

    [ClientRpc]
    private void NotifyRepairCompletedClientRpc()
    {
        CarriageRepairZoneVisualHost.EnsureAttached(this)?.HideSession();
        GameplayInteractAudio.PlayReviveComplete();
    }

    private void HandleSessionDeactivated()
    {
        _repairActive.Value = false;
        _repairProgress.Value = 0f;
        NotifyRepairSessionEndedClientRpc();
    }

    private void ClearRepairState()
    {
        _sessions.Clear();
        _repairActive.Value = false;
        _repairProgress.Value = 0f;
    }

    private void PublishProgress(CarriageRepairSession session)
    {
        _repairActive.Value = session.IsActive;
        _repairProgress.Value = session.IsActive || session.IsCompleted ? session.Progress : 0f;
    }

    private void EnsureSessionSlot()
    {
        if (_sessions.Count == 0)
            _sessions.Add(default);
    }

    private void UpsertSession(CarriageRepairSession session)
    {
        if (_sessions.Count == 0)
            _sessions.Add(session);
        else
            _sessions[0] = session;
    }

    public bool TryGetActiveSession(out CarriageRepairSession session)
    {
        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].IsActive)
            {
                session = _sessions[i];
                return true;
            }
        }

        session = default;
        return false;
    }

    private void BroadcastActiveSessionToClients()
    {
        if (!IsServer || !TryGetActiveSession(out CarriageRepairSession session))
            return;

        BroadcastSessionVisual(session);
    }

    private void BroadcastSessionVisual(CarriageRepairSession session)
    {
        NotifyRepairZoneVisualClientRpc(
            session.ZoneA,
            session.ZoneB,
            session.ZoneC,
            session.ZoneD,
            session.ZoneCount,
            session.Flags,
            session.Progress);
    }

    private void HandleSessionsListChanged(NetworkListEvent<CarriageRepairSession> _) =>
        CarriageRepairZoneVisualHost.EnsureAttached(this)?.RefreshFromSessions();

    private static int ResolveConnectedPlayerZoneCount()
    {
        NetworkManager nm = NetworkManager.Singleton;
        int connected = nm != null ? nm.ConnectedClientsIds.Count : CountAlivePlayers();
        return Mathf.Clamp(Mathf.Max(1, connected), 1, CarriageRepairSession.MaxZones);
    }

    private static NetworkPlayerHealth FindFightingHealth(ulong clientId)
    {
        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health != null && health.IsSpawned && health.OwnerClientId == clientId && health.CanFight)
                return health;
        }

        return null;
    }

    private static int CountAlivePlayers()
    {
        int count = 0;
        foreach (NetworkPlayerHealth player in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (player != null && player.IsSpawned && player.CanFight)
                count++;
        }

        return Mathf.Max(1, count);
    }
}
