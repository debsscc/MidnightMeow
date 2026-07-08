using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Desenha áreas circulares de reviver (transplante de <see cref="RatHoleSealZoneVisual"/>).
/// </summary>
[DisallowMultipleComponent]
public class DownedReviveZoneVisualHost : MonoBehaviour
{
    public static DownedReviveZoneVisualHost Instance { get; private set; }

    private readonly Dictionary<ulong, List<GameObject>> _zoneObjects = new Dictionary<ulong, List<GameObject>>();
    private NetworkDownedReviveManager _manager;
    private Transform _poolRoot;
    private bool _subscribed;

    public static DownedReviveZoneVisualHost EnsureAttached(NetworkDownedReviveManager manager)
    {
        if (manager == null)
            return Instance;

        DownedReviveZoneVisualHost existing = manager.GetComponentInChildren<DownedReviveZoneVisualHost>(true);
        if (existing != null)
        {
            existing.Bind(manager);
            return existing;
        }

        var host = new GameObject("DownedReviveZoneVisuals");
        host.transform.SetParent(manager.transform, false);
        var visual = host.AddComponent<DownedReviveZoneVisualHost>();
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

    private void OnEnable()
    {
        RefreshAllZones();
    }

    private void LateUpdate() => RefreshFromSessions();

    public void RefreshFromSessions() => RefreshAllZones();

    public void HideSession(ulong downedClientId)
    {
        if (_zoneObjects.TryGetValue(downedClientId, out List<GameObject> zones))
            SetZonesActive(zones, false);
    }

    public void Bind(NetworkDownedReviveManager manager)
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

    public void ShowSession(DownedReviveSession session)
    {
        if (!session.IsActive)
            return;

        DownedPlayerConfig activeConfig = ResolveConfig();
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

    private void HandleSessionsChanged(NetworkListEvent<DownedReviveSession> _) => RefreshFromSessions();

    private void RefreshAllZones()
    {
        NetworkDownedReviveManager manager = _manager != null ? _manager : NetworkDownedReviveManager.Instance;
        DownedPlayerConfig activeConfig = ResolveConfig(manager);

        if (manager == null || activeConfig == null || !manager.IsSpawned)
            return;

        var active = new HashSet<ulong>();
        foreach (DownedReviveSession session in manager.Sessions)
        {
            if (!session.IsActive)
                continue;

            active.Add(session.DownedClientId);
            RenderSession(session, activeConfig);
        }

        foreach (KeyValuePair<ulong, List<GameObject>> pair in _zoneObjects)
        {
            if (active.Contains(pair.Key))
                continue;

            SetZonesActive(pair.Value, false);
        }
    }

    private DownedPlayerConfig ResolveConfig(NetworkDownedReviveManager manager = null)
    {
        manager ??= _manager != null ? _manager : NetworkDownedReviveManager.Instance;
        return manager != null ? manager.Config : null;
    }

    private void EnsurePoolRoot()
    {
        if (_poolRoot != null)
            return;

        Transform existing = transform.Find("ReviveZonePool");
        if (existing != null)
        {
            _poolRoot = existing;
            return;
        }

        var pool = new GameObject("ReviveZonePool");
        pool.transform.SetParent(transform, false);
        _poolRoot = pool.transform;
    }

    private void EnsurePoolForPlayer(ulong downedClientId, DownedPlayerConfig activeConfig)
    {
        if (activeConfig == null)
            return;

        if (_zoneObjects.TryGetValue(downedClientId, out List<GameObject> existing) && existing.Count >= 2)
            return;

        if (!_zoneObjects.TryGetValue(downedClientId, out existing))
        {
            existing = new List<GameObject>(2);
            _zoneObjects[downedClientId] = existing;
        }

        while (existing.Count < 2)
        {
            GameObject zone = CreateZone($"ReviveZone_{downedClientId}_{existing.Count}", activeConfig);
            zone.SetActive(false);
            existing.Add(zone);
        }
    }

    private void RenderSession(DownedReviveSession session, DownedPlayerConfig activeConfig)
    {
        EnsurePoolForPlayer(session.DownedClientId, activeConfig);
        List<GameObject> zones = _zoneObjects[session.DownedClientId];
        float diameter = activeConfig.GetReviveZoneVisualDiameter();
        int sortingOrder = activeConfig.reviveZoneSortingOrder;

        ActivateZone(zones[0], session.ZoneA, activeConfig, diameter, sortingOrder, session.Progress);

        if (session.ZoneCount > 1 && zones.Count > 1)
            ActivateZone(zones[1], session.ZoneB, activeConfig, diameter, sortingOrder, session.Progress);
        else if (zones.Count > 1)
            zones[1].SetActive(false);
    }

    private static void ActivateZone(
        GameObject zone,
        Vector2 worldPosition,
        DownedPlayerConfig activeConfig,
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
            activeConfig.reviveZoneBackgroundColor,
            activeConfig.reviveZoneFillColor,
            activeConfig.reviveZoneOutlineColor,
            sortingOrder,
            diameter,
            activeConfig.reviveZoneOutlineThickness,
            activeConfig.reviveZoneShowInteriorFill);
        ring.SetFill(progress);
    }

    private GameObject CreateZone(string name, DownedPlayerConfig activeConfig)
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
