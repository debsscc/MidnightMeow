using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seleção de alvos para sub-projéteis de respingo (Cora).
/// </summary>
public static class ProjectileSplashUtility
{
    public static void CollectSplashTargets(
        Vector2 origin,
        float range,
        int splashCount,
        bool prioritizeDifferentEnemies,
        LayerMask enemyLayers,
        Transform primaryHitRoot,
        List<Transform> results)
    {
        results.Clear();
        if (splashCount <= 0 || range <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyLayers);
        if (hits == null || hits.Length == 0)
            return;

        var candidates = new List<(Transform root, float distSq)>(hits.Length);
        var seen = new HashSet<int>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Transform root = ResolveEnemyRoot(hit);
            if (root == null)
                continue;

            int id = root.GetInstanceID();
            if (!seen.Add(id))
                continue;

            if (root.TryGetComponent<HealthComponent>(out var health) && health.IsDead)
                continue;

            if (root.TryGetComponent<NetworkEnemyController>(out var netEnemy) && netEnemy.IsDeadOnNetwork)
                continue;

            float distSq = ((Vector2)root.position - origin).sqrMagnitude;
            candidates.Add((root, distSq));
        }

        if (candidates.Count == 0)
            return;

        candidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        var ordered = new List<Transform>(candidates.Count);
        if (prioritizeDifferentEnemies && primaryHitRoot != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].root != primaryHitRoot)
                    ordered.Add(candidates[i].root);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].root == primaryHitRoot)
                    ordered.Add(candidates[i].root);
            }
        }
        else
        {
            for (int i = 0; i < candidates.Count; i++)
                ordered.Add(candidates[i].root);
        }

        if (ordered.Count == 0)
            return;

        for (int i = 0; i < splashCount; i++)
        {
            if (prioritizeDifferentEnemies)
                results.Add(ordered[i % ordered.Count]);
            else
                results.Add(ordered[0]);
        }
    }

    private static Transform ResolveEnemyRoot(Collider2D hit)
    {
        var networkEnemy = hit.GetComponentInParent<NetworkEnemyController>();
        if (networkEnemy != null)
            return networkEnemy.transform;

        var health = hit.GetComponentInParent<HealthComponent>();
        if (health == null)
            return null;

        if (health.GetComponentInParent<NetworkPlayerHealth>() != null)
            return null;

        return health.transform;
    }
}
