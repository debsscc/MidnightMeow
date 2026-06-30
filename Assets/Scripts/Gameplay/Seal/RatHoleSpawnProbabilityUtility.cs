using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sorteia prefabs com base em pesos configurados no <see cref="RatHoleSpawnProfile"/>.
/// </summary>
public static class RatHoleSpawnProbabilityUtility
{
    public static GameObject PickWeightedPrefab(IReadOnlyList<RatHoleSpawnProfile.WeightedEnemyEntry> table)
    {
        if (table == null || table.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < table.Count; i++)
        {
            if (table[i].enemyPrefab != null && table[i].spawnWeight > 0f)
                totalWeight += table[i].spawnWeight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < table.Count; i++)
        {
            RatHoleSpawnProfile.WeightedEnemyEntry entry = table[i];
            if (entry.enemyPrefab == null || entry.spawnWeight <= 0f)
                continue;

            roll -= entry.spawnWeight;
            if (roll <= 0f)
                return entry.enemyPrefab;
        }

        for (int i = table.Count - 1; i >= 0; i--)
        {
            if (table[i].enemyPrefab != null)
                return table[i].enemyPrefab;
        }

        return null;
    }
}
