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
    [SerializeField] private Transform spawnPoint; // Arraste um objeto vazio do lado direito da tela
    [SerializeField] private float timeBetweenWaves = 5f;
    private WaveSettings _currentSettings;

    private int _enemiesAlive = 0;
    private int _currentWaveIndex = 0;
    
    //private bool _isSpawning = false;

    public event System.Action OnAllWavesCleared; // Avisa o NightManager que acabou

    public void Initialize(WaveSettings settings)
    {
        _currentSettings = settings;
        _currentWaveIndex = 0;
        _enemiesAlive = 0;
    }

    public void StartSpawning()
    {
        if (_currentSettings == null) return;
        StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        // Opcional: Destruir inimigos existentes se necessário ao perder
    }

    private IEnumerator SpawnRoutine()
    {
        while (_currentWaveIndex < _currentSettings.waves.Count)
        {
            WaveData wave = _currentSettings.waves[_currentWaveIndex];

            for (int i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave.enemyPrefab);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            _currentWaveIndex++;

            // Espera um pouco antes da próxima onda interna, se houver
            if (_currentWaveIndex < _currentSettings.waves.Count)
                yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // Incrementa contador
        _enemiesAlive++;

        // Assina evento de morte do inimigo para saber quando a noite acaba
        // ASSUMINDO que o inimigo tem um HealthComponent como o da casa
        if (enemy.TryGetComponent<HealthComponent>(out var health))
        {
            health.OnDied.AddListener(HandleEnemyDeath);
        }
    }

    private void HandleEnemyDeath()
    {
        _enemiesAlive--;

        // Se não estamos mais spawnando e não tem inimigos vivos...
        if (_currentWaveIndex >= _currentSettings.waves.Count && _enemiesAlive <= 0)
        {
            OnAllWavesCleared?.Invoke();
        }
    }
}