using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Loop de spawn por buraco: aguarda intervalo sorteado do SO e spawna um rato da tabela de probabilidade.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RatHoleSpawnPoint))]
public class RatHoleSpawnController : MonoBehaviour
{
    public delegate GameObject SpawnRequest(RatHoleSpawnPoint hole, GameObject enemyPrefab, Vector3 spawnPosition);

    [SerializeField] private RatHoleSpawnProfile spawnProfile;

    private RatHoleSpawnPoint _hole;
    private SpawnRequest _spawnRequest;
    private Func<bool> _canSpawnMore;
    private Coroutine _spawnRoutine;
    private bool _running;

    public RatHoleSpawnProfile SpawnProfile
    {
        get => spawnProfile;
        set => spawnProfile = value;
    }

    public bool IsRunning => _running;

    private void Awake()
    {
        _hole = GetComponent<RatHoleSpawnPoint>();
        if (spawnProfile == null && _hole != null)
            spawnProfile = _hole.SpawnProfile;
    }

    public void ConfigureProfile(RatHoleSpawnProfile profile)
    {
        if (profile != null)
            spawnProfile = profile;
    }

    public void StartSpawning(SpawnRequest spawnRequest, Func<bool> canSpawnMore = null)
    {
        if (spawnRequest == null || spawnProfile == null || !spawnProfile.IsValid())
            return;

        _spawnRequest = spawnRequest;
        _canSpawnMore = canSpawnMore;
        StopSpawning();
        _spawnRoutine = StartCoroutine(SpawnLoopRoutine());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        _running = false;
    }

    private IEnumerator SpawnLoopRoutine()
    {
        _running = true;

        while (_running)
        {
            if (_hole == null || !_hole.CanSpawn)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (_canSpawnMore != null && !_canSpawnMore())
            {
                yield return new WaitForSeconds(0.35f);
                continue;
            }

            float wait = spawnProfile.RollSpawnDelay();
            yield return new WaitForSeconds(wait);

            if (!_running || _hole == null || !_hole.CanSpawn)
                continue;

            if (_canSpawnMore != null && !_canSpawnMore())
                continue;

            GameObject prefab = spawnProfile.RollEnemyPrefab();
            if (prefab == null || _spawnRequest == null)
                continue;

            Vector3 spawnPosition = _hole.GetSpawnPosition();
            _spawnRequest.Invoke(_hole, prefab, spawnPosition);
        }
    }
}
