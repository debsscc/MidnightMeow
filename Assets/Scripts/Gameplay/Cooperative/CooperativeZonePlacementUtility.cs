using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Posiciona círculos cooperativos ao redor de um ponto âncora sem sobreposição.
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
        var positions = new Vector2[zoneCount];

        if (zoneCount == 1)
        {
            if (TryPlaceSingle(anchor, zoneRadius, minDistance, maxDistance, out Vector2 single))
                return new PlacementResult { Success = true, Positions = new[] { single } };

            return new PlacementResult { Success = false, Positions = System.Array.Empty<Vector2>() };
        }

        const int maxAttempts = 24;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (!TryPlaceSingle(anchor, zoneRadius, minDistance, maxDistance, out Vector2 first))
                continue;

            if (!TryPlaceSingle(anchor, zoneRadius, minDistance, maxDistance, out Vector2 second))
                continue;

            if (Vector2.Distance(first, second) < minSeparation)
                continue;

            positions[0] = first;
            positions[1] = second;
            return new PlacementResult { Success = true, Positions = positions };
        }

        if (TryPlaceSingle(anchor, zoneRadius, minDistance, maxDistance, out Vector2 fallback))
            return new PlacementResult { Success = true, Positions = new[] { fallback } };

        return new PlacementResult { Success = false, Positions = System.Array.Empty<Vector2>() };
    }

    private static bool TryPlaceSingle(
        Vector2 anchor,
        float zoneRadius,
        float minDistance,
        float maxDistance,
        out Vector2 position)
    {
        float minDist = Mathf.Max(0.1f, minDistance);
        float maxDist = Mathf.Max(minDist, maxDistance);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minDist, maxDist);
        position = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        return true;
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
