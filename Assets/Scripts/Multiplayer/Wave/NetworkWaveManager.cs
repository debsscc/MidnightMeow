/// <summary>
/// Gerencia spawn de inimigos na rede: ondas legadas, spawn por buracos ou boss único.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private int _maxEnemiesAlive = 35;
    private float _firstSpawnDelay = 3f;
    private RatHoleSpawnOrchestrator _holeOrchestrator;

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

        string sceneName = SceneManager.GetActiveScene().name;
        if (GameplaySceneBootstrap.IsGameplayScene(sceneName))
            PhaseGameplayContentInstaller.ApplyPhaseContent(sceneName);

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
        if (newState == GameState.Paused)
        {
            if (_holeOrchestrator != null)
                _holeOrchestrator.SetSpawnPaused(true);
            return;
        }

        if (newState == GameState.Playing)
        {
            if (_holeOrchestrator != null)
                _holeOrchestrator.SetSpawnPaused(false);
            TryStartSpawning();
        }
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
                Debug.Log("[NetworkWaveManager] Iniciando spawn por buracos (data-driven).");
                BeginHoleSpawning();
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

    private void BeginHoleSpawning()
    {
        EnsureHoleProfilesFromLegacySettings();
        _holeOrchestrator = EnsureHoleOrchestrator();
        _holeOrchestrator.Configure(_firstSpawnDelay);
        _holeOrchestrator.Begin(SpawnNetworkEnemyFromHole, () => _enemiesAlive < _maxEnemiesAlive);
    }

    private RatHoleSpawnOrchestrator EnsureHoleOrchestrator()
    {
        RatHoleSpawnOrchestrator orchestrator = RatHoleSpawnOrchestrator.Instance;
        if (orchestrator != null)
            return orchestrator;

        GameObject host = new GameObject("RatHoleSpawnOrchestrator");
        return host.AddComponent<RatHoleSpawnOrchestrator>();
    }

    private void EnsureHoleProfilesFromLegacySettings()
    {
        RatHoleSpawnProfile fallback = BuildFallbackHoleProfile();
        if (fallback == null)
            return;

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null || hole.SpawnProfile != null)
                continue;

            hole.ConfigureSpawnProfile(fallback);
        }
    }

    private RatHoleSpawnProfile BuildFallbackHoleProfile()
    {
        if (waveSettings == null || waveSettings.waves == null || waveSettings.waves.Count == 0)
            return null;

        var profile = ScriptableObject.CreateInstance<RatHoleSpawnProfile>();
        profile.minSpawnTime = 2f;
        profile.maxSpawnTime = 5f;

        WaveData firstWave = waveSettings.waves[0];
        if (firstWave.enemies == null)
            return profile;

        for (int i = 0; i < firstWave.enemies.Count; i++)
        {
            GameObject prefab = firstWave.enemies[i].enemyPrefab;
            if (prefab == null)
                continue;

            profile.enemyTable.Add(new RatHoleSpawnProfile.WeightedEnemyEntry
            {
                enemyPrefab = prefab,
                spawnWeight = Mathf.Max(1, firstWave.enemies[i].count)
            });
        }

        return profile;
    }

    private GameObject SpawnNetworkEnemyFromHole(RatHoleSpawnPoint hole, GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab == null)
            return null;

        SpawnNetworkEnemyAt(prefab, spawnPosition);
        return null;
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
        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            RatHoleSpawnProfile profile = hole != null ? hole.SpawnProfile : null;
            if (profile == null || profile.enemyTable == null || profile.enemyTable.Count == 0)
                continue;

            GameObject prefab = profile.enemyTable[0].enemyPrefab;
            if (prefab != null)
                return prefab;
        }

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
            while (GameEvents.IsPaused)
                yield return new WaitForSecondsRealtime(0.25f);

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

        SpawnNetworkEnemyAt(prefab, spawnPosition, useHoleSelection);
    }

    private void SpawnNetworkEnemyAt(GameObject prefab, Vector3 spawnPosition, bool broadcastObjective = true)
    {
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
                if (ShouldInvokeLegacyNightEnded())
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
        GameObject toSpawn = ResolveCienciaSpawnPrefab(prefab);
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

    private GameObject ResolveCienciaSpawnPrefab(GameObject statsPrefab)
    {
        if (networkCienciaPrefab != null
            && networkCienciaPrefab.GetComponent<NetworkCienciaController>() != null
            && networkCienciaPrefab.GetComponent<CircleCollider2D>() != null)
            return networkCienciaPrefab;

        return statsPrefab;
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
        _holeOrchestrator?.StopAll();
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

    private static bool ShouldInvokeLegacyNightEnded()
    {
        return PhaseObjectiveManager.Instance == null;
    }
}
