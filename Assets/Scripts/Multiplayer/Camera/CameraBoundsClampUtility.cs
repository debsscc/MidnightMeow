using UnityEngine;

/// <summary>
/// Limita o centro de uma câmera ortográfica 2D a um <see cref="Collider2D"/> de bounds
/// (ex.: <see cref="CameraBoundsVolume"/>), considerando metade da altura/largura visível.
/// </summary>
public static class CameraBoundsClampUtility
{
    /// <summary>
    /// Retorna o centro clampado; se o viewport for maior que o bounds, fixa no centro do collider.
    /// </summary>
    public static Vector2 ClampOrthographicCenter(
        Vector2 center,
        Collider2D boundsShape,
        float orthographicSize,
        float aspect)
    {
        if (boundsShape == null || orthographicSize <= 0f || aspect <= 0f)
            return center;

        Bounds bounds = boundsShape.bounds;
        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * aspect;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        if (minX > maxX)
            center.x = bounds.center.x;
        else
            center.x = Mathf.Clamp(center.x, minX, maxX);

        if (minY > maxY)
            center.y = bounds.center.y;
        else
            center.y = Mathf.Clamp(center.y, minY, maxY);

        return center;
    }

    public static Vector3 ClampOrthographicPosition(
        Vector3 position,
        Collider2D boundsShape,
        float orthographicSize,
        float aspect)
    {
        Vector2 clamped = ClampOrthographicCenter(
            new Vector2(position.x, position.y),
            boundsShape,
            orthographicSize,
            aspect);

        position.x = clamped.x;
        position.y = clamped.y;
        return position;
    }
}
