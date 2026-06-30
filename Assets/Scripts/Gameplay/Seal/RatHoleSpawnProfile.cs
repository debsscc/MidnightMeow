using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuração data-driven de spawn por buraco: tabela de ratos com peso e intervalo entre spawns.
/// Pode ser reutilizado em vários <see cref="RatHoleSpawnPoint"/>.
/// </summary>
[CreateAssetMenu(fileName = "RatHoleSpawnProfile", menuName = "MidnightMeow/Gameplay/Rat Hole Spawn Profile")]
public class RatHoleSpawnProfile : ScriptableObject
{
    [System.Serializable]
    public struct WeightedEnemyEntry
    {
        [Tooltip("Prefab do inimigo a spawnar.")]
        public GameObject enemyPrefab;

        [Tooltip("Peso relativo na tabela (ex.: 0.7 vs 0.3).")]
        [Min(0f)]
        public float spawnWeight;
    }

    [Header("Tabela de spawn")]
    [Tooltip("Lista de ratos e chances relativas de spawn.")]
    public List<WeightedEnemyEntry> enemyTable = new();

    [Header("Intervalo entre spawns")]
    [Min(0.1f)]
    public float minSpawnTime = 2f;

    [Min(0.1f)]
    public float maxSpawnTime = 6f;

    public float RollSpawnDelay()
    {
        float min = Mathf.Min(minSpawnTime, maxSpawnTime);
        float max = Mathf.Max(minSpawnTime, maxSpawnTime);
        return Random.Range(min, max);
    }

    public GameObject RollEnemyPrefab()
    {
        return RatHoleSpawnProbabilityUtility.PickWeightedPrefab(enemyTable);
    }

    public bool IsValid()
    {
        if (enemyTable == null || enemyTable.Count == 0)
            return false;

        for (int i = 0; i < enemyTable.Count; i++)
        {
            if (enemyTable[i].enemyPrefab != null && enemyTable[i].spawnWeight > 0f)
                return true;
        }

        return false;
    }
}
