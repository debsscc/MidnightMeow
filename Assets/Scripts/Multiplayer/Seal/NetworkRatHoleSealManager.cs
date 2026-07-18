using System.Collections;
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
    private Coroutine _refreshSessionsRoutine;

    public RatHoleSealConfig Config => config;

    public void ConfigureSealConfig(RatHoleSealConfig sealConfig)
    {
        if (sealConfig != null)
            config = sealConfig;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (config == null)
            config = Resources.Load<RatHoleSealConfig>("RatHoleSealConfig");

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
        _sessions.OnListChanged += HandleSessionsListChanged;
        RatHoleSealZoneVisual.EnsureAttached(this);

        if (!IsServer)
            return;

        EnsureSessionsForSceneHoles();
        _refreshSessionsRoutine = StartCoroutine(RefreshSessionsWhenHolesReadyRoutine());
    }

    public override void OnNetworkDespawn()
    {
        _sessions.OnListChanged -= HandleSessionsListChanged;

        if (_refreshSessionsRoutine != null)
        {
            StopCoroutine(_refreshSessionsRoutine);
            _refreshSessionsRoutine = null;
        }
    }

    private IEnumerator RefreshSessionsWhenHolesReadyRoutine()
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            EnsureSessionsForSceneHoles();

            if (RatHoleSpawnPoint.All.Count > 0 && _sessions.Count >= RatHoleSpawnPoint.All.Count)
                yield break;

            yield return new WaitForSeconds(0.25f);
        }
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null)
            return;

        bool objectiveDirty = false;

        for (int i = 0; i < _sessions.Count; i++)
        {
            RatHoleSealSession before = _sessions[i];
            RatHoleSealSession session = before;
            RatHoleSealZoneSystem.TickSession(ref session, config, Time.deltaTime);

            if (session.Equals(before))
                continue;

            if (before.IsActive != session.IsActive || before.IsSealed != session.IsSealed)
                SyncHoleBeingSealedState(session.HoleId, session.IsActive && !session.IsSealed);

            if (!before.IsSealed && session.IsSealed)
            {
                objectiveDirty = true;
                PlayHoleSealedClientRpc(session.HoleId);
            }

            _sessions[i] = session;
        }

        if (objectiveDirty)
        {
            BroadcastObjectiveStatus();
            PhaseObjectiveManager.Instance?.TryEvaluateSealVictory();
        }
    }

    private void HandleSessionsListChanged(NetworkListEvent<RatHoleSealSession> changeEvent)
    {
        if (IsServer)
        {
            BroadcastObjectiveStatus();
            return;
        }

        PhaseObjectiveStatusUtility.BroadcastCurrentStatus(PhaseObjectiveStatusUtility.CountAliveNetworkEnemies());
    }

    private void BroadcastObjectiveStatus()
    {
        int alive = PhaseObjectiveStatusUtility.CountAliveNetworkEnemies();
        PhaseObjectiveStatusUtility.BroadcastCurrentStatus(alive);
        NotifyObjectiveStatusClientRpc(alive);
    }

    [ClientRpc]
    private void PlayHoleSealedClientRpc(ushort holeId)
    {
        RatHoleSealAudioUtility.PlaySealComplete(config);
        GameEvents.InvokeTutorialSealHoleExecuted();
    }

    [ClientRpc]
    private void NotifyObjectiveStatusClientRpc(int enemiesAlive)
    {
        if (IsServer)
            return;

        PhaseObjectiveStatusUtility.BroadcastCurrentStatus(enemiesAlive);
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartSealRpc(ushort holeId, RpcParams rpcParams = default)
    {
        if (!IsServer || config == null)
            return;

        EnsureSessionsForSceneHoles();

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
        SyncHoleBeingSealedState(holeId, isBeingSealed: true);
        BroadcastObjectiveStatus();

        RatHoleSealSession started = session;
        RatHoleSealZoneVisual visual = RatHoleSealZoneVisual.EnsureAttached(this);
        visual?.ShowSession(started);

        NotifySealZoneVisualClientRpc(
            session.HoleId,
            session.ZoneA,
            session.ZoneB,
            session.ZoneCount,
            session.Flags,
            session.Progress);
    }

    [ClientRpc]
    private void NotifySealZoneVisualClientRpc(
        ushort holeId,
        Vector2 zoneA,
        Vector2 zoneB,
        byte zoneCount,
        byte flags,
        float progress)
    {
        if ((flags & RatHoleSealSession.FlagActive) == 0)
            return;

        RatHoleSealZoneVisual visual = RatHoleSealZoneVisual.EnsureAttached(this);
        visual.ShowSession(new RatHoleSealSession
        {
            HoleId = holeId,
            Flags = flags,
            Progress = progress,
            ZoneA = zoneA,
            ZoneB = zoneB,
            ZoneCount = zoneCount
        });
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

    /// <summary>
    /// Marca o buraco local para pausar spawn durante selamento ativo (servidor).
    /// </summary>
    private static void SyncHoleBeingSealedState(ushort holeId, bool isBeingSealed)
    {
        RatHoleSpawnPoint hole = RatHoleSpawnPoint.FindById(holeId);
        if (hole == null)
            return;

        hole.IsBeingSealed = isBeingSealed;
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
