using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gerencia selamento de buracos de spawn (servidor autoritativo, estado replicado).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkRatHoleSealManager : NetworkBehaviour
{
    public static NetworkRatHoleSealManager Instance { get; private set; }

    [SerializeField] private RatHoleSealConfig config;

    private readonly NetworkList<RatHoleSealSession> _sessions = new NetworkList<RatHoleSealSession>();

    public RatHoleSealConfig Config => config;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<RatHoleSealConfig>();
            Debug.LogWarning("[NetworkRatHoleSealManager] RatHoleSealConfig não atribuído — usando instância padrão em memória.");
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        EnsureSessionsForSceneHoles();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null)
            return;

        for (int i = 0; i < _sessions.Count; i++)
        {
            RatHoleSealSession session = _sessions[i];
            RatHoleSealZoneSystem.TickSession(ref session, config, Time.deltaTime);
            if (!session.Equals(_sessions[i]))
                _sessions[i] = session;
        }
    }

    public bool IsHoleSealed(ushort holeId)
    {
        if (TryGetSession(holeId, out RatHoleSealSession session))
            return session.IsSealed;
        return false;
    }

    public bool TryGetSession(ushort holeId, out RatHoleSealSession session)
    {
        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].HoleId == holeId)
            {
                session = _sessions[i];
                return true;
            }
        }

        session = default;
        return false;
    }

    public NetworkList<RatHoleSealSession> Sessions => _sessions;

    [Rpc(SendTo.Server)]
    public void RequestStartSealRpc(ushort holeId, RpcParams rpcParams = default)
    {
        if (!IsServer || config == null)
            return;

        RatHoleSpawnPoint hole = RatHoleSpawnPoint.FindById(holeId);
        if (hole == null || hole.IsSealed)
            return;

        if (TryGetSession(holeId, out RatHoleSealSession existing) && (existing.IsSealed || existing.IsActive))
            return;

        int alivePlayers = CountAlivePlayers();
        int zoneCount = alivePlayers >= 2 ? 2 : 1;

        CooperativeZonePlacementUtility.PlacementResult placement =
            CooperativeZonePlacementUtility.TryPlaceZones(
                hole.AnchorPosition,
                zoneCount,
                config.zoneRadius,
                config.minDistanceFromHole,
                config.maxDistanceFromHole,
                config.minZoneSeparation);

        if (!placement.Success || placement.Positions == null || placement.Positions.Length == 0)
            return;

        RatHoleSealSession session = new RatHoleSealSession
        {
            HoleId = holeId,
            Flags = RatHoleSealSession.FlagActive,
            Progress = 0f,
            AbandonTimer = 0f,
            ZoneA = placement.Positions[0],
            ZoneB = placement.Positions.Length > 1 ? placement.Positions[1] : placement.Positions[0],
            ZoneCount = (byte)Mathf.Clamp(placement.Positions.Length, 1, 2)
        };

        UpsertSession(session);
    }

    private void EnsureSessionsForSceneHoles()
    {
        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null || TryGetSession(hole.HoleId, out _))
                continue;

            _sessions.Add(new RatHoleSealSession { HoleId = hole.HoleId });
        }
    }

    private void UpsertSession(RatHoleSealSession session)
    {
        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].HoleId != session.HoleId)
                continue;

            _sessions[i] = session;
            return;
        }

        _sessions.Add(session);
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
