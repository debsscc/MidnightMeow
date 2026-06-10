using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Obstáculo da Cora: bloqueia pathfinding e física (inimigos/projéteis inimigos); projéteis do jogador passam.
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class CoraBarrier : MonoBehaviour
{
    [SerializeField] private CircleCollider2D enemyStunTrigger;
    [SerializeField] private BoxCollider2D blockingCollider;
    [SerializeField] private LayerMask enemyLayers;

    private AbilityTierData _tierData;
    private ulong _ownerClientId;
    private NavMeshObstacle _obstacle;
    private Coroutine _lifetimeRoutine;
    private NetworkCoraBarrier _networkBarrier;

    private void Awake()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _networkBarrier = GetComponent<NetworkCoraBarrier>();
        if (enemyStunTrigger == null)
            enemyStunTrigger = GetComponent<CircleCollider2D>();

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }
    }

    public void Initialize(AbilityTierData tierData, ulong ownerClientId)
    {
        _tierData = tierData;
        _ownerClientId = ownerClientId;

        ConfigureBlockingCollider(tierData);
        ConfigureStunTrigger(tierData);
        ConfigureNavMeshObstacle(tierData);
        ScheduleLifetime(tierData.effectDuration);
    }

    private void ConfigureBlockingCollider(AbilityTierData tierData)
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<BoxCollider2D>();

        if (blockingCollider == null)
            blockingCollider = gameObject.AddComponent<BoxCollider2D>();

        float width = tierData.range * 2f;
        float height = tierData.areaWidth > 0f ? tierData.areaWidth : 0.5f;

        blockingCollider.isTrigger = false;
        blockingCollider.size = new Vector2(width, height);
        blockingCollider.offset = Vector2.zero;

        int structureLayer = LayerMask.NameToLayer("Structure");
        if (structureLayer >= 0)
            gameObject.layer = structureLayer;

        int playerProjectileLayer = LayerMask.NameToLayer("Projectile");
        if (playerProjectileLayer >= 0)
            blockingCollider.excludeLayers = 1 << playerProjectileLayer;
    }

    private void ConfigureStunTrigger(AbilityTierData tierData)
    {
        if (enemyStunTrigger == null)
            return;

        enemyStunTrigger.isTrigger = true;
        enemyStunTrigger.radius = tierData.range;
    }

    private void ConfigureNavMeshObstacle(AbilityTierData tierData)
    {
        if (_obstacle == null)
            return;

        _obstacle.shape = NavMeshObstacleShape.Box;
        _obstacle.size = new Vector3(
            tierData.range * 2f,
            tierData.areaWidth > 0f ? tierData.areaWidth : 0.5f,
            1f);
        _obstacle.carving = true;
        _obstacle.carveOnlyStationary = false;
    }

    private void ScheduleLifetime(float duration)
    {
        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);

        if (duration <= 0f)
            return;

        if (_networkBarrier != null && _networkBarrier.IsSpawned && _networkBarrier.IsServer)
        {
            _networkBarrier.ServerScheduleDespawn(duration);
            return;
        }

        _lifetimeRoutine = StartCoroutine(LifetimeRoutine(duration));
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyLayers.value != 0 && ((1 << other.gameObject.layer) & enemyLayers.value) == 0)
            return;

        var enemy = other.GetComponentInParent<HealthComponent>();
        if (enemy == null || !enemy.IsAlive) return;

        EnemyCombatUtility.ApplyStun(enemy.gameObject, _tierData.stunDuration);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == null)
            return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("ProjectileEnemy")
            && collision.collider.TryGetComponent<EnemyProjectile>(out var projectile))
        {
            projectile.TriggerHitAndDestroy();
        }
    }
}
