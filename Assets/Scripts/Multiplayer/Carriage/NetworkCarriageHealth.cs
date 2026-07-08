using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Vida da carruagem replicada pelo servidor. Integra <see cref="HealthComponent"/> + barra de inimigo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent), typeof(CarriageDamageFilter))]
public class NetworkCarriageHealth : NetworkBehaviour
{
    private HealthComponent _health;

    private readonly NetworkVariable<float> _syncHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _syncMaxHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isBroken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsBroken => _isBroken.Value;
    public float RepairProgress => GetComponent<NetworkCarriageRepairManager>()?.RepairProgress ?? 0f;
    public bool IsRepairActive => GetComponent<NetworkCarriageRepairManager>()?.RepairActive ?? false;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _health.SetAllowDestroyOnDeath(false);

        if (GetComponent<CarriageDamageFilter>() == null)
            gameObject.AddComponent<CarriageDamageFilter>();
    }

    public void SetAllowDestroyOnDeath(bool allow) => _health.SetAllowDestroyOnDeath(allow);

    public override void OnNetworkSpawn()
    {
        _syncHealth.OnValueChanged += HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged += HandleSyncedMaxHealthChanged;
        _isBroken.OnValueChanged += HandleBrokenChanged;

        _health.OnHealthChanged.AddListener(HandleHealthChanged);
        _health.OnDied.AddListener(HandleDied);

        ApplySyncedHealthToComponent();
    }

    public override void OnNetworkDespawn()
    {
        _syncHealth.OnValueChanged -= HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged -= HandleSyncedMaxHealthChanged;
        _isBroken.OnValueChanged -= HandleBrokenChanged;

        if (_health != null)
        {
            _health.OnHealthChanged.RemoveListener(HandleHealthChanged);
            _health.OnDied.RemoveListener(HandleDied);
        }

        base.OnNetworkDespawn();
    }

    public void ServerInitialize(float maxHealth)
    {
        if (!IsServer)
            return;

        _health.Initialize(maxHealth);
        _isBroken.Value = false;
        PublishHealthToNetwork();
    }

    public void ServerRestoreAfterRepair(float healthAmount)
    {
        if (!IsServer)
            return;

        _isBroken.Value = false;
        _health.Initialize(healthAmount);
        PublishHealthToNetwork();
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (IsServer)
            PublishHealthToNetwork();
    }

    private void HandleDied()
    {
        if (!IsServer)
            return;

        _isBroken.Value = true;
        PublishHealthToNetwork();
    }

    private void HandleSyncedHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void HandleSyncedMaxHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void HandleBrokenChanged(bool previous, bool current) => ApplySyncedHealthToComponent();

    private void ApplySyncedHealthToComponent()
    {
        if (_health == null || _syncMaxHealth.Value <= 0f)
            return;

        bool isDead = _isBroken.Value;
        _health.ApplyNetworkMirror(_syncHealth.Value, _syncMaxHealth.Value, isDead);
    }

    private void PublishHealthToNetwork()
    {
        if (_health == null)
            return;

        _syncHealth.Value = _health.CurrentHealth;
        _syncMaxHealth.Value = _health.MaxHealth;
    }
}

/// <summary>
/// Restringe dano da carruagem a ataques/projéteis de inimigos (ignora jogadores).
/// Consultado por <see cref="HealthComponent.TakeDamage"/> via TryGetComponent — mesmo padrão de
/// <see cref="PlayerDamageImmunity"/> no jogador.
/// </summary>
[DisallowMultipleComponent]
public class CarriageDamageFilter : MonoBehaviour
{
    public bool CanAcceptDamage(GameObject instigator, DamageType damageType)
    {
        if (instigator == null)
            return false;

        if (IsEnemyInstigator(instigator))
            return true;

        return false;
    }

    private static bool IsEnemyInstigator(GameObject instigator)
    {
        if (instigator.CompareTag("Enemy"))
            return true;

        if (instigator.GetComponentInParent<NetworkEnemyController>() != null)
            return true;

        if (instigator.GetComponent<EnemyProjectile>() != null)
            return true;

        if (instigator.GetComponentInParent<EnemyProjectile>() != null)
            return true;

        return false;
    }
}

// --- Conserto cooperativo (mesmo arquivo para garantir compilação) ---

/// <summary>
/// Estado replicado de conserto da carruagem (espelha <see cref="DownedReviveSession"/>).
/// </summary>
public struct CarriageRepairSession : INetworkSerializable, IEquatable<CarriageRepairSession>
{
    public const byte FlagCompleted = 1;
    public const byte FlagActive = 2;

    public byte Flags;
    public float Progress;
    public float AbandonTimer;
    public Vector2 ZoneA;
    public Vector2 ZoneB;
    public byte ZoneCount;

    public bool IsCompleted => (Flags & FlagCompleted) != 0;
    public bool IsActive => (Flags & FlagActive) != 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Progress);
        serializer.SerializeValue(ref AbandonTimer);
        serializer.SerializeValue(ref ZoneA);
        serializer.SerializeValue(ref ZoneB);
        serializer.SerializeValue(ref ZoneCount);
    }

    public bool Equals(CarriageRepairSession other) =>
        Flags == other.Flags &&
        Mathf.Approximately(Progress, other.Progress) &&
        Mathf.Approximately(AbandonTimer, other.AbandonTimer) &&
        ZoneA == other.ZoneA &&
        ZoneB == other.ZoneB &&
        ZoneCount == other.ZoneCount;

    public override bool Equals(object obj) => obj is CarriageRepairSession other && Equals(other);
    public override int GetHashCode() => Flags.GetHashCode();
}

public static class CarriageRepairZoneSystem
{
    public static void TickSession(ref CarriageRepairSession session, CarriageConfig config, float deltaTime)
    {
        if (!session.IsActive || session.IsCompleted || config == null)
            return;

        var zones = new List<Vector2>(2) { session.ZoneA };
        if (session.ZoneCount > 1)
            zones.Add(session.ZoneB);

        int occupiedZones = CooperativeZonePlacementUtility.CountPlayersInZones(
            zones, config.repairZoneRadius, requireDistinctZones: session.ZoneCount > 1);

        if (occupiedZones <= 0)
        {
            session.AbandonTimer += deltaTime;
            if (session.AbandonTimer >= config.repairAbandonTimeout)
            {
                session.Flags &= unchecked((byte)~CarriageRepairSession.FlagActive);
                session.Progress = 0f;
                session.AbandonTimer = 0f;
            }

            return;
        }

        session.AbandonTimer = 0f;
        float speed = 1f / Mathf.Max(0.1f, config.repairDuration);
        if (session.ZoneCount > 1 && occupiedZones >= 2)
            speed *= config.repairDualPlayerSpeedMultiplier;

        session.Progress = Mathf.Clamp01(session.Progress + speed * deltaTime);
        if (session.Progress < 1f)
            return;

        session.Flags |= CarriageRepairSession.FlagCompleted;
        session.Flags &= unchecked((byte)~CarriageRepairSession.FlagActive);
        session.Progress = 1f;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CarriageController), typeof(NetworkCarriageHealth))]
public class NetworkCarriageRepairManager : NetworkBehaviour
{
    [SerializeField] private CarriageConfig config;

    private readonly NetworkList<CarriageRepairSession> _sessions = new NetworkList<CarriageRepairSession>();

    private CarriageController _carriage;
    private NetworkCarriageHealth _health;

    public CarriageConfig Config => config;
    public NetworkList<CarriageRepairSession> Sessions => _sessions;
    public bool RepairActive => TryGetActiveSession(out CarriageRepairSession session) && session.IsActive;
    public float RepairProgress => TryGetActiveSession(out CarriageRepairSession session) ? session.Progress : 0f;

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
            if (_sessions.Count > 0)
                _sessions.Clear();
            return;
        }

        EnsureSessionExists();

        bool dirty = false;
        for (int i = 0; i < _sessions.Count; i++)
        {
            CarriageRepairSession before = _sessions[i];
            CarriageRepairSession session = before;
            CarriageRepairZoneSystem.TickSession(ref session, config, Time.deltaTime);

            if (session.Equals(before))
                continue;

            if (!before.IsCompleted && session.IsCompleted)
                CompleteRepair();

            if (before.IsActive && !session.IsActive)
                HandleSessionDeactivated();

            _sessions[i] = session;
            dirty = true;
        }

        if (dirty)
            BroadcastActiveSessionToClients();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartRepairRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || config == null || _health == null || !_health.IsBroken)
            return;

        ulong callerId = rpcParams.Receive.SenderClientId;
        if (!IsFightingPlayer(callerId))
            return;

        if (Vector2.Distance(_carriage.transform.position, ResolvePlayerPosition(callerId)) > config.repairPromptRadius)
            return;

        if (TryGetActiveSession(out CarriageRepairSession existing) && (existing.IsActive || existing.IsCompleted))
            return;

        int zoneCount = CountAlivePlayers() >= 2 ? 2 : 1;
        CooperativeZonePlacementUtility.PlacementResult placement =
            CooperativeZonePlacementUtility.TryPlaceZones(
                _carriage.transform.position, zoneCount, config.repairZoneRadius,
                config.repairMinDistance, config.repairMaxDistance, config.repairMinZoneSeparation);

        if (!placement.Success || placement.Positions == null || placement.Positions.Length == 0)
            return;

        CarriageRepairSession session = new CarriageRepairSession
        {
            Flags = CarriageRepairSession.FlagActive,
            Progress = 0f,
            AbandonTimer = 0f,
            ZoneA = placement.Positions[0],
            ZoneB = placement.Positions.Length > 1 ? placement.Positions[1] : placement.Positions[0],
            ZoneCount = (byte)Mathf.Clamp(placement.Positions.Length, 1, 2)
        };

        if (_sessions.Count == 0)
            _sessions.Add(session);
        else
            _sessions[0] = session;

        NotifyRepairZoneVisualClientRpc(session.ZoneA, session.ZoneB, session.ZoneCount, session.Flags, session.Progress);
    }

    [ClientRpc]
    private void NotifyRepairZoneVisualClientRpc(Vector2 zoneA, Vector2 zoneB, byte zoneCount, byte flags, float progress)
    {
        if ((flags & CarriageRepairSession.FlagActive) == 0)
            return;

        CarriageRepairZoneVisualHost.EnsureAttached(this)?.ShowSession(new CarriageRepairSession
        {
            Flags = flags,
            Progress = progress,
            ZoneA = zoneA,
            ZoneB = zoneB,
            ZoneCount = zoneCount
        });
    }

    [ClientRpc]
    private void NotifyRepairSessionEndedClientRpc() =>
        CarriageRepairZoneVisualHost.EnsureAttached(this)?.HideSession();

    private void CompleteRepair()
    {
        float restoreAmount = config.maxHealth * Mathf.Clamp01(config.repairRestoreHealthFraction);
        _health.ServerRestoreAfterRepair(restoreAmount);
        _sessions.Clear();
        NotifyRepairSessionEndedClientRpc();
    }

    private void HandleSessionDeactivated() => NotifyRepairSessionEndedClientRpc();

    private void EnsureSessionExists()
    {
        if (_sessions.Count == 0)
            _sessions.Add(default);
    }

    private bool TryGetActiveSession(out CarriageRepairSession session)
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

        NotifyRepairZoneVisualClientRpc(session.ZoneA, session.ZoneB, session.ZoneCount, session.Flags, session.Progress);
    }

    private void HandleSessionsListChanged(NetworkListEvent<CarriageRepairSession> _) =>
        CarriageRepairZoneVisualHost.EnsureAttached(this)?.RefreshFromSessions();

    private static bool IsFightingPlayer(ulong clientId)
    {
        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health != null && health.IsSpawned && health.OwnerClientId == clientId && health.CanFight)
                return true;
        }

        return false;
    }

    private static Vector2 ResolvePlayerPosition(ulong clientId)
    {
        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health != null && health.IsSpawned && health.OwnerClientId == clientId)
                return health.transform.position;
        }

        return Vector2.zero;
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

[DisallowMultipleComponent]
public class CarriageRepairZoneVisualHost : MonoBehaviour
{
    public static CarriageRepairZoneVisualHost Instance { get; private set; }

    private readonly List<GameObject> _zoneObjects = new List<GameObject>(2);
    private NetworkCarriageRepairManager _manager;
    private Transform _poolRoot;
    private bool _subscribed;

    public static CarriageRepairZoneVisualHost EnsureAttached(NetworkCarriageRepairManager manager)
    {
        if (manager == null)
            return Instance;

        CarriageRepairZoneVisualHost existing = manager.GetComponentInChildren<CarriageRepairZoneVisualHost>(true);
        if (existing != null)
        {
            existing.Bind(manager);
            return existing;
        }

        var host = new GameObject("CarriageRepairZoneVisuals");
        host.transform.SetParent(manager.transform, false);
        var visual = host.AddComponent<CarriageRepairZoneVisualHost>();
        visual.Bind(manager);
        return visual;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsurePoolRoot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Unsubscribe();
    }

    private void OnEnable() => RefreshFromSessions();
    private void LateUpdate() => RefreshFromSessions();
    public void RefreshFromSessions() => RefreshAllZones();
    public void HideSession() => SetZonesActive(false);

    public void Bind(NetworkCarriageRepairManager manager)
    {
        if (manager == null)
            return;

        if (_manager != manager)
        {
            Unsubscribe();
            _manager = manager;
            Subscribe();
        }

        RefreshAllZones();
    }

    public void ShowSession(CarriageRepairSession session)
    {
        if (!session.IsActive)
            return;

        CarriageConfig config = ResolveConfig();
        if (config == null)
            return;

        RenderSession(session, config);
    }

    private void Subscribe()
    {
        if (_subscribed || _manager == null)
            return;

        _manager.Sessions.OnListChanged += HandleSessionsChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _manager == null)
            return;

        _manager.Sessions.OnListChanged -= HandleSessionsChanged;
        _subscribed = false;
    }

    private void HandleSessionsChanged(NetworkListEvent<CarriageRepairSession> _) => RefreshFromSessions();

    private void RefreshAllZones()
    {
        NetworkCarriageRepairManager manager = _manager != null ? _manager : FindManager();
        CarriageConfig config = ResolveConfig(manager);

        if (manager == null || config == null || !manager.IsSpawned)
            return;

        bool hasActive = false;
        foreach (CarriageRepairSession session in manager.Sessions)
        {
            if (!session.IsActive)
                continue;

            hasActive = true;
            RenderSession(session, config);
            break;
        }

        if (!hasActive)
            SetZonesActive(false);
    }

    private NetworkCarriageRepairManager FindManager()
    {
        CarriageController carriage = CarriageController.Instance;
        return carriage != null ? carriage.GetComponent<NetworkCarriageRepairManager>() : null;
    }

    private CarriageConfig ResolveConfig(NetworkCarriageRepairManager manager = null)
    {
        manager ??= _manager != null ? _manager : FindManager();
        return manager != null ? manager.Config : CarriageConfigUtility.Resolve();
    }

    private void EnsurePoolRoot()
    {
        if (_poolRoot != null)
            return;

        Transform existing = transform.Find("RepairZonePool");
        if (existing != null)
        {
            _poolRoot = existing;
            return;
        }

        var pool = new GameObject("RepairZonePool");
        pool.transform.SetParent(transform, false);
        _poolRoot = pool.transform;
    }

    private void EnsurePool(CarriageConfig config)
    {
        if (config == null)
            return;

        while (_zoneObjects.Count < 2)
        {
            GameObject zone = CreateZone($"CarriageRepairZone_{_zoneObjects.Count}", config);
            zone.SetActive(false);
            _zoneObjects.Add(zone);
        }
    }

    private void RenderSession(CarriageRepairSession session, CarriageConfig config)
    {
        EnsurePool(config);
        float diameter = config.GetRepairZoneVisualDiameter();
        int sortingOrder = config.repairZoneSortingOrder;

        ActivateZone(_zoneObjects[0], session.ZoneA, config, diameter, sortingOrder, session.Progress);

        if (session.ZoneCount > 1 && _zoneObjects.Count > 1)
            ActivateZone(_zoneObjects[1], session.ZoneB, config, diameter, sortingOrder, session.Progress);
        else if (_zoneObjects.Count > 1)
            _zoneObjects[1].SetActive(false);
    }

    private static void ActivateZone(
        GameObject zone, Vector2 worldPosition, CarriageConfig config,
        float diameter, int sortingOrder, float progress)
    {
        zone.SetActive(true);
        zone.transform.position = new Vector3(worldPosition.x, worldPosition.y, -2f);

        SealZoneRingVisual ring = zone.GetComponent<SealZoneRingVisual>();
        if (ring == null)
            return;

        ring.Configure(
            config.repairZoneBackgroundColor,
            config.repairZoneFillColor,
            config.repairZoneOutlineColor,
            sortingOrder,
            diameter,
            config.repairZoneOutlineThickness,
            config.repairZoneShowInteriorFill);
        ring.SetFill(progress);
    }

    private GameObject CreateZone(string name, CarriageConfig config)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_poolRoot != null ? _poolRoot : transform, false);
        go.AddComponent<SealZoneRingVisual>();
        return go;
    }

    private void SetZonesActive(bool active)
    {
        for (int i = 0; i < _zoneObjects.Count; i++)
        {
            if (_zoneObjects[i] != null)
                _zoneObjects[i].SetActive(active);
        }
    }
}
