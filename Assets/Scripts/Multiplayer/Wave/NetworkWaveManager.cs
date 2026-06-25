/// <summary>
/// Gerencia spawn de inimigos na rede: ondas legadas, spawn por buracos ou boss único.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWaveManager : NetworkBehaviour
{
    public enum SpawnMode
    {
        Waves,
        RatHoles,
        SingleBoss
    }

    public static NetworkWaveManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private WaveSettings waveSettings;
    [SerializeField] private MultiplayerConfig multiplayerConfig;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Prefab de Ciência para drops de rede")]
    [SerializeField] private GameObject networkCienciaPrefab;

    private int _currentWaveIndex;
    private int _enemiesAlive;
    private int _totalEnemiesInCurrentWave;
    private int _totalKilledInPhase;
    private bool _isSpawning;
    private bool _hasStarted;
    private SpawnMode _spawnMode = SpawnMode.Waves;
    private float _holeSpawnInterval = 4f;
    private int _maxEnemiesAlive = 35;
    private float _firstSpawnDelay = 3f;
    private readonly List<GameObject> _holeSpawnPrefabs = new List<GameObject>();

    private readonly List<NetworkObject> _spawnedEnemies = new List<NetworkObject>();

    public int EnemiesAlive => _enemiesAlive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (GetComponent<NetworkRatHoleSealManager>() == null)
            gameObject.AddComponent<NetworkRatHoleSealManager>();
    }

    private void Start()
    {
        NetworkRatHoleSealManager sealManager = GetComponent<NetworkRatHoleSealManager>();
        if (sealManager != null)
            RatHoleSealZoneVisual.EnsureAttached(sealManager);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        MultiplayerGameManager.OnGameStateChanged += HandleGameStateChanged;

        if (waveSettings == null)
            Debug.LogError("[NetworkWaveManager] WaveSettings não atribuído no Inspector!");
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("[NetworkWaveManager] Nenhum SpawnPoint atribuído no Inspector!");

        TryStartSpawningIfGameplayAlreadyActive();
    }

    private void TryStartSpawningIfGameplayAlreadyActive()
    {
        if (!IsServer) return;

        MultiplayerGameManager gm = MultiplayerGameManager.Instance;
        if (gm != null && gm.CurrentState == GameState.Playing)
            TryStartSpawning();
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
            TryStartSpawning();
    }

    [Rpc(SendTo.Server)]
    public void StartWavesRpc() => TryStartSpawning();

    private void TryStartSpawning()
    {
        if (!IsServer || _hasStarted) return;
        _hasStarted = true;

        switch (_spawnMode)
        {
            case SpawnMode.RatHoles:
                Debug.Log("[NetworkWaveManager] Iniciando spawn por buracos.");
                StartCoroutine(HoleSpawnLoopRoutine());
                break;
            case SpawnMode.SingleBoss:
                Debug.Log("[NetworkWaveManager] Spawnando boss.");
                StartCoroutine(SpawnBossRoutine());
                break;
            default:
                Debug.Log("[NetworkWaveManager] Iniciando ondas.");
                StartCoroutine(FirstWaveDelayRoutine());
                break;
        }
    }

    private IEnumerator FirstWaveDelayRoutine()
    {
        float delay = waveSettings != null ? waveSettings.firstWaveDelay : _firstSpawnDelay;
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator HoleSpawnLoopRoutine()
    {
        yield return new WaitForSeconds(_firstSpawnDelay);

        while (true)
        {
            if (_enemiesAlive < _maxEnemiesAlive && _holeSpawnPrefabs.Count > 0)
            {
                GameObject prefab = _holeSpawnPrefabs[Random.Range(0, _holeSpawnPrefabs.Count)];
                if (prefab != null)
                    SpawnNetworkEnemy(prefab);
            }

            BroadcastObjectiveStatusClientRpc(_enemiesAlive);
            yield return new WaitForSeconds(_holeSpawnInterval);
        }
    }

    private IEnumerator SpawnBossRoutine()
    {
        yield return new WaitForSeconds(_firstSpawnDelay);

        GameObject bossPrefab = ResolveBossPrefab();
        if (bossPrefab != null)
            SpawnNetworkEnemy(bossPrefab, useHoleSelection: false);

        BroadcastObjectiveStatusClientRpc(_enemiesAlive);
    }

    private GameObject ResolveBossPrefab()
    {
        if (_holeSpawnPrefabs.Count > 0)
            return _holeSpawnPrefabs[0];

        if (waveSettings == null || waveSettings.waves == null || waveSettings.waves.Count == 0)
            return null;

        WaveData wave = waveSettings.waves[0];
        if (wave.enemies == null || wave.enemies.Count == 0)
            return null;

        return wave.enemies[0].enemyPrefab;
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

        List<GameObject> enemyPool = new List<GameObject>();
        foreach (EnemySpawnData enemyData in wave.enemies)
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
    }

    private void SpawnNetworkEnemy(GameObject prefab, bool useHoleSelection = true)
    {
        Vector3 spawnPosition;
        if (useHoleSelection)
        {
            if (!RatHoleSpawnSelectionUtility.TryPickSpawnPoint(spawnPoints, out _, out spawnPosition))
            {
                Debug.LogWarning("[NetworkWaveManager] Nenhum spawn point ativo disponível.");
                return;
            }
        }
        else if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null)
        {
            spawnPosition = spawnPoints[0].position;
        }
        else
        {
            spawnPosition = Vector3.zero;
        }

        GameObject enemyObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[NetworkWaveManager] Prefab '{prefab.name}' não tem NetworkObject.");
            Destroy(enemyObj);
            return;
        }

        netObj.Spawn(true);
        _spawnedEnemies.Add(netObj);
        _enemiesAlive++;

        if (_spawnMode == SpawnMode.Waves && waveSettings != null)
        {
            BroadcastWaveStatusClientRpc(
                Mathf.Min(_currentWaveIndex + 1, waveSettings.waves.Count),
                waveSettings.waves.Count,
                _enemiesAlive,
                _totalKilledInPhase);
        }
        else
        {
            BroadcastObjectiveStatusClientRpc(_enemiesAlive);
        }

        if (enemyObj.TryGetComponent<HealthComponent>(out HealthComponent health))
            health.OnDied.AddListener(() => HandleEnemyDeath(netObj));

        if (enemyObj.TryGetComponent<EnemyDropHandler>(out EnemyDropHandler dropHandler))
            dropHandler.SpawnDelegate = SpawnNetworkCiencia;
    }

    private void HandleEnemyDeath(NetworkObject enemyNetObj)
    {
        if (!_spawnedEnemies.Contains(enemyNetObj))
            return;

        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
        _totalKilledInPhase++;
        _spawnedEnemies.Remove(enemyNetObj);

        if (enemyNetObj != null && enemyNetObj.GetComponent<BossEnemyMarker>() != null)
            PhaseObjectiveManager.Instance?.NotifyBossDefeated();

        if (_spawnMode == SpawnMode.Waves && waveSettings != null)
        {
            BroadcastWaveStatusClientRpc(
                Mathf.Min(_currentWaveIndex + 1, waveSettings.waves.Count),
                waveSettings.waves.Count,
                _enemiesAlive,
                _totalKilledInPhase);

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
                AllWavesClearedClientRpc();
                GameEvents.InvokeNightEnded();
            }
        }
        else
        {
            BroadcastObjectiveStatusClientRpc(_enemiesAlive);
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
    private void BroadcastObjectiveStatusClientRpc(int enemiesAlive)
    {
        PhaseObjectiveStatusUtility.BroadcastCurrentStatus(enemiesAlive);
    }

    [ClientRpc]
    private void AllWavesClearedClientRpc()
    {
        Debug.Log("[NetworkWaveManager] ClientRpc: todas as ondas eliminadas.");
    }

    public void StopSpawning()
    {
        if (!IsServer)
            return;

        StopAllCoroutines();
        _isSpawning = false;
    }

    [Rpc(SendTo.Server)]
    public void RequestStopSpawningRpc() => StopSpawning();

    public void ConfigureWaveSettings(WaveSettings settings)
    {
        if (settings != null)
            waveSettings = settings;
    }

    public void ConfigureSpawnPoints(Transform[] points)
    {
        if (points != null && points.Length > 0)
            spawnPoints = points;
    }

    public void ConfigurePhaseEntry(PhaseWaveSettingsCatalog.PhaseEntry entry)
    {
        if (entry == null)
            return;

        if (entry.waveSettings != null)
            waveSettings = entry.waveSettings;

        _holeSpawnPrefabs.Clear();
        CollectHoleSpawnPrefabs(waveSettings);
        _holeSpawnInterval = Mathf.Max(0.5f, entry.holeSpawnInterval);
        _maxEnemiesAlive = Mathf.Max(1, entry.maxEnemiesAlive);
        _firstSpawnDelay = Mathf.Max(0f, entry.firstSpawnDelay);

        if (entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss)
            _spawnMode = SpawnMode.SingleBoss;
        else if (!entry.useWaveSpawning && entry.useHoleSpawning)
            _spawnMode = SpawnMode.RatHoles;
        else if (entry.useWaveSpawning)
            _spawnMode = SpawnMode.Waves;
        else
            _spawnMode = SpawnMode.RatHoles;
    }

    private void CollectHoleSpawnPrefabs(WaveSettings settings)
    {
        if (settings == null || settings.waves == null || settings.waves.Count == 0)
            return;

        WaveData firstWave = settings.waves[0];
        if (firstWave.enemies == null)
            return;

        for (int i = 0; i < firstWave.enemies.Count; i++)
        {
            GameObject prefab = firstWave.enemies[i].enemyPrefab;
            if (prefab != null && !_holeSpawnPrefabs.Contains(prefab))
                _holeSpawnPrefabs.Add(prefab);
        }
    }
}
