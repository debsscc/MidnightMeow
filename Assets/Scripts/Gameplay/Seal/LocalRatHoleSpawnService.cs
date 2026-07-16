using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Spawn local por buracos para single player (sem NGO).
/// </summary>
[DisallowMultipleComponent]
public class LocalRatHoleSpawnService : MonoBehaviour
{
    [SerializeField] private WaveSettings fallbackWaveSettings;

    [Tooltip("Limite global de ratos vivos.")]
    [FormerlySerializedAs("maxEnemiesAlive")]
    [SerializeField] private int maxRatsAlive = 35;
    [SerializeField] private float firstSpawnDelay = 3f;

    private int _enemiesAlive;
    private RatHoleSpawnOrchestrator _orchestrator;
    private bool _started;

    public void Configure(WaveSettings settings, int maxAlive, float initialDelay)
    {
        if (settings != null)
            fallbackWaveSettings = settings;

        maxRatsAlive = Mathf.Max(1, maxAlive);
        firstSpawnDelay = Mathf.Max(0f, initialDelay);
    }

    public void Begin()
    {
        if (_started)
            return;

        _started = true;
        EnsureFallbackProfiles();
        _orchestrator = EnsureOrchestrator();
        _orchestrator.Configure(firstSpawnDelay);
        // Guarda: não spawna se já atingiu o teto de ratos vivos da fase.
        _orchestrator.Begin(SpawnLocalEnemy, () => _enemiesAlive < maxRatsAlive);
    }

    public void Stop()
    {
        _started = false;
        _orchestrator?.StopAll();
    }

    private RatHoleSpawnOrchestrator EnsureOrchestrator()
    {
        if (RatHoleSpawnOrchestrator.Instance != null)
            return RatHoleSpawnOrchestrator.Instance;

        GameObject host = new GameObject("RatHoleSpawnOrchestrator");
        return host.AddComponent<RatHoleSpawnOrchestrator>();
    }

    private void EnsureFallbackProfiles()
    {
        if (fallbackWaveSettings == null)
            return;

        RatHoleSpawnProfile fallback = BuildFallbackProfile(fallbackWaveSettings);
        if (fallback == null)
            return;

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null || hole.SpawnProfile != null)
                continue;

            hole.ConfigureSpawnProfile(fallback);
        }
    }

    private static RatHoleSpawnProfile BuildFallbackProfile(WaveSettings settings)
    {
        if (settings.waves == null || settings.waves.Count == 0)
            return null;

        var profile = ScriptableObject.CreateInstance<RatHoleSpawnProfile>();
        profile.minSpawnTime = 2f;
        profile.maxSpawnTime = 5f;

        WaveData firstWave = settings.waves[0];
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

    private GameObject SpawnLocalEnemy(RatHoleSpawnPoint hole, GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab == null)
            return null;

        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        _enemiesAlive++;

        if (enemy.TryGetComponent<HealthComponent>(out HealthComponent health))
            health.OnDied.AddListener(HandleEnemyDied);

        GameEvents.InvokeWaveStatusChanged(1, 1, _enemiesAlive, 0);
        return enemy;
    }

    private void HandleEnemyDied()
    {
        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
        GameEvents.InvokeWaveStatusChanged(1, 1, _enemiesAlive, 0);
    }
}
