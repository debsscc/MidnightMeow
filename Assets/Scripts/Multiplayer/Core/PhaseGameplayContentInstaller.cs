using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aplica conteúdo de fase em runtime (selamento, carruagem, wave settings) quando a cena carrega.
/// Complementa o setup feito pelo <see cref="PhaseSceneSetupEditor"/> no Editor.
/// </summary>
public static class PhaseGameplayContentInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!GameplaySceneBootstrap.IsGameplayScene(sceneName))
            return;

        ApplyPhaseContent(sceneName);
    }

    public static void ApplyPhaseContent(string sceneName)
    {
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        if (catalog == null || !catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry))
            return;

        ConfigureWaveManager(entry);
        EnsurePhaseObjectiveManager(entry);
        DisableLegacyWaveSystemsIfMultiplayerActive();
        GameplayScenePlayerCleanup.RemoveOrphanScenePlayers();

        if (entry.enableRatHoleSealing)
        {
            EnsureRatHoleSealConfig();
            EnsureSealZoneVisualHost();
        }

        if (entry.enableCarriage && sceneName == "Fase-2")
        {
            EnsureCarriagePathFromBounds();
            EnsureCarriageVisuals();
        }
    }

    private static void ConfigureWaveManager(PhaseWaveSettingsCatalog.PhaseEntry entry)
    {
        NetworkWaveManager waveManager = Object.FindFirstObjectByType<NetworkWaveManager>(FindObjectsInactive.Include);
        if (waveManager == null)
            return;

        waveManager.ConfigurePhaseEntry(entry);
    }

    private static void EnsurePhaseObjectiveManager(PhaseWaveSettingsCatalog.PhaseEntry entry)
    {
        PhaseObjectiveManager manager = Object.FindFirstObjectByType<PhaseObjectiveManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            NetworkWaveManager waveManager = Object.FindFirstObjectByType<NetworkWaveManager>(FindObjectsInactive.Include);
            GameObject host = waveManager != null ? waveManager.gameObject : new GameObject("PhaseObjectiveManager");
            if (waveManager == null)
                manager = host.AddComponent<PhaseObjectiveManager>();
            else
                manager = host.AddComponent<PhaseObjectiveManager>();
        }

        manager.Configure(entry);
        EnsureRatHoleSealStatusUi();
    }

    private static void EnsureRatHoleSealStatusUi()
    {
        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null || hole.GetComponent<RatHoleSealStatusUI>() != null)
                continue;

            hole.gameObject.AddComponent<RatHoleSealStatusUI>();
        }
    }

    private static void EnsureRatHoleSealConfig()
    {
        RatHoleSealConfig config = Resources.Load<RatHoleSealConfig>("RatHoleSealConfig");
        NetworkRatHoleSealManager sealManager =
            Object.FindFirstObjectByType<NetworkRatHoleSealManager>(FindObjectsInactive.Include);

        if (sealManager != null && config != null)
            sealManager.ConfigureSealConfig(config);
    }

    private static void EnsureSealZoneVisualHost()
    {
        NetworkRatHoleSealManager sealManager =
            Object.FindFirstObjectByType<NetworkRatHoleSealManager>(FindObjectsInactive.Include);

        if (sealManager != null)
            RatHoleSealZoneVisual.EnsureAttached(sealManager);
    }

    private static void EnsureCarriagePathFromBounds()
    {
        if (NetworkCarriage.Instance != null)
            return;

        CarriagePath existingPath = Object.FindFirstObjectByType<CarriagePath>(FindObjectsInactive.Include);
        if (existingPath != null)
            return;

        if (!TryGetMapBounds(out Bounds bounds))
            return;

        float centerY = bounds.center.y;
        Vector3 left = new Vector3(bounds.min.x, centerY, 0f);
        Vector3 right = new Vector3(bounds.max.x, centerY, 0f);

        GameObject pathRoot = new GameObject("CarriagePath_Runtime");
        CarriagePath path = pathRoot.AddComponent<CarriagePath>();

        GameObject wpStart = new GameObject("Waypoint_Start");
        wpStart.transform.SetParent(pathRoot.transform, false);
        wpStart.transform.position = left;

        GameObject wpEnd = new GameObject("Waypoint_End");
        wpEnd.transform.SetParent(pathRoot.transform, false);
        wpEnd.transform.position = right;

        path.ConfigureWaypoints(new[] { wpStart.transform, wpEnd.transform });
    }

    private static void EnsureCarriageVisuals()
    {
        NetworkCarriage carriage = Object.FindFirstObjectByType<NetworkCarriage>(FindObjectsInactive.Include);
        if (carriage == null)
            return;

        carriage.EnsureRuntimePresentation();
    }

    private static bool TryGetMapBounds(out Bounds bounds)
    {
        bounds = default;
        CameraBoundsVolume volume = Object.FindFirstObjectByType<CameraBoundsVolume>(FindObjectsInactive.Include);
        if (volume == null || volume.BoundsCollider == null)
            return false;

        bounds = volume.BoundsCollider.bounds;
        return bounds.size.sqrMagnitude > 0.01f;
    }

    private static void DisableLegacyWaveSystemsIfMultiplayerActive()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        NightManager night = Object.FindFirstObjectByType<NightManager>(FindObjectsInactive.Include);
        if (night != null)
            night.enabled = false;

        WaveGenerator generator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        if (generator != null)
            generator.enabled = false;
    }
}
