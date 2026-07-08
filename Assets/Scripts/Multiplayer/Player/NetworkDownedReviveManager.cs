using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gerencia sessões de reviver (servidor autoritativo, estado replicado) — transplante de <see cref="NetworkRatHoleSealManager"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkDownedReviveManager : NetworkBehaviour
{
    public static NetworkDownedReviveManager Instance { get; private set; }

    [SerializeField] private DownedPlayerConfig config;

    private readonly NetworkList<DownedReviveSession> _sessions = new NetworkList<DownedReviveSession>();

    public DownedPlayerConfig Config => config;
    public NetworkList<DownedReviveSession> Sessions => _sessions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveConfig();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _sessions.OnListChanged += HandleSessionsListChanged;
        DownedReviveZoneVisualHost.EnsureAttached(this);

        if (!IsServer)
            return;

        SyncSessionsWithDownedPlayers();
    }

    public override void OnNetworkDespawn()
    {
        _sessions.OnListChanged -= HandleSessionsListChanged;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null)
            return;

        SyncSessionsWithDownedPlayers();

        bool dirty = false;
        for (int i = 0; i < _sessions.Count; i++)
        {
            DownedReviveSession before = _sessions[i];
            DownedReviveSession session = before;
            DownedReviveZoneSystem.TickSession(ref session, config, Time.deltaTime);

            if (session.Equals(before))
                continue;

            if (!before.IsCompleted && session.IsCompleted)
                CompleteRevive(session.DownedClientId);

            if (before.IsActive && !session.IsActive)
                HandleSessionDeactivated(session.DownedClientId);

            _sessions[i] = session;
            dirty = true;

            NetworkPlayerHealth downed = FindDownedHealth(session.DownedClientId);
            if (downed != null)
            {
                bool allyInside = session.IsActive && IsSessionOccupied(session, config);
                downed.ServerSetRevivePaused(allyInside);
                downed.ServerSetReviveProgress(session.Progress);
                downed.ServerSetReviveZoneActive(session.IsActive);
            }
        }

        if (dirty)
            BroadcastActiveSessionsToClients();
    }

    public bool HasActiveSession(ulong downedClientId)
    {
        return TryGetSession(downedClientId, out DownedReviveSession session) && session.IsActive;
    }

    public bool TryGetSession(ulong downedClientId, out DownedReviveSession session)
    {
        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].DownedClientId == downedClientId)
            {
                session = _sessions[i];
                return true;
            }
        }

        session = default;
        return false;
    }

    public void RegisterDownedPlayer(ulong downedClientId)
    {
        if (!IsServer)
            return;

        if (TryGetSession(downedClientId, out _))
            return;

        _sessions.Add(new DownedReviveSession { DownedClientId = downedClientId });
    }

    public void UnregisterDownedPlayer(ulong downedClientId)
    {
        if (!IsServer)
            return;

        int index = FindSessionIndex(downedClientId);
        if (index >= 0)
            _sessions.RemoveAt(index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartReviveRpc(ulong downedClientId, RpcParams rpcParams = default)
    {
        if (!IsServer || config == null)
            return;

        ulong callerId = rpcParams.Receive.SenderClientId;
        if (callerId == downedClientId)
            return;

        NetworkPlayerHealth downed = FindDownedHealth(downedClientId);
        NetworkPlayerHealth ally = FindFightingHealth(callerId);
        if (downed == null || ally == null || !downed.CanBeRevived)
            return;

        if (Vector2.Distance(downed.transform.position, ally.transform.position) > config.revivePromptRadius)
            return;

        if (TryGetSession(downedClientId, out DownedReviveSession existing) && (existing.IsActive || existing.IsCompleted))
            return;

        int alivePlayers = CountAlivePlayers();
        int zoneCount = alivePlayers >= 2 ? 2 : 1;
        Vector2 anchor = downed.transform.position;

        CooperativeZonePlacementUtility.PlacementResult placement =
            CooperativeZonePlacementUtility.TryPlaceZones(
                anchor,
                zoneCount,
                config.reviveZoneRadius,
                config.reviveZonePlacementMinDistance,
                config.reviveZonePlacementMaxDistance,
                config.reviveZoneMinSeparation);

        if (!placement.Success || placement.Positions == null || placement.Positions.Length == 0)
            return;

        DownedReviveSession session = new DownedReviveSession
        {
            DownedClientId = downedClientId,
            Flags = DownedReviveSession.FlagActive,
            Progress = 0f,
            AbandonTimer = 0f,
            ZoneA = placement.Positions[0],
            ZoneB = placement.Positions.Length > 1 ? placement.Positions[1] : placement.Positions[0],
            ZoneCount = (byte)Mathf.Clamp(placement.Positions.Length, 1, 2)
        };

        UpsertSession(session);
        downed.ServerSetReviveZoneActive(true);
        downed.ServerSetReviveProgress(0f);
        downed.ServerSetRevivePaused(false);

        DownedReviveZoneVisualHost.EnsureAttached(this)?.ShowSession(session);
        NotifyReviveZoneVisualClientRpc(
            session.DownedClientId,
            session.ZoneA,
            session.ZoneB,
            session.ZoneCount,
            session.Flags,
            session.Progress);
    }

    [ClientRpc]
    private void NotifyReviveZoneVisualClientRpc(
        ulong downedClientId,
        Vector2 zoneA,
        Vector2 zoneB,
        byte zoneCount,
        byte flags,
        float progress)
    {
        if ((flags & DownedReviveSession.FlagActive) == 0)
            return;

        DownedReviveZoneVisualHost.EnsureAttached(this)?.ShowSession(new DownedReviveSession
        {
            DownedClientId = downedClientId,
            Flags = flags,
            Progress = progress,
            ZoneA = zoneA,
            ZoneB = zoneB,
            ZoneCount = zoneCount
        });
    }

    private void BroadcastActiveSessionsToClients()
    {
        if (!IsServer)
            return;

        for (int i = 0; i < _sessions.Count; i++)
        {
            DownedReviveSession session = _sessions[i];
            if (!session.IsActive)
                continue;

            NotifyReviveZoneVisualClientRpc(
                session.DownedClientId,
                session.ZoneA,
                session.ZoneB,
                session.ZoneCount,
                session.Flags,
                session.Progress);
        }
    }

    [ClientRpc]
    private void NotifyReviveSessionEndedClientRpc(ulong downedClientId)
    {
        DownedReviveZoneVisualHost.EnsureAttached(this)?.HideSession(downedClientId);
    }

    private void HandleSessionDeactivated(ulong downedClientId)
    {
        NetworkPlayerHealth downed = FindDownedHealth(downedClientId);
        if (downed != null)
        {
            downed.ServerSetReviveZoneActive(false);
            downed.ServerSetReviveProgress(0f);
            downed.ServerSetRevivePaused(false);
        }

        NotifyReviveSessionEndedClientRpc(downedClientId);
    }

    private void CompleteRevive(ulong downedClientId)
    {
        FindDownedHealth(downedClientId)?.ServerReviveFromUnconscious();
    }

    private void SyncSessionsWithDownedPlayers()
    {
        var tracked = new HashSet<ulong>();

        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health == null || !health.IsSpawned || !health.CanBeRevived)
                continue;

            tracked.Add(health.OwnerClientId);
            RegisterDownedPlayer(health.OwnerClientId);
        }

        for (int i = _sessions.Count - 1; i >= 0; i--)
        {
            ulong clientId = _sessions[i].DownedClientId;
            if (tracked.Contains(clientId))
                continue;

            _sessions.RemoveAt(i);
        }
    }

    private void UpsertSession(DownedReviveSession session)
    {
        int index = FindSessionIndex(session.DownedClientId);
        if (index >= 0)
            _sessions[index] = session;
        else
            _sessions.Add(session);
    }

    private int FindSessionIndex(ulong downedClientId)
    {
        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].DownedClientId == downedClientId)
                return i;
        }

        return -1;
    }

    private static bool IsSessionOccupied(DownedReviveSession session, DownedPlayerConfig activeConfig)
    {
        if (activeConfig == null)
            return false;

        var zones = new List<Vector2>(2) { session.ZoneA };
        if (session.ZoneCount > 1)
            zones.Add(session.ZoneB);

        return CooperativeZonePlacementUtility.CountPlayersInZones(
            zones,
            activeConfig.reviveZoneRadius,
            requireDistinctZones: session.ZoneCount > 1) > 0;
    }

    private static NetworkPlayerHealth FindDownedHealth(ulong clientId)
    {
        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health != null && health.IsSpawned && health.OwnerClientId == clientId && health.CanBeRevived)
                return health;
        }

        return null;
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

    private void HandleSessionsListChanged(NetworkListEvent<DownedReviveSession> _)
    {
        DownedReviveZoneVisualHost.EnsureAttached(this)?.RefreshFromSessions();
    }

    private void ResolveConfig()
    {
        if (config != null)
            return;

        MultiplayerGameManager gameManager = MultiplayerGameManager.Instance;
        if (gameManager != null && gameManager.DownedPlayerConfig != null)
        {
            config = gameManager.DownedPlayerConfig;
            return;
        }

        MultiplayerConfig multiplayer = Resources.Load<MultiplayerConfig>("MultiplayerConfig");
        if (multiplayer != null && multiplayer.downedPlayerConfig != null)
        {
            config = multiplayer.downedPlayerConfig;
            return;
        }

        config = Resources.Load<DownedPlayerConfig>("DownedPlayerConfig");
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DownedPlayerConfig>();
            Debug.LogWarning("[NetworkDownedReviveManager] DownedPlayerConfig ausente — usando instância padrão em memória.");
        }
    }
}
