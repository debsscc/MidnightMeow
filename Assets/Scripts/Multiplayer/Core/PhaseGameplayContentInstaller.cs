using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aplica conteúdo de fase em runtime (selamento, carruagem, wave settings) quando a cena carrega.
/// Complementa o setup feito pelo <see cref="PhaseSceneSetupEditor"/> no Editor.
/// </summary>
public static class PhaseGameplayContentInstaller
{
    private static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (_subscribed)
            return;

        _subscribed = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameplaySceneBootstrap.IsGameplayScene(scene.name))
            return;

        ApplyPhaseContent(scene.name);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstSceneLoad()
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
            EnsureCarriageSetup();
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
        Debug.Log($"[PhaseGameplayContentInstaller] PhaseObjectiveManager configurado para {entry.sceneName} (vitória={entry.winCondition}).");
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

    private static void EnsureCarriageSetup()
    {
        NetworkCarriage carriage = Object.FindFirstObjectByType<NetworkCarriage>(FindObjectsInactive.Include);
        if (carriage != null)
            ConfigureCarriage(carriage);

        NetworkCarriageSpawner.EnsureCarriageSpawned();
    }

    public static void ConfigureCarriage(NetworkCarriage carriage)
    {
        if (carriage == null)
            return;

        CarriageConfig config = carriage.Config;
        CarriagePath path = EnsureCarriagePath(carriage, config);
        carriage.ConfigurePath(path);
        carriage.EnsureRuntimePresentation();
    }

    private static CarriagePath EnsureCarriagePath(NetworkCarriage carriage, CarriageConfig config)
    {
        CarriagePath path = carriage.Path;
        if (path == null)
            path = Object.FindFirstObjectByType<CarriagePath>(FindObjectsInactive.Include);

        float pathY = ResolveCarriagePathY(config);
        Vector3 start = new Vector3(config != null ? config.pathStartX : -42f, pathY, 0f);
        Vector3 end = new Vector3(config != null ? config.pathEndX : 18f, pathY, 0f);

        if (TryGetMapBounds(out Bounds bounds))
            end.x = Mathf.Min(end.x, bounds.max.x - 2f);

        if (path == null)
        {
            GameObject pathRoot = new GameObject("CarriagePath");
            path = pathRoot.AddComponent<CarriagePath>();
        }

        Transform pathRootTransform = path.transform;
        Transform wpStart = pathRootTransform.Find("Waypoint_Start");
        if (wpStart == null)
        {
            GameObject startGo = new GameObject("Waypoint_Start");
            startGo.transform.SetParent(pathRootTransform, false);
            wpStart = startGo.transform;
        }

        Transform wpEnd = pathRootTransform.Find("Waypoint_End");
        if (wpEnd == null)
        {
            GameObject endGo = new GameObject("Waypoint_End");
            endGo.transform.SetParent(pathRootTransform, false);
            wpEnd = endGo.transform;
        }

        wpStart.position = start;
        wpEnd.position = end;
        path.ConfigureWaypoints(new[] { wpStart, wpEnd });

        if (carriage != null)
            carriage.transform.position = start;

        return path;
    }

    private static float ResolveCarriagePathY(CarriageConfig config)
    {
        if (config != null && config.useCustomPathY)
            return config.pathY;

        if (TryGetMapBounds(out Bounds bounds))
            return bounds.center.y;

        return 0f;
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
