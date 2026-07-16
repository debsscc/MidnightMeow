///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia o ciclo da noite, iniciando e terminando as ondas de inimigos.
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;

public class NightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Config")]
    [SerializeField] private WaveSettings nightConfiguration;

    public event System.Action OnNightEnded;

    private void OnEnable()
    {
        waveGenerator.OnAllWavesCleared += HandleVictory;
    }

    private void OnDisable()
    {
        waveGenerator.OnAllWavesCleared -= HandleVictory;
    }

    private void Start()
    {
        if (ShouldDeferToNetworkWaveManager())
        {
            Debug.Log("[NightManager] Multiplayer ativo — ondas delegadas ao NetworkWaveManager.");
            return;
        }

        StartNight();
    }

    private static bool ShouldDeferToNetworkWaveManager()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || (!nm.IsClient && !nm.IsServer))
            return false;

        return Object.FindFirstObjectByType<NetworkWaveManager>(FindObjectsInactive.Include) != null;
    }

    public void StartNight()
    {
        if (nightConfiguration != null && TryBeginHoleSpawning())
            return;

        if (nightConfiguration != null)
        {
            waveGenerator.Initialize(nightConfiguration);
            waveGenerator.StartSpawning();
        }
        else
        {
            Debug.LogError("Nenhuma configuração de wave atribuída ao NightManager!");
        }
    }

    private bool TryBeginHoleSpawning()
    {
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (catalog == null || !catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry))
            return false;

        if (!entry.useHoleSpawning || entry.useWaveSpawning)
            return false;

        LocalRatHoleSpawnService service = Object.FindFirstObjectByType<LocalRatHoleSpawnService>(FindObjectsInactive.Include);
        if (service == null)
        {
            GameObject host = waveGenerator != null ? waveGenerator.gameObject : new GameObject("LocalRatHoleSpawnService");
            service = host.GetComponent<LocalRatHoleSpawnService>();
            if (service == null)
                service = host.AddComponent<LocalRatHoleSpawnService>();
        }

        service.Configure(nightConfiguration, entry.maxRatsAlive, entry.firstSpawnDelay);
        service.Begin();
        return true;
    }

    public void ForceStop()
    {
        waveGenerator.StopSpawning();
        LocalRatHoleSpawnService localHoleSpawner =
            Object.FindFirstObjectByType<LocalRatHoleSpawnService>(FindObjectsInactive.Include);
        if (localHoleSpawner != null)
            localHoleSpawner.Stop();
    }

    private void HandleVictory()
    {
        Debug.Log("Todas as waves foram completadas!");
        GameEvents.InvokeNightEnded();
        OnNightEnded?.Invoke();
    }
}
