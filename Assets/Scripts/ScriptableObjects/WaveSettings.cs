///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Define as configura��es das ondas de inimigos para cada noite do jogo.
// ---------------------------------------------------------------- */

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct EnemySpawnData
{
    public GameObject enemyPrefab;
    public int count;
}

[System.Serializable]
public struct WaveData
{
    public List<EnemySpawnData> enemies;
    public float spawnInterval;
}

[CreateAssetMenu(fileName = "NewWaveSettings", menuName = "Game/Wave Settings")]
public class WaveSettings : ScriptableObject
{
    [Header("Wave Configuration")]
    public List<WaveData> waves;

    [Header("Wave Progression")]
    [Range(0f, 1f)]
    [Tooltip("Porcentagem de inimigos que devem ser derrotados para iniciar pr�xima wave")]
    public float percentageToNextWave = 0.8f;

    [Header("First Wave Delay")]
    [Tooltip("Tempo em segundos antes de iniciar a primeira wave")]
    public float firstWaveDelay = 3f;
}