using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Desenha áreas circulares de selamento. Vive como filho de <see cref="NetworkRatHoleSealManager"/>
/// para aparecer na hierarquia em _GameLoop/SealZoneVisuals e persistir enquanto a sessão estiver ativa.
/// </summary>
[DisallowMultipleComponent]
public class RatHoleSealZoneVisual : MonoBehaviour
{
    public static RatHoleSealZoneVisual Instance { get; private set; }

    [SerializeField] private RatHoleSealConfig config;

    private readonly Dictionary<ushort, List<GameObject>> _zoneObjects = new Dictionary<ushort, List<GameObject>>();
    private NetworkRatHoleSealManager _manager;
    private Transform _poolRoot;
    private bool _subscribed;

    public static RatHoleSealZoneVisual EnsureAttached(NetworkRatHoleSealManager manager)
    {
        if (manager == null)
            return Instance;

        RatHoleSealZoneVisual existing = manager.GetComponentInChildren<RatHoleSealZoneVisual>(true);
        if (existing != null)
        {
            existing.Bind(manager);
            return existing;
        }

        var host = new GameObject("SealZoneVisuals");
        host.transform.SetParent(manager.transform, false);
        var visual = host.AddComponent<RatHoleSealZoneVisual>();
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

        if (config == null)
            config = Resources.Load<RatHoleSealConfig>("RatHoleSealConfig");

        EnsurePoolRoot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Unsubscribe();
    }

    private void OnEnable()
    {
        PrewarmAllHolePools();
        RefreshAllZones();
    }

    private void LateUpdate() => RefreshAllZones();

    public void Bind(NetworkRatHoleSealManager manager)
    {
        if (manager == null)
            return;

        if (_manager != manager)
        {
            Unsubscribe();
            _manager = manager;
            Subscribe();
        }

        PrewarmAllHolePools();
        RefreshAllZones();
    }

    /// <summary>Força exibição imediata (host + ClientRpc ao iniciar selamento).</summary>
    public void ShowSession(RatHoleSealSession session)
    {
        if (!session.IsActive)
            return;

        RatHoleSealConfig activeConfig = ResolveConfig();
        if (activeConfig == null)
            return;

        RenderSession(session, activeConfig);
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

    private void HandleSessionsChanged(NetworkListEvent<RatHoleSealSession> changeEvent) => RefreshAllZones();

    private void RefreshAllZones()
    {
        NetworkRatHoleSealManager manager = _manager != null ? _manager : NetworkRatHoleSealManager.Instance;
        RatHoleSealConfig activeConfig = ResolveConfig(manager);

        if (manager == null || activeConfig == null)
            return;

        if (!manager.IsSpawned)
            return;

        var active = new HashSet<ushort>();
        foreach (RatHoleSealSession session in manager.Sessions)
        {
            if (!session.IsActive)
                continue;

            active.Add(session.HoleId);
            RenderSession(session, activeConfig);
        }

        foreach (KeyValuePair<ushort, List<GameObject>> pair in _zoneObjects)
        {
            if (active.Contains(pair.Key))
                continue;

            SetZonesActive(pair.Value, false);
        }
    }

    private RatHoleSealConfig ResolveConfig(NetworkRatHoleSealManager manager = null)
    {
        manager ??= _manager != null ? _manager : NetworkRatHoleSealManager.Instance;
        if (manager != null && manager.Config != null)
            return manager.Config;

        return config;
    }

    private void PrewarmAllHolePools()
    {
        EnsurePoolRoot();

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null)
                continue;

            EnsurePoolForHole(hole.HoleId, ResolveConfig());
        }
    }

    private void EnsurePoolRoot()
    {
        if (_poolRoot != null)
            return;

        Transform existing = transform.Find("SealZonePool");
        if (existing != null)
        {
            _poolRoot = existing;
            return;
        }

        var pool = new GameObject("SealZonePool");
        pool.transform.SetParent(transform, false);
        _poolRoot = pool.transform;
    }

    private void EnsurePoolForHole(ushort holeId, RatHoleSealConfig activeConfig)
    {
        if (activeConfig == null)
            return;

        if (_zoneObjects.TryGetValue(holeId, out List<GameObject> existing) && existing.Count >= 2)
            return;

        if (!_zoneObjects.TryGetValue(holeId, out existing))
        {
            existing = new List<GameObject>(2);
            _zoneObjects[holeId] = existing;
        }

        while (existing.Count < 2)
        {
            GameObject zone = CreateZone($"SealZone_{holeId}_{existing.Count}", activeConfig);
            zone.SetActive(false);
            existing.Add(zone);
        }
    }

    private void RenderSession(RatHoleSealSession session, RatHoleSealConfig activeConfig)
    {
        EnsurePoolForHole(session.HoleId, activeConfig);
        List<GameObject> zones = _zoneObjects[session.HoleId];
        float diameter = activeConfig.GetZoneVisualDiameter();
        int sortingOrder = activeConfig.zoneSortingOrder;

        ActivateZone(zones[0], session.ZoneA, activeConfig, diameter, sortingOrder, session.Progress);

        if (session.ZoneCount > 1 && zones.Count > 1)
            ActivateZone(zones[1], session.ZoneB, activeConfig, diameter, sortingOrder, session.Progress);
        else if (zones.Count > 1)
            zones[1].SetActive(false);
    }

    private static void ActivateZone(
        GameObject zone,
        Vector2 worldPosition,
        RatHoleSealConfig activeConfig,
        float diameter,
        int sortingOrder,
        float progress)
    {
        zone.SetActive(true);
        zone.transform.position = new Vector3(worldPosition.x, worldPosition.y, -2f);

        SealZoneRingVisual ring = zone.GetComponent<SealZoneRingVisual>();
        if (ring == null)
            return;

        ring.Configure(
            activeConfig.zoneBackgroundColor,
            activeConfig.zoneFillColor,
            activeConfig.zoneOutlineColor,
            sortingOrder,
            diameter,
            activeConfig.zoneOutlineThickness,
            activeConfig.zoneShowInteriorFill);
        ring.SetFill(progress);
    }

    private GameObject CreateZone(string name, RatHoleSealConfig activeConfig)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_poolRoot != null ? _poolRoot : transform, false);
        go.AddComponent<SealZoneRingVisual>();
        return go;
    }

    private static void SetZonesActive(List<GameObject> zones, bool active)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] != null)
                zones[i].SetActive(active);
        }
    }
}
