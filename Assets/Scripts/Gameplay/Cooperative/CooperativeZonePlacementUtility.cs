using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Posiciona círculos cooperativos ao redor de um ponto âncora, visíveis na câmera.
/// </summary>
public static class CooperativeZonePlacementUtility
{
    public struct PlacementResult
    {
        public bool Success;
        public Vector2[] Positions;
    }

    public static PlacementResult TryPlaceZones(
        Vector2 anchor,
        int zoneCount,
        float zoneRadius,
        float minDistance,
        float maxDistance,
        float minSeparation)
    {
        zoneCount = Mathf.Clamp(zoneCount, 1, 2);
        Vector2 biasDir = ResolveCameraBiasDirection(anchor);
        float distance = Mathf.Clamp(Mathf.Lerp(minDistance, maxDistance, 0.7f), minDistance, maxDistance);

        if (zoneCount == 1)
        {
            Vector2 single = anchor + biasDir * distance;
            return new PlacementResult { Success = true, Positions = new[] { single } };
        }

        Vector2 perpendicular = new Vector2(-biasDir.y, biasDir.x);
        float lateral = Mathf.Max(minSeparation * 0.5f, zoneRadius * 0.9f);
        Vector2 center = anchor + biasDir * distance;
        Vector2 first = center + perpendicular * lateral;
        Vector2 second = center - perpendicular * lateral;

        if (Vector2.Distance(first, second) < minSeparation)
        {
            first = center + perpendicular * minSeparation;
            second = center - perpendicular * minSeparation;
        }

        return new PlacementResult { Success = true, Positions = new[] { first, second } };
    }

    private static Vector2 ResolveCameraBiasDirection(Vector2 anchor)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector2.up;

        Vector2 toCamera = (Vector2)cam.transform.position - anchor;
        if (toCamera.sqrMagnitude < 0.0001f)
            return Vector2.up;

        return toCamera.normalized;
    }

    public static bool IsInsideZone(Vector2 worldPosition, Vector2 zoneCenter, float radius) =>
        Vector2.Distance(worldPosition, zoneCenter) <= radius;

    public static int CountPlayersInZones(
        IReadOnlyList<Vector2> zoneCenters,
        float radius,
        bool requireDistinctZones = false)
    {
        if (zoneCenters == null || zoneCenters.Count == 0)
            return 0;

        var players = Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
        int occupiedZones = 0;

        for (int z = 0; z < zoneCenters.Count; z++)
        {
            bool zoneHasPlayer = false;
            for (int p = 0; p < players.Length; p++)
            {
                NetworkPlayerHealth player = players[p];
                if (player == null || !player.IsSpawned || !player.CanFight)
                    continue;

                if (!IsInsideZone(player.transform.position, zoneCenters[z], radius))
                    continue;

                zoneHasPlayer = true;
                if (!requireDistinctZones)
                    return Mathf.Max(1, occupiedZones + 1);
            }

            if (zoneHasPlayer)
                occupiedZones++;
        }

        return occupiedZones;
    }
}
