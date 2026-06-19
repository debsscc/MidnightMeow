using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escolhe pontos de spawn ativos, respeitando buracos selados.
/// </summary>
public static class RatHoleSpawnSelectionUtility
{
    public static bool TryPickSpawnPoint(Transform[] spawnPoints, out Transform selected, out Vector3 spawnPosition)
    {
        selected = null;
        spawnPosition = Vector3.zero;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        var candidates = new List<Transform>(spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null)
                continue;

            RatHoleSpawnPoint hole = point.GetComponent<RatHoleSpawnPoint>();
            if (hole != null && !hole.CanSpawn)
                continue;

            candidates.Add(point);
        }

        if (candidates.Count == 0)
            return false;

        selected = candidates[Random.Range(0, candidates.Count)];
        RatHoleSpawnPoint selectedHole = selected.GetComponent<RatHoleSpawnPoint>();
        spawnPosition = selectedHole != null ? selectedHole.GetSpawnPosition() : selected.position;
        return true;
    }
}
