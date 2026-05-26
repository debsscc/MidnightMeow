using UnityEngine;

/// <summary>
/// Hit test de ataque melee em trapézio (base estreita no jogador, base larga no alcance).
/// </summary>
public static class MeleeHitUtility
{
    public static bool IsInsideTrapezoid(
        Vector2 origin,
        Vector2 forward,
        float depth,
        float nearHalfWidth,
        float farHalfWidth,
        Vector2 point)
    {
        if (depth <= 0.001f) return false;

        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);

        Vector2 local = point - origin;
        float along = Vector2.Dot(local, forward);

        if (along < 0f || along > depth)
            return false;

        float t = along / depth;
        float allowedHalfWidth = Mathf.Lerp(nearHalfWidth, farHalfWidth, t);
        float lateral = Mathf.Abs(Vector2.Dot(local, right));

        return lateral <= allowedHalfWidth;
    }

    public static Vector3[] GetTrapezoidWorldCorners(
        Vector2 origin,
        Vector2 forward,
        float depth,
        float nearHalfWidth,
        float farHalfWidth,
        float z = 0f)
    {
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);

        Vector2 nearLeft = origin - right * nearHalfWidth;
        Vector2 nearRight = origin + right * nearHalfWidth;
        Vector2 farCenter = origin + forward * depth;
        Vector2 farLeft = farCenter - right * farHalfWidth;
        Vector2 farRight = farCenter + right * farHalfWidth;

        return new[]
        {
            new Vector3(nearLeft.x, nearLeft.y, z),
            new Vector3(nearRight.x, nearRight.y, z),
            new Vector3(farRight.x, farRight.y, z),
            new Vector3(farLeft.x, farLeft.y, z)
        };
    }
}
