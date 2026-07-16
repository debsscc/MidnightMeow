using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Loop de spawn por buraco: aguarda intervalo sorteado do SO e spawna um rato da tabela de probabilidade.
/// Pausa o timer enquanto o buraco está sendo selado ou o jogo está pausado.
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
    private bool _spawnPaused;

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

    public void SetSpawnPaused(bool paused)
    {
        _spawnPaused = paused;
    }

    private IEnumerator SpawnLoopRoutine()
    {
        _running = true;

        while (_running)
        {
            if (ShouldHoldSpawnTimer())
            {
                yield return new WaitForSecondsRealtime(0.25f);
                continue;
            }

            if (_hole == null || !_hole.CanSpawn)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Guarda de limite global (maxRatsAlive): não consome o timer de delay.
            if (_canSpawnMore != null && !_canSpawnMore())
            {
                yield return new WaitForSeconds(0.35f);
                continue;
            }

            float wait = spawnProfile.RollSpawnDelay();
            float elapsed = 0f;

            // Timer pausável: selamento / pause / limite não avançam o delay.
            while (elapsed < wait)
            {
                if (!_running)
                    yield break;

                if (ShouldHoldSpawnTimer() || _hole == null || !_hole.CanSpawn)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    continue;
                }

                if (_canSpawnMore != null && !_canSpawnMore())
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!_running || _hole == null || !_hole.CanSpawn || _hole.IsBeingSealed)
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

    private bool ShouldHoldSpawnTimer()
    {
        return _spawnPaused ||
               GameEvents.IsPaused ||
               (_hole != null && _hole.IsBeingSealed);
    }
}
