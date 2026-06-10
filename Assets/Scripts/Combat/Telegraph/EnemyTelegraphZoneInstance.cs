using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Executa um strike: preenchimento visual + resolução (dano em área ou visual até a zona).</summary>
public class EnemyTelegraphZoneInstance : MonoBehaviour
{
    public event Action<EnemyTelegraphZoneInstance> OnResolved;

    private TelegraphStrikeDefinition _strike;
    private EnemyTelegraphVisualStyle _style;
    private GameObject _instigator;
    private Transform _attackOrigin;
    private Vector2 _travelSpawnPosition;
    private float _travelSpeedOverride = -1f;
    private bool _forceTravelVisual;
    private EnemyTelegraphZoneView _view;
    private bool _visualOnly;
    private bool _resolved;
    private Func<GameObject, Vector3, Quaternion, GameObject> _projectileSpawnDelegate;

    public bool IsResolved => _resolved;

    public void Initialize(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        GameObject instigator,
        Transform attackOrigin,
        bool visualOnly,
        Func<GameObject, Vector3, Quaternion, GameObject> projectileSpawnDelegate = null,
        Vector2 travelSpawnPosition = default,
        bool forceTravelVisual = false,
        float travelSpeedOverride = -1f)
    {
        _strike = strike;
        _style = style;
        _instigator = instigator;
        _attackOrigin = attackOrigin;
        _travelSpawnPosition = travelSpawnPosition;
        _forceTravelVisual = forceTravelVisual;
        _travelSpeedOverride = travelSpeedOverride;
        _visualOnly = visualOnly;
        _projectileSpawnDelegate = projectileSpawnDelegate;
        _resolved = false;

        _view = GetComponent<EnemyTelegraphZoneView>();
        if (_view == null)
            _view = gameObject.AddComponent<EnemyTelegraphZoneView>();

        _view.ApplyStyle(style, strike.shape, strike.fillMode);
        _view.SetWorldPose(worldPosition, rotationDegrees, strike.shape, strike.size);
        _view.SetFill(0f);

        var eventData = BuildEventData(worldPosition, rotationDegrees);
        EnemyTelegraphEvents.InvokeStarted(eventData);

        StartCoroutine(FillRoutine(eventData, worldPosition, rotationDegrees));
    }

    private IEnumerator FillRoutine(TelegraphEventData eventData, Vector2 worldPosition, float rotationDegrees)
    {
        float duration = Mathf.Max(0.05f, _strike.fillDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_view != null)
                _view.SetFill(elapsed / duration);
            yield return null;
        }

        if (_view != null)
            _view.SetFill(1f);

        EnemyTelegraphEvents.InvokeFillComplete(eventData);

        int hits = 0;
        bool usedTravelVisual = false;

        if (_visualOnly)
        {
            if (_strike.resolution == EnemyTelegraphResolution.ProjectileToZone
                && (_forceTravelVisual || GetTravelVisualPrefab() != null))
            {
                yield return PlayTravelVisualOnly(worldPosition, rotationDegrees);
                usedTravelVisual = true;
            }
        }
        else
        {
            switch (_strike.resolution)
            {
                case EnemyTelegraphResolution.AreaDamage:
                    hits = ResolveAreaDamage(worldPosition, rotationDegrees);
                    break;
                case EnemyTelegraphResolution.ProjectileToZone:
                    yield return ResolveProjectileToZone(worldPosition, rotationDegrees, result => hits = result);
                    usedTravelVisual = true;
                    break;
            }
        }

        var resolved = new TelegraphResolvedEventData(eventData, hits, usedTravelVisual);
        EnemyTelegraphEvents.InvokeResolved(resolved);

        _resolved = true;
        OnResolved?.Invoke(this);

        ScheduleDestroy(0.15f);
    }

    private int ResolveAreaDamage(Vector2 worldPosition, float rotationDegrees)
    {
        SpawnZoneEffect(worldPosition, rotationDegrees);
        return ApplyAreaDamage(worldPosition, rotationDegrees);
    }

    private IEnumerator PlayTravelVisualOnly(Vector2 worldPosition, float rotationDegrees)
    {
        var travelPrefab = GetTravelVisualPrefab();
        if (travelPrefab == null)
            yield break;

        Vector2 spawnPos = ResolveTravelSpawnPosition();
        Vector2 dir = worldPosition - spawnPos;
        var rotation = ProjectileAimUtility.RotationFromDirection(
            dir,
            ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);

        GameObject travelerGo = Instantiate(travelPrefab, spawnPos, rotation);
        if (travelerGo == null)
            yield break;

        DisableTravelProjectilePhysics(travelerGo);
        ProjectileAimUtility.ApplyRotation(
            travelerGo.transform,
            dir,
            ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);

        var traveler = travelerGo.GetComponent<TelegraphZoneTraveler>();
        if (traveler == null)
            traveler = travelerGo.AddComponent<TelegraphZoneTraveler>();

        traveler.Launch(worldPosition, ResolveTravelSpeed());

        while (!traveler.HasArrived)
            yield return null;

        DestroyTraveler(travelerGo, 0.5f);
    }

    private IEnumerator ResolveProjectileToZone(Vector2 worldPosition, float rotationDegrees, Action<int> onComplete)
    {
        var travelPrefab = GetTravelVisualPrefab();
        if (travelPrefab == null)
        {
            onComplete(ResolveAreaDamage(worldPosition, rotationDegrees));
            yield break;
        }

        Vector2 spawnPos = ResolveTravelSpawnPosition();
        Vector2 dir = worldPosition - spawnPos;
        var rotation = ProjectileAimUtility.RotationFromDirection(
            dir,
            ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);

        // Visual de voo é local no servidor; clientes reproduzem via ClientRpc (sem NGO preso).
        GameObject travelerGo = Instantiate(travelPrefab, spawnPos, rotation);

        if (travelerGo == null)
        {
            onComplete(ResolveAreaDamage(worldPosition, rotationDegrees));
            yield break;
        }

        DisableTravelProjectilePhysics(travelerGo);
        ProjectileAimUtility.ApplyRotation(
            travelerGo.transform,
            dir,
            ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);

        var traveler = travelerGo.GetComponent<TelegraphZoneTraveler>();
        if (traveler == null)
            traveler = travelerGo.AddComponent<TelegraphZoneTraveler>();

        traveler.Launch(worldPosition, ResolveTravelSpeed());

        while (!traveler.HasArrived)
            yield return null;

        DestroyTraveler(travelerGo, 0.5f);
        onComplete(ApplyAreaDamage(worldPosition, rotationDegrees));
    }

    private Vector2 ResolveTravelSpawnPosition()
    {
        if (_forceTravelVisual)
            return _travelSpawnPosition;

        if (_attackOrigin != null)
            return _attackOrigin.position;

        return _instigator != null ? (Vector2)_instigator.transform.position : Vector2.zero;
    }

    private float ResolveTravelSpeed()
    {
        if (_travelSpeedOverride > 0f)
            return _travelSpeedOverride;

        if (_strike.travelSpeed > 0f)
            return _strike.travelSpeed;

        if (_strike.projectileSpeedOverride > 0f)
            return _strike.projectileSpeedOverride;

        return 12f;
    }

    private static void DisableTravelProjectilePhysics(GameObject travelerGo)
    {
        if (travelerGo.TryGetComponent<EnemyProjectile>(out var projectile))
            projectile.enabled = false;

        if (travelerGo.TryGetComponent<NetworkEnemyProjectileController>(out var networkProjectile))
            networkProjectile.enabled = false;

        if (travelerGo.TryGetComponent<NetworkObject>(out var networkObject)
            && networkObject.IsSpawned
            && NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsServer)
        {
            networkObject.Despawn(true);
        }

        if (travelerGo.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        foreach (var col in travelerGo.GetComponents<Collider2D>())
            col.enabled = false;
    }

    private GameObject GetTravelVisualPrefab()
    {
        if (_strike.travelVisualPrefab != null)
            return _strike.travelVisualPrefab;

        if (_strike.projectilePrefab != null)
            return _strike.projectilePrefab;

        return GameplayPrefabCatalog.LoadCached()?.enemyTelegraphTravelPrefab;
    }

    private void SpawnZoneEffect(Vector2 worldPosition, float rotationDegrees)
    {
        if (_strike.effectPrefab == null) return;

        var rot = Quaternion.Euler(0f, 0f, rotationDegrees);
        Instantiate(_strike.effectPrefab, worldPosition, rot);
    }

    private int ApplyAreaDamage(Vector2 worldPosition, float rotationDegrees)
    {
        if (_strike.damage <= 0) return 0;

        var hits = new HashSet<Collider2D>();
        LayerMask mask = _strike.damageLayers.value == 0
            ? (LayerMask)(1 << LayerMask.NameToLayer("Player"))
            : _strike.damageLayers;

        Collider2D[] results = _strike.shape == TelegraphShapeType.Circle
            ? Physics2D.OverlapCircleAll(worldPosition, _strike.size.x, mask)
            : Physics2D.OverlapBoxAll(worldPosition, _strike.size, rotationDegrees, mask);

        int count = 0;
        foreach (var col in results)
        {
            if (col == null || hits.Contains(col)) continue;
            hits.Add(col);

            if (PlayerCombatUtility.TryApplyDamage(col, _strike.damage, _instigator))
                count++;
        }

        return count;
    }

    private TelegraphEventData BuildEventData(Vector2 worldPosition, float rotationDegrees)
    {
        return new TelegraphEventData(
            _instigator,
            worldPosition,
            rotationDegrees,
            _strike.shape,
            _strike.size,
            _strike.resolution);
    }

    private void ScheduleDestroy(float delay)
    {
        if (delay <= 0f)
            Destroy(gameObject);
        else
            Destroy(gameObject, delay);
    }

    private static void DestroyTraveler(GameObject travelerGo, float delay)
    {
        if (travelerGo == null) return;

        if (travelerGo.TryGetComponent<NetworkObject>(out var networkObject)
            && networkObject.IsSpawned
            && NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsServer)
        {
            networkObject.Despawn(true);
            return;
        }

        Destroy(travelerGo, delay);
    }
}
