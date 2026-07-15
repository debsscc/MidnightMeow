using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host visual das áreas de conserto (mesmo pipeline <see cref="SealZoneRingVisual"/> do selamento).
/// </summary>
[DisallowMultipleComponent]
public class CarriageRepairZoneVisualHost : MonoBehaviour
{
    public static CarriageRepairZoneVisualHost Instance { get; private set; }

    private readonly List<GameObject> _zoneObjects = new List<GameObject>(CarriageRepairSession.MaxZones);
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

    public void ShowSession(CarriageRepairSession session, Vector2 zoneC = default, Vector2 zoneD = default)
    {
        if (!session.IsActive)
            return;

        if (session.ZoneCount >= 3)
            session.ZoneC = zoneC;
        if (session.ZoneCount >= 4)
            session.ZoneD = zoneD;

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

    private void EnsurePool(CarriageConfig config, int zoneCount)
    {
        if (config == null)
            return;

        int needed = Mathf.Clamp(zoneCount, 1, CarriageRepairSession.MaxZones);
        while (_zoneObjects.Count < needed)
        {
            GameObject zone = CreateZone($"CarriageRepairZone_{_zoneObjects.Count}", config);
            zone.SetActive(false);
            _zoneObjects.Add(zone);
        }
    }

    private void RenderSession(CarriageRepairSession session, CarriageConfig config)
    {
        EnsurePool(config, session.ZoneCount);
        float diameter = config.GetRepairZoneVisualDiameter();
        int sortingOrder = config.repairZoneSortingOrder;

        var zones = new List<Vector2>(CarriageRepairSession.MaxZones);
        session.CollectZones(zones);

        for (int i = 0; i < _zoneObjects.Count; i++)
        {
            if (i < zones.Count)
                ActivateZone(_zoneObjects[i], zones[i], config, diameter, sortingOrder, session.Progress);
            else
                _zoneObjects[i].SetActive(false);
        }
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
