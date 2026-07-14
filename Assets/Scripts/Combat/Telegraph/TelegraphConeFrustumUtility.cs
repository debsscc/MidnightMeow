///* ----------------------------------------------------------------
// ATUALIZADO EM: 14-07-2026
// DESCRIÇÃO: Matemática do telegraph em tronco de cone (trapézio 2D): raios, AABB, ponto-dentro e gizmos.
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>Utilitários geométricos para <see cref="TelegraphShapeType.ConeFrustum"/>.</summary>
public static class TelegraphConeFrustumUtility
{
    /// <summary>Resolve raio interno, externo e comprimento a partir do strike.</summary>
    public static void ResolveRadii(
        TelegraphStrikeDefinition strike,
        out float innerRadius,
        out float outerRadius,
        out float length)
    {
        length = Mathf.Max(0.1f, strike.size.y > 0.01f ? strike.size.y : 2f);
        innerRadius = Mathf.Max(0.05f, strike.coneInnerRadius > 0.01f ? strike.coneInnerRadius : Mathf.Max(0.05f, strike.size.x));

        if (strike.coneOuterRadius > 0.01f)
        {
            outerRadius = Mathf.Max(innerRadius, strike.coneOuterRadius);
            return;
        }

        float halfAngleRad = Mathf.Max(0f, strike.coneOpeningAngleDegrees) * Mathf.Deg2Rad;
        outerRadius = innerRadius + length * Mathf.Tan(halfAngleRad);
        outerRadius = Mathf.Max(innerRadius, outerRadius);
    }

    /// <summary>
    /// Centro da AABB do trapézio: origem do ataque + metade do comprimento na direção do strike
    /// (mesmo pivot usado por retângulos com <c>localOffset.y ≈ length/2</c>).
    /// </summary>
    public static Vector2 ComputeCenteredWorldPosition(Vector2 attackOrigin, float rotationDegrees, float length)
    {
        float rad = (rotationDegrees + 90f) * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        return attackOrigin + forward * (length * 0.5f);
    }

    public static Vector2 GetAabbSize(float innerRadius, float outerRadius, float length)
    {
        float width = Mathf.Max(innerRadius, outerRadius) * 2f;
        return new Vector2(Mathf.Max(0.1f, width), Mathf.Max(0.1f, length));
    }

    /// <summary>
    /// Testa se o ponto está dentro do trapézio centrado em <paramref name="worldCenter"/>,
    /// com +Y local apontando na ponta (mesma convenção do retângulo).
    /// </summary>
    public static bool ContainsPoint(
        Vector2 worldPoint,
        Vector2 worldCenter,
        float rotationDegrees,
        float innerRadius,
        float outerRadius,
        float length)
    {
        Vector2 local = Quaternion.Euler(0f, 0f, -rotationDegrees) * (worldPoint - worldCenter);
        float halfLen = length * 0.5f;
        if (local.y < -halfLen || local.y > halfLen)
            return false;

        float t = (local.y + halfLen) / Mathf.Max(0.0001f, length);
        float halfWidth = Mathf.Lerp(innerRadius, outerRadius, t);
        return Mathf.Abs(local.x) <= halfWidth;
    }

    public static void DrawGizmos(
        Vector2 worldCenter,
        float rotationDegrees,
        float innerRadius,
        float outerRadius,
        float length,
        Color color)
    {
        Vector2[] corners = BuildWorldCorners(worldCenter, rotationDegrees, innerRadius, outerRadius, length);
        Gizmos.color = color;
        for (int i = 0; i < corners.Length; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
    }

    public static Vector2[] BuildWorldCorners(
        Vector2 worldCenter,
        float rotationDegrees,
        float innerRadius,
        float outerRadius,
        float length)
    {
        float halfLen = length * 0.5f;
        Vector2[] local =
        {
            new Vector2(-innerRadius, -halfLen),
            new Vector2(innerRadius, -halfLen),
            new Vector2(outerRadius, halfLen),
            new Vector2(-outerRadius, halfLen)
        };

        var rot = Quaternion.Euler(0f, 0f, rotationDegrees);
        var world = new Vector2[4];
        for (int i = 0; i < 4; i++)
            world[i] = worldCenter + (Vector2)(rot * local[i]);
        return world;
    }
}
