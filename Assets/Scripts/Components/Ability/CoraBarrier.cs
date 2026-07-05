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

    public static void GetBarrierDimensions(AbilityTierData tierData, out float length, out float thickness)
    {
        length = Mathf.Max(0.2f, tierData.range * 2f);
        thickness = tierData.areaWidth > 0f ? tierData.areaWidth : 0.5f;
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

        GetBarrierDimensions(tierData, out float length, out float thickness);
        SyncBarrierVisualAndCollider(length, thickness);

        blockingCollider.isTrigger = false;
        blockingCollider.excludeLayers = 0;

        int barrierLayer = LayerMask.NameToLayer("Barrier");
        if (barrierLayer >= 0)
            gameObject.layer = barrierLayer;
    }

    private void SyncBarrierVisualAndCollider(float worldLength, float worldThickness)
    {
        transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Vector2 spriteSize = Vector2.one;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            spriteSize = spriteRenderer.sprite.bounds.size;

        float scaleX = worldLength / Mathf.Max(0.01f, spriteSize.x);
        float scaleY = worldThickness / Mathf.Max(0.01f, spriteSize.y);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        if (blockingCollider != null)
        {
            blockingCollider.size = spriteSize;
            blockingCollider.offset = Vector2.zero;
        }
    }

    private void ConfigureNavMeshObstacle(AbilityTierData tierData)
    {
        if (_obstacle == null)
            return;

        GetBarrierDimensions(tierData, out float length, out float thickness);

        _obstacle.shape = NavMeshObstacleShape.Box;
        _obstacle.carving = true;
        _obstacle.carveOnlyStationary = false;
        _obstacle.enabled = true;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Vector2 spriteSize = Vector2.one;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            spriteSize = spriteRenderer.sprite.bounds.size;

        _obstacle.size = new Vector3(spriteSize.x, spriteSize.y, 1f);
        _obstacle.center = Vector3.zero;
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D collider = blockingCollider != null ? blockingCollider : GetComponent<BoxCollider2D>();
        if (collider == null)
            return;

        Bounds bounds = collider.bounds;
        Gizmos.color = new Color(0.2f, 0.95f, 0.45f, 0.35f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(0.5f, 1f, 0.65f, 0.95f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        NavMeshObstacle obstacle = _obstacle != null ? _obstacle : GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            return;

        Vector3 worldCenter = transform.TransformPoint(obstacle.center);
        Vector3 worldSize = Vector3.Scale(obstacle.size, transform.lossyScale);
        Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(worldCenter, worldSize);
    }
#endif
}
