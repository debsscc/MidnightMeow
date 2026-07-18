using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Posiciona labels world-space acima ou abaixo dos círculos cooperativos,
/// sem sobrepor a área circular.
/// </summary>
public static class CooperativeZoneLabelPlacementUtility
{
    private const float VerticalPadding = 0.28f;

    /// <summary>
    /// Coloca o centro do label fora do(s) círculo(s): preferência acima;
    /// abaixo quando a âncora da entidade está claramente acima do cluster.
    /// </summary>
    public static Vector3 ResolvePosition(
        IReadOnlyList<Vector2> zoneCenters,
        float zoneVisualRadius,
        Vector2 fallbackAnchor,
        Vector3 fallbackOffset,
        Vector2? entityAnchorForSideChoice = null)
    {
        if (zoneCenters == null || zoneCenters.Count == 0 || zoneVisualRadius <= 0.01f)
            return (Vector3)fallbackAnchor + fallbackOffset;

        float halfLabelHeight = GameplayUiFonts.WorldInteractionCanvasSize.y * 0.5f;
        float sumX = 0f;
        float top = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;

        for (int i = 0; i < zoneCenters.Count; i++)
        {
            Vector2 zone = zoneCenters[i];
            sumX += zone.x;
            top = Mathf.Max(top, zone.y + zoneVisualRadius);
            bottom = Mathf.Min(bottom, zone.y - zoneVisualRadius);
        }

        float x = sumX / zoneCenters.Count;
        bool placeAbove = true;
        if (entityAnchorForSideChoice.HasValue)
        {
            float midY = (top + bottom) * 0.5f;
            // Entidade acima do cluster → texto abaixo dos círculos (entre entidade e zonas).
            placeAbove = entityAnchorForSideChoice.Value.y <= midY;
        }

        float y = placeAbove
            ? top + halfLabelHeight + VerticalPadding
            : bottom - halfLabelHeight - VerticalPadding;

        return new Vector3(x, y, 0f);
    }

    public static void CollectSealZones(in RatHoleSealSession session, List<Vector2> into)
    {
        into.Clear();
        if (session.ZoneCount <= 0)
            return;

        into.Add(session.ZoneA);
        if (session.ZoneCount >= 2)
            into.Add(session.ZoneB);
    }
}
