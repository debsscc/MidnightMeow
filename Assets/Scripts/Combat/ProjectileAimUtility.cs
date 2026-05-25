using UnityEngine;

/// <summary>
/// Rotação de projéteis 2D alinhada à direção do voo.
/// </summary>
public static class ProjectileAimUtility
{
    /// <summary>Sprite com frente no +Y (projétil do jogador / Cora).</summary>
    public const float PlayerForwardOffsetDegrees = -90f;

    /// <summary>Sprite com frente no -X (PNG do rato: cabeça arredondada à esquerda).</summary>
    public const float EnemyRatProjectileForwardOffsetDegrees = -180f;

    public static Quaternion RotationFromDirection(Vector2 direction, float forwardOffsetDegrees = PlayerForwardOffsetDegrees)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;
        else
            direction = direction.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + forwardOffsetDegrees;
        return Quaternion.Euler(0f, 0f, angle);
    }

    public static void ApplyRotation(Transform transform, Vector2 direction, float forwardOffsetDegrees = PlayerForwardOffsetDegrees)
    {
        if (transform == null) return;
        transform.rotation = RotationFromDirection(direction, forwardOffsetDegrees);
    }
}
