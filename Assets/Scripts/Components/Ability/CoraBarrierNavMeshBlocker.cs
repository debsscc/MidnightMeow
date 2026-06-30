using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Inimigos se movem via NavMeshAgent (não por colisão 2D). Empurra agentes que invadem a barreira sólida.
/// </summary>
[DisallowMultipleComponent]
public class CoraBarrierNavMeshBlocker : MonoBehaviour
{
    [SerializeField] private BoxCollider2D blockingCollider;
    [SerializeField] private LayerMask enemyLayers;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>(16);
    private ContactFilter2D _contactFilter;
    private CoraBarrier _barrier;

    private void Awake()
    {
        _barrier = GetComponent<CoraBarrier>();

        if (blockingCollider == null)
            blockingCollider = GetComponent<BoxCollider2D>();

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }

        _contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = enemyLayers,
            useTriggers = false
        };
    }

    private void FixedUpdate()
    {
        if (blockingCollider == null || !blockingCollider.enabled || !blockingCollider.gameObject.activeInHierarchy)
            return;

        _overlapResults.Clear();
        blockingCollider.Overlap(_contactFilter, _overlapResults);

        Bounds barrierBounds = blockingCollider.bounds;
        for (int i = 0; i < _overlapResults.Count; i++)
        {
            Collider2D enemyCollider = _overlapResults[i];
            if (enemyCollider == null)
                continue;

            if (_barrier != null)
                _barrier.TryApplyStun(enemyCollider.gameObject);

            NavMeshAgent agent = enemyCollider.GetComponentInParent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                continue;

            Vector2 push = ComputePushOut(enemyCollider.bounds.center, barrierBounds);
            if (push.sqrMagnitude <= 0.0001f)
                continue;

            Vector3 newPosition = enemyCollider.transform.position + (Vector3)push;
            agent.Warp(newPosition);
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    private static Vector2 ComputePushOut(Vector2 point, Bounds barrierBounds)
    {
        if (!barrierBounds.Contains(point))
            return Vector2.zero;

        float left = point.x - barrierBounds.min.x;
        float right = barrierBounds.max.x - point.x;
        float bottom = point.y - barrierBounds.min.y;
        float top = barrierBounds.max.y - point.y;

        float min = Mathf.Min(left, right, bottom, top);
        if (min <= 0f)
            return Vector2.zero;

        if (Mathf.Approximately(min, left))
            return Vector2.left * (left + 0.05f);
        if (Mathf.Approximately(min, right))
            return Vector2.right * (right + 0.05f);
        if (Mathf.Approximately(min, bottom))
            return Vector2.down * (bottom + 0.05f);

        return Vector2.up * (top + 0.05f);
    }
}
