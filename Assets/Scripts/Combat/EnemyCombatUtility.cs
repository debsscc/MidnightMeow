using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Utilitário para aplicar dano e CC em inimigos (offline e multiplayer).
/// </summary>
public static class EnemyCombatUtility
{
    public static void ApplyDamage(GameObject target, float damage, ulong instigatorClientId, GameObject instigator)
    {
        ApplyDamage(target, damage, instigatorClientId, instigator, DamageType.Generic);
    }

    public static void ApplyDamage(GameObject target, float damage, ulong instigatorClientId, GameObject instigator, DamageType damageType)
    {
        if (target == null || damage <= 0f) return;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
            networkEnemy.TakeDamageRpc(damage, instigatorClientId, damageType);
        else if (target.TryGetComponent<HealthComponent>(out var health))
            health.TakeDamage(damage, instigator, damageType);
    }

    public static void ApplyKnockback(GameObject target, Vector2 direction, float distance, float duration)
    {
        if (target == null || distance <= 0f) return;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
            networkEnemy.ApplyKnockbackRpc(direction, distance, duration);
        else if (target.TryGetComponent<KnockbackReceiver>(out var knockback))
            knockback.ApplyKnockback(direction, distance / Mathf.Max(0.01f, duration), duration);
    }

    public static void ApplySlow(GameObject target, float speedMultiplier, float duration)
    {
        if (target == null || duration <= 0f) return;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
            networkEnemy.ApplySlowRpc(speedMultiplier, duration);
        else
        {
            var slow = target.GetComponent<EnemySlowEffect>() ?? target.AddComponent<EnemySlowEffect>();
            slow.ApplySlow(speedMultiplier, duration);
        }
    }

    public static void ApplyStun(GameObject target, float duration)
    {
        if (target == null || duration <= 0f) return;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
            networkEnemy.ApplyStunRpc(duration);
        else if (target.TryGetComponent<EnemyHitStun>(out var stun))
            stun.ApplyStun(duration);
    }
}
