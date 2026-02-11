///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gera ondas de inimigos com base nas configurações definidas em WaveSettings.
// ---------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform[] spawnPoints;
    private WaveSettings _currentSettings;

    private int _enemiesAlive = 0;
    private int _currentWaveIndex = 0;
    private int _totalEnemiesInCurrentWave = 0;
    private int _totalKilledInPhase = 0;
    private List<GameObject> _currentWaveEnemyPool = new List<GameObject>();
    private bool _isSpawning = false;

    public event System.Action OnAllWavesCleared;

    public void Initialize(WaveSettings settings)
    {
        _currentSettings = settings;
        _currentWaveIndex = 0;
        _enemiesAlive = 0;
        _totalKilledInPhase = 0;
    }

    public void StartSpawning()
    {
        if (_currentSettings == null) return;
        StartCoroutine(FirstWaveDelayRoutine());
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        _isSpawning = false;
    }

    private IEnumerator FirstWaveDelayRoutine()
    {
        yield return new WaitForSeconds(_currentSettings.firstWaveDelay);
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (_currentWaveIndex >= _currentSettings.waves.Count)
        {
            yield break;
        }

        _isSpawning = true;
        WaveData wave = _currentSettings.waves[_currentWaveIndex];

        _currentWaveEnemyPool.Clear();
        foreach (var enemyData in wave.enemies)
        {
            for (int i = 0; i < enemyData.count; i++)
            {
                _currentWaveEnemyPool.Add(enemyData.enemyPrefab);
            }
        }

        _totalEnemiesInCurrentWave = _currentWaveEnemyPool.Count;

        while (_currentWaveEnemyPool.Count > 0)
        {
            int randomIndex = Random.Range(0, _currentWaveEnemyPool.Count);
            GameObject enemyPrefab = _currentWaveEnemyPool[randomIndex];
            _currentWaveEnemyPool.RemoveAt(randomIndex);

            SpawnEnemy(enemyPrefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        _isSpawning = false;
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Nenhum spawn point configurado no WaveGenerator!");
            return;
        }

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(prefab, randomSpawnPoint.position, Quaternion.identity);

        _enemiesAlive++;
        UpdateWaveStatus();

        if (enemy.TryGetComponent<HealthComponent>(out var health))
        {
            health.OnDied.AddListener(HandleEnemyDeath);
        }
    }

    private void HandleEnemyDeath()
    {
        _enemiesAlive--;
        _totalKilledInPhase++;
        UpdateWaveStatus();

        if (!_isSpawning && _currentWaveIndex < _currentSettings.waves.Count)
        {
            float percentageCleared = 1f - ((float)_enemiesAlive / _totalEnemiesInCurrentWave);

            if (percentageCleared >= _currentSettings.percentageToNextWave)
            {
                _currentWaveIndex++;

                if (_currentWaveIndex < _currentSettings.waves.Count)
                {
                    StartCoroutine(SpawnWaveRoutine());
                }
            }
        }

        if (_currentWaveIndex >= _currentSettings.waves.Count && _enemiesAlive <= 0)
        {
            OnAllWavesCleared?.Invoke();
        }
    }

    private void UpdateWaveStatus()
    {
        int currentWaveNumber = Mathf.Min(_currentWaveIndex + 1, _currentSettings.waves.Count);
        GameEvents.InvokeWaveStatusChanged(
            currentWaveNumber,
            _currentSettings.waves.Count,
            _enemiesAlive,
            _totalKilledInPhase
        );
    }
}