using UnityEngine;

/// <summary>
/// Desenho compartilhado de gizmos para zonas de habilidade.
/// </summary>
public static class AbilityDebugGizmoUtility
{
    public static void DrawCircle(Vector2 center, float radius, Color fill, Color outline)
    {
        const int segments = 32;
        float z = center.y;
        var prev = center + Vector2.right * radius;

        Gizmos.color = outline;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            var next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(new Vector3(prev.x, prev.y, 0f), new Vector3(next.x, next.y, 0f));
            prev = next;
        }

        Gizmos.color = fill;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, 0f), radius * 0.15f);
    }

    public static void DrawOrientedRect(Vector2 origin, Vector2 forward, float depth, float halfWidth, Color fill, Color outline)
    {
        var corners = RectHitUtility.GetOrientedRectCorners(origin, forward, depth, halfWidth, 0f);
        Gizmos.color = fill;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        Gizmos.color = outline;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }

    public static void DrawDash(Vector2 origin, Vector2 direction, float distance, float width, Color fill, Color outline)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        DrawOrientedRect(origin, direction, distance, width * 0.5f, fill, outline);
    }

    public static void DrawCenteredOrientedRect(
        Vector2 center,
        Vector2 forward,
        float depth,
        float halfWidth,
        Color fill,
        Color outline)
    {
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        Vector2 origin = center - forward * (depth * 0.5f);
        DrawOrientedRect(origin, forward, depth, halfWidth, fill, outline);
    }
}
