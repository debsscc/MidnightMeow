using System.Collections;
using UnityEngine;

/// <summary>
/// Inicia/para spawn por buraco em todos os <see cref="RatHoleSpawnPoint"/> da cena.
/// Usado por <see cref="NetworkWaveManager"/> (rede) e spawn local (single player).
/// </summary>
[DisallowMultipleComponent]
public class RatHoleSpawnOrchestrator : MonoBehaviour
{
    public static RatHoleSpawnOrchestrator Instance { get; private set; }

    [SerializeField] private float firstSpawnDelay = 3f;

    private RatHoleSpawnController.SpawnRequest _spawnRequest;
    private System.Func<bool> _canSpawnMore;
    private Coroutine _startRoutine;
    private bool _started;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(float initialDelay)
    {
        firstSpawnDelay = Mathf.Max(0f, initialDelay);
    }

    public void Begin(
        RatHoleSpawnController.SpawnRequest spawnRequest,
        System.Func<bool> canSpawnMore = null)
    {
        if (spawnRequest == null || _started)
            return;

        _spawnRequest = spawnRequest;
        _canSpawnMore = canSpawnMore;
        _started = true;
        _startRoutine = StartCoroutine(BeginAfterDelayRoutine());
    }

    public void StopAll()
    {
        _started = false;

        if (_startRoutine != null)
        {
            StopCoroutine(_startRoutine);
            _startRoutine = null;
        }

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null)
                continue;

            RatHoleSpawnController controller = hole.GetComponent<RatHoleSpawnController>();
            controller?.StopSpawning();
        }
    }

    private IEnumerator BeginAfterDelayRoutine()
    {
        if (firstSpawnDelay > 0f)
            yield return new WaitForSeconds(firstSpawnDelay);

        EnsureControllersOnHoles();

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null)
                continue;

            RatHoleSpawnController controller = hole.GetComponent<RatHoleSpawnController>();
            if (controller == null || controller.SpawnProfile == null || !controller.SpawnProfile.IsValid())
                continue;

            controller.StartSpawning(_spawnRequest, _canSpawnMore);
        }

        _startRoutine = null;
    }

    private static void EnsureControllersOnHoles()
    {
        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null)
                continue;

            if (hole.GetComponent<RatHoleSpawnController>() == null)
                hole.gameObject.AddComponent<RatHoleSpawnController>();
        }
    }
}
