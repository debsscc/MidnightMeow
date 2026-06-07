using UnityEngine;

/// <summary>
/// Hit test retangular orientado (Investida da Nix).
/// </summary>
public static class RectHitUtility
{
    public static bool IsInsideOrientedRect(
        Vector2 origin,
        Vector2 forward,
        float depth,
        float halfWidth,
        Vector2 point)
    {
        if (depth <= 0.001f || halfWidth <= 0f) return false;

        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);

        Vector2 local = point - origin;
        float along = Vector2.Dot(local, forward);
        if (along < 0f || along > depth) return false;

        float lateral = Mathf.Abs(Vector2.Dot(local, right));
        return lateral <= halfWidth;
    }

    public static Vector3[] GetOrientedRectCorners(
        Vector2 origin,
        Vector2 forward,
        float depth,
        float halfWidth,
        float z)
    {
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);

        Vector2 nearLeft = origin - right * halfWidth;
        Vector2 nearRight = origin + right * halfWidth;
        Vector2 farLeft = origin + forward * depth - right * halfWidth;
        Vector2 farRight = origin + forward * depth + right * halfWidth;

        return new[]
        {
            new Vector3(nearLeft.x, nearLeft.y, z),
            new Vector3(farLeft.x, farLeft.y, z),
            new Vector3(farRight.x, farRight.y, z),
            new Vector3(nearRight.x, nearRight.y, z)
        };
    }
}
