/// <summary>
/// NetworkWaveManager.cs
/// NetworkBehaviour server-autoritativo que substitui NightManager no contexto multiplayer.
/// Apenas o servidor/host executa a lógica de spawn de ondas, instanciando inimigos como
/// NetworkObjects via NetworkObject.Spawn() para replicação automática a todos os clientes.
/// IMPORTANTE NO EDITOR: Este componente DEVE estar num GameObject que também tenha NetworkObject.
/// Pode ser iniciado via evento estático do MultiplayerGameManager OU via StartWavesRpc()
/// diretamente do lobby (StartGameButton → ambos chamados no mesmo fluxo).
/// SRP: exclusivamente gerencia spawning e progresso de ondas na rede.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWaveManager : NetworkBehaviour
{
    public static NetworkWaveManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private WaveSettings waveSettings;
    [SerializeField] private MultiplayerConfig multiplayerConfig;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Prefab de Ciência para drops de rede")]
    [SerializeField] private GameObject networkCienciaPrefab;

    private int _currentWaveIndex = 0;
    private int _enemiesAlive = 0;
    private int _totalEnemiesInCurrentWave = 0;
    private int _totalKilledInPhase = 0;
    private bool _isSpawning = false;
    private bool _hasStarted = false;

    private readonly List<NetworkObject> _spawnedEnemies = new List<NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        MultiplayerGameManager.OnGameStateChanged += HandleGameStateChanged;

        Debug.Log("[NetworkWaveManager] Pronto. Aguardando início do jogo.");

        if (waveSettings == null)
            Debug.LogError("[NetworkWaveManager] WaveSettings não atribuído no Inspector!");
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("[NetworkWaveManager] Nenhum SpawnPoint atribuído no Inspector!");

        TryStartWavesIfGameplayAlreadyActive();
    }

    /// <summary>
    /// Se o GameManager já estiver em Playing quando este objeto spawna (ordem de spawn NGO).
    /// </summary>
    private void TryStartWavesIfGameplayAlreadyActive()
    {
        if (!IsServer) return;

        var gm = MultiplayerGameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.Playing)
            TryStartWaves();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        MultiplayerGameManager.OnGameStateChanged -= HandleGameStateChanged;
        StopAllCoroutines();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
            TryStartWaves();
    }

    /// <summary>
    /// Rpc de fallback que pode ser chamado diretamente pelo lobby
    /// sem depender da cadeia de eventos do GameState.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void StartWavesRpc()
    {
        TryStartWaves();
    }

    private void TryStartWaves()
    {
        if (!IsServer || _hasStarted) return;
        _hasStarted = true;
        Debug.Log("[NetworkWaveManager] Iniciando ondas!");
        StartCoroutine(FirstWaveDelayRoutine());
    }

    private IEnumerator FirstWaveDelayRoutine()
    {
        float delay = waveSettings != null ? waveSettings.firstWaveDelay : 3f;
        Debug.Log($"[NetworkWaveManager] Primeira onda em {delay}s...");
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (waveSettings == null || _currentWaveIndex >= waveSettings.waves.Count)
        {
            Debug.LogWarning("[NetworkWaveManager] Sem waves para spawnar.");
            yield break;
        }

        _isSpawning = true;
        WaveData wave = waveSettings.waves[_currentWaveIndex];
        Debug.Log($"[NetworkWaveManager] Iniciando Wave {_currentWaveIndex + 1}/{waveSettings.waves.Count}");

        List<GameObject> enemyPool = new List<GameObject>();
        foreach (var enemyData in wave.enemies)
        {
            for (int i = 0; i < enemyData.count; i++)
                enemyPool.Add(enemyData.enemyPrefab);
        }

        _totalEnemiesInCurrentWave = enemyPool.Count;

        while (enemyPool.Count > 0)
        {
            int idx = Random.Range(0, enemyPool.Count);
            GameObject prefab = enemyPool[idx];
            enemyPool.RemoveAt(idx);

            if (prefab != null)
                SpawnNetworkEnemy(prefab);

            yield return new WaitForSeconds(wave.spawnInterval);
        }

        _isSpawning = false;
        Debug.Log($"[NetworkWaveManager] Wave {_currentWaveIndex + 1} totalmente spawnada. Inimigos vivos: {_enemiesAlive}");
    }

    private void SpawnNetworkEnemy(GameObject prefab)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[NetworkWaveManager] Prefab '{prefab.name}' não tem NetworkObject! Adicione NetworkObject + NetworkEnemyController.");
            Destroy(enemyObj);
            return;
        }

        netObj.Spawn(true);
        _spawnedEnemies.Add(netObj);
        _enemiesAlive++;

        Debug.Log($"[NetworkWaveManager] Inimigo spawnado: {enemyObj.name} netId={netObj.NetworkObjectId} IsSpawned={netObj.IsSpawned}");

        BroadcastWaveStatusClientRpc(
            Mathf.Min(_currentWaveIndex + 1, waveSettings.waves.Count),
            waveSettings.waves.Count,
            _enemiesAlive,
            _totalKilledInPhase
        );

        if (enemyObj.TryGetComponent<HealthComponent>(out var health))
            health.OnDied.AddListener(() => HandleEnemyDeath(netObj));

        if (enemyObj.TryGetComponent<EnemyDropHandler>(out var dropHandler))
            dropHandler.SpawnDelegate = SpawnNetworkCiencia;
    }

    private void HandleEnemyDeath(NetworkObject enemyNetObj)
    {
        if (!_spawnedEnemies.Contains(enemyNetObj))
            return;

        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
        _totalKilledInPhase++;
        _spawnedEnemies.Remove(enemyNetObj);

        BroadcastWaveStatusClientRpc(
            Mathf.Min(_currentWaveIndex + 1, waveSettings.waves.Count),
            waveSettings.waves.Count,
            _enemiesAlive,
            _totalKilledInPhase
        );

        if (!_isSpawning && _currentWaveIndex < waveSettings.waves.Count)
        {
            float pct = _totalEnemiesInCurrentWave > 0
                ? 1f - ((float)_enemiesAlive / _totalEnemiesInCurrentWave)
                : 1f;

            if (pct >= waveSettings.percentageToNextWave)
            {
                _currentWaveIndex++;
                if (_currentWaveIndex < waveSettings.waves.Count)
                    StartCoroutine(SpawnWaveRoutine());
            }
        }

        if (_currentWaveIndex >= waveSettings.waves.Count && _enemiesAlive <= 0)
        {
            Debug.Log("[NetworkWaveManager] Todas as ondas eliminadas! Vitória.");
            AllWavesClearedClientRpc();
            GameEvents.InvokeNightEnded();
        }
    }

    private GameObject SpawnNetworkCiencia(GameObject prefab, Vector3 position, int amount)
    {
        GameObject toSpawn = networkCienciaPrefab != null ? networkCienciaPrefab : prefab;
        GameObject cienciaObj = Instantiate(toSpawn, position, Quaternion.identity);

        if (cienciaObj.TryGetComponent<Ciencia>(out Ciencia ciencia))
            ciencia.SetValue(amount);

        NetworkObject netObj = cienciaObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[NetworkWaveManager] Prefab de Ciência não tem NetworkObject!");
            Destroy(cienciaObj);
            return null;
        }

        netObj.Spawn(true);
        return cienciaObj;
    }

    [ClientRpc]
    private void BroadcastWaveStatusClientRpc(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        GameEvents.InvokeWaveStatusChanged(currentWave, totalWaves, enemiesRemaining, totalKilled);
    }

    [ClientRpc]
    private void AllWavesClearedClientRpc()
    {
        Debug.Log("[NetworkWaveManager] ClientRpc: todas as ondas eliminadas.");
    }
}
