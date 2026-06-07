using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Obstáculo da Cora: bloqueia pathfinding, atordoa inimigos ao toque. Projéteis passam (apenas trigger + NavMeshObstacle).
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class CoraBarrier : MonoBehaviour
{
    [SerializeField] private CircleCollider2D enemyStunTrigger;
    [SerializeField] private LayerMask enemyLayers;

    private AbilityTierData _tierData;
    private ulong _ownerClientId;
    private NavMeshObstacle _obstacle;
    private Coroutine _lifetimeRoutine;

    private void Awake()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
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

        if (enemyStunTrigger != null)
            enemyStunTrigger.radius = tierData.range;

        if (_obstacle != null)
        {
            _obstacle.shape = NavMeshObstacleShape.Box;
            _obstacle.size = new Vector3(tierData.range * 2f, tierData.areaWidth > 0f ? tierData.areaWidth : 0.5f, 1f);
            _obstacle.carving = true;
        }

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);

        if (tierData.effectDuration > 0f)
            _lifetimeRoutine = StartCoroutine(LifetimeRoutine(tierData.effectDuration));
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
}
