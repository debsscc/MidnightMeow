using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Posiciona círculos cooperativos ao redor de um ponto âncora, visíveis na câmera
/// e sem sobrepor colisões de parede.
/// </summary>
public static class CooperativeZonePlacementUtility
{
    private const int AngleCandidateCount = 16;
    private const int DistanceCandidateCount = 5;
    private static readonly Collider2D[] ObstacleHitBuffer = new Collider2D[1];

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
        return TryPlaceZones(
            anchor,
            zoneCount,
            zoneRadius,
            minDistance,
            maxDistance,
            minSeparation,
            ResolveDefaultObstacleMask());
    }

    public static PlacementResult TryPlaceZones(
        Vector2 anchor,
        int zoneCount,
        float zoneRadius,
        float minDistance,
        float maxDistance,
        float minSeparation,
        LayerMask obstacleMask)
    {
        zoneCount = Mathf.Clamp(zoneCount, 1, 4);
        Vector2 biasDir = ResolveCameraBiasDirection(anchor);
        float clampedMin = Mathf.Min(minDistance, maxDistance);
        float clampedMax = Mathf.Max(minDistance, maxDistance);

        // Preferência original (câmera + 70% do intervalo), depois varre ângulos/distâncias.
        for (int angleIndex = 0; angleIndex < AngleCandidateCount; angleIndex++)
        {
            float angleOffsetDeg = angleIndex * (360f / AngleCandidateCount);
            Vector2 dir = Rotate(biasDir, angleOffsetDeg);

            for (int distanceIndex = 0; distanceIndex < DistanceCandidateCount; distanceIndex++)
            {
                float t = distanceIndex == 0
                    ? 0.7f
                    : (distanceIndex - 1) / Mathf.Max(1f, DistanceCandidateCount - 2);
                float distance = Mathf.Lerp(clampedMin, clampedMax, Mathf.Clamp01(t));

                if (!TryBuildCandidate(
                        anchor,
                        zoneCount,
                        zoneRadius,
                        minSeparation,
                        dir,
                        distance,
                        out Vector2[] positions))
                    continue;

                if (!AreZonesClearOfObstacles(positions, zoneRadius, obstacleMask))
                    continue;

                return new PlacementResult { Success = true, Positions = positions };
            }
        }

        return new PlacementResult { Success = false, Positions = null };
    }

    public static LayerMask ResolveDefaultObstacleMask() =>
        LayerMask.GetMask("Wall", "DashableWall");

    public static bool AreZonesClearOfObstacles(
        IReadOnlyList<Vector2> positions,
        float zoneRadius,
        LayerMask obstacleMask)
    {
        if (positions == null || positions.Count == 0)
            return false;

        // Máscara vazia = sem obstáculo a considerar (ex.: testes EditMode).
        if (obstacleMask.value == 0)
            return true;

        float radius = Mathf.Max(0.05f, zoneRadius);
        var filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            useDepth = false
        };
        filter.SetLayerMask(obstacleMask);

        for (int i = 0; i < positions.Count; i++)
        {
            if (Physics2D.OverlapCircle(positions[i], radius, filter, ObstacleHitBuffer) > 0)
                return false;
        }

        return true;
    }

    private static bool TryBuildCandidate(
        Vector2 anchor,
        int zoneCount,
        float zoneRadius,
        float minSeparation,
        Vector2 dir,
        float distance,
        out Vector2[] positions)
    {
        if (zoneCount == 1)
        {
            positions = new[] { anchor + dir * distance };
            return true;
        }

        if (zoneCount == 2)
        {
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);
            float lateral = Mathf.Max(minSeparation * 0.5f, zoneRadius * 0.9f);
            Vector2 center = anchor + dir * distance;
            Vector2 first = center + perpendicular * lateral;
            Vector2 second = center - perpendicular * lateral;

            if (Vector2.Distance(first, second) < minSeparation)
            {
                first = center + perpendicular * (minSeparation * 0.5f);
                second = center - perpendicular * (minSeparation * 0.5f);
            }

            positions = new[] { first, second };
            return true;
        }

        // 3–4 zonas: leque na direção escolhida com espaçamento mínimo.
        positions = new Vector2[zoneCount];
        float arcDegrees = zoneCount == 3 ? 90f : 120f;
        float startAngle = -arcDegrees * 0.5f;
        float step = zoneCount > 1 ? arcDegrees / (zoneCount - 1) : 0f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < zoneCount; i++)
        {
            float angle = (baseAngle + startAngle + step * i) * Mathf.Deg2Rad;
            Vector2 fanDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            positions[i] = anchor + fanDir * distance;
        }

        EnforceMinSeparation(positions, minSeparation);
        return true;
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }

    private static void EnforceMinSeparation(Vector2[] positions, float minSeparation)
    {
        if (positions == null || positions.Length < 2 || minSeparation <= 0f)
            return;

        for (int iter = 0; iter < 4; iter++)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = i + 1; j < positions.Length; j++)
                {
                    Vector2 delta = positions[j] - positions[i];
                    float dist = delta.magnitude;
                    if (dist >= minSeparation || dist < 0.0001f)
                        continue;

                    Vector2 push = (delta / dist) * ((minSeparation - dist) * 0.5f);
                    positions[i] -= push;
                    positions[j] += push;
                }
            }
        }
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
