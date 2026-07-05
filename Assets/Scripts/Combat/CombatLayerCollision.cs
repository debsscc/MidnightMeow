using UnityEngine;

/// <summary>
/// Matriz de colisão 2D para combate: player, inimigos e projéteis não se empurram via física.
/// Dano e knockback continuam por triggers, raycasts e RPCs.
/// </summary>
public static class CombatLayerCollision
{
    private static bool _applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _applied = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => Apply();

    public static void Apply()
    {
        if (_applied)
            return;

        _applied = true;

        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        int projectile = LayerMask.NameToLayer("Projectile");
        int projectileEnemy = LayerMask.NameToLayer("ProjectileEnemy");
        int barrier = LayerMask.NameToLayer("Barrier");

        SetIgnore(player, enemy, true);
        SetIgnore(player, projectile, true);
        SetIgnore(player, projectileEnemy, true);
        SetIgnore(projectile, projectile, true);
        SetIgnore(projectile, enemy, false);
        SetIgnore(projectileEnemy, enemy, true);
        SetIgnore(projectileEnemy, projectile, true);

        if (barrier >= 0)
        {
            SetIgnore(barrier, player, true);
            SetIgnore(barrier, projectile, true);
            SetIgnore(barrier, enemy, false);
            SetIgnore(barrier, projectileEnemy, false);
        }
    }

    public static bool IsPlayerEnemyCollisionIgnored()
    {
        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        if (player < 0 || enemy < 0)
            return false;

        return Physics2D.GetIgnoreLayerCollision(player, enemy);
    }

    private static void SetIgnore(int layerA, int layerB, bool ignore)
    {
        if (layerA < 0 || layerB < 0)
            return;

        Physics2D.IgnoreLayerCollision(layerA, layerB, ignore);
    }
}
