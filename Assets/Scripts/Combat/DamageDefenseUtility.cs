using UnityEngine;

/// <summary>
/// Aplica modificadores de defesa por tipo de dano (data-driven via <see cref="EnemyStats"/>).
/// </summary>
public static class DamageDefenseUtility
{
    /// <summary>
    /// Reduz dano ranged conforme <see cref="EnemyStats.rangedDefense"/> (0–1 = percentual).
    /// </summary>
    public static float ApplyDefense(float amount, DamageType damageType, EnemyStats stats)
    {
        if (amount <= 0f || stats == null || damageType != DamageType.Ranged)
            return amount;

        float reduction = Mathf.Clamp01(stats.rangedDefense);
        return amount * (1f - reduction);
    }

    public static EnemyStats ResolveEnemyStats(GameObject target)
    {
        if (target == null)
            return null;

        if (target.TryGetComponent<EnemyMovement>(out var movement))
            return movement.Stats;

        if (target.TryGetComponent<EnemyTargetFinder>(out var finder))
            return finder.Stats;

        return null;
    }
}
