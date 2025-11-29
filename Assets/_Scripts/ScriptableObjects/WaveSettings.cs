///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Define as configurações das ondas de inimigos para cada noite do jogo.
// ---------------------------------------------------------------- */

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct WaveData
{
    public GameObject enemyPrefab; // O prefab do inimigo
    public int count;              // Quantos inimigos nesta onda
    public float spawnInterval;    // Tempo entre spawns
}

[CreateAssetMenu(fileName = "NewWaveSettings", menuName = "Game/Wave Settings")]
public class WaveSettings : ScriptableObject
{
    [Header("Night Configuration")]
    public List<WaveData> waves; // Lista de ondas para uma noite específica
}