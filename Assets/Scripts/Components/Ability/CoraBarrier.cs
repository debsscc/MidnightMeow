using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Obstáculo da Cora: bloqueia pathfinding e física (inimigos); jogadores e projéteis do jogador passam via Layer Collision Matrix.
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class CoraBarrier : MonoBehaviour
{
    [SerializeField] private BoxCollider2D blockingCollider;
    [SerializeField] private LayerMask enemyLayers;

    private AbilityTierData _tierData;
    private ulong _ownerClientId;
    private NavMeshObstacle _obstacle;
    private Coroutine _lifetimeRoutine;
    private NetworkCoraBarrier _networkBarrier;
    private readonly HashSet<int> _stunnedEnemyIds = new();

    private void Awake()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _networkBarrier = GetComponent<NetworkCoraBarrier>();

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
        ConfigureNavMeshObstacle(tierData);
        EnsureNavMeshBlocker();
        ScheduleLifetime(tierData.effectDuration);
    }

    public void TryApplyStun(GameObject enemyObject)
    {
        if (_tierData.stunDuration <= 0f || enemyObject == null)
            return;

        if (enemyLayers.value != 0 && ((1 << enemyObject.layer) & enemyLayers.value) == 0)
            return;

        var enemy = enemyObject.GetComponentInParent<HealthComponent>();
        if (enemy == null || !enemy.IsAlive)
            return;

        int id = enemy.GetInstanceID();
        if (!_stunnedEnemyIds.Add(id))
            return;

        EnemyCombatUtility.ApplyStun(enemy.gameObject, _tierData.stunDuration);
    }

    private void EnsureNavMeshBlocker()
    {
        if (blockingCollider == null)
            return;

        if (GetComponent<CoraBarrierNavMeshBlocker>() != null)
            return;

        gameObject.AddComponent<CoraBarrierNavMeshBlocker>();
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
        blockingCollider.excludeLayers = 0;

        int barrierLayer = LayerMask.NameToLayer("Barrier");
        if (barrierLayer >= 0)
            gameObject.layer = barrierLayer;
    }

    private void ConfigureNavMeshObstacle(AbilityTierData tierData)
    {
        if (_obstacle == null)
            return;

        _obstacle.shape = NavMeshObstacleShape.Box;
        _obstacle.carving = true;
        _obstacle.carveOnlyStationary = false;
        _obstacle.enabled = true;
        _obstacle.size = new Vector3(
            tierData.range * 2f,
            tierData.areaWidth > 0f ? tierData.areaWidth : 0.5f,
            1f);
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
