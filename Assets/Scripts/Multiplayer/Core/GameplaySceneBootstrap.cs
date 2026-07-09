using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap ao entrar em cenas de gameplay: câmera MP, overlay e HUD.
/// </summary>
public static class GameplaySceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!IsGameplayScene(scene))
            return;

        TransitionCameraKeeper.EnsureActive();
        EnsureCameraRig();
        TransitionCameraKeeper.Refresh();

        RoundMagiculaTracker.EnsureExists();
        RoundMagiculaTracker.Instance?.ResetRound();

        TryEnsureGameplayHud();
        EnsureCooperativeZoneVisuals();
    }

    private static void EnsureCooperativeZoneVisuals()
    {
        NetworkRatHoleSealManager sealManager =
            Object.FindFirstObjectByType<NetworkRatHoleSealManager>(FindObjectsInactive.Include);

        if (sealManager != null)
            RatHoleSealZoneVisual.EnsureAttached(sealManager);

        NetworkDownedReviveManager reviveManager =
            Object.FindFirstObjectByType<NetworkDownedReviveManager>(FindObjectsInactive.Include);
        if (reviveManager != null)
            DownedReviveZoneVisualHost.EnsureAttached(reviveManager);

        CarriageController carriage = CarriageController.Instance
            ?? Object.FindFirstObjectByType<CarriageController>(FindObjectsInactive.Include);
        if (carriage != null)
        {
            NetworkCarriageRepairManager repairManager = carriage.GetComponent<NetworkCarriageRepairManager>();
            if (repairManager != null)
                CarriageRepairZoneVisualHost.EnsureAttached(repairManager);
        }
    }

    public static void TryEnsureGameplayHud()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        Canvas hudCanvas = ResolveGameplayHudCanvas();
        if (hudCanvas != null)
            EnsureGameplayHudWidgets(hudCanvas);
    }

    private static void EnsureGameplayHudWidgets(Canvas hudCanvas)
    {
        if (hudCanvas == null)
            return;

        if (hudCanvas.transform.localScale.sqrMagnitude < 0.01f)
            hudCanvas.transform.localScale = Vector3.one;

        GameplayHudController controller = hudCanvas.GetComponent<GameplayHudController>();
        if (controller == null)
            controller = hudCanvas.gameObject.AddComponent<GameplayHudController>();
        else
            controller.EnsureWidgets();
    }

    private static Canvas ResolveGameplayHudCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "Gameplay_UI")
                return canvases[i];
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            if (canvas.GetComponent<GameplayHudController>() != null
                || canvas.GetComponentInChildren<HordeIndicator>(true) != null
                || canvas.GetComponentInChildren<healthBarUi>(true) != null)
                return canvas;
        }

        return Object.FindFirstObjectByType<Canvas>();
    }

    /// <summary>Garante que o rig existe e está ativo, sem rebind (seguro para TryBindCameraNow).</summary>
    public static void EnsureCameraRigPresent()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        MultiplayerCameraController existing = MultiplayerCameraController.Resolve();
        if (existing != null)
            ActivateCameraRig(existing);
    }

    /// <summary>Instancia o rig só quando a cena não possui um (último recurso).</summary>
    public static void SpawnCameraRigIfMissing()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        if (MultiplayerCameraController.Resolve() != null)
            return;

        GameplayPrefabCatalog catalog = GameplayPrefabCatalog.LoadCached();
        if (catalog == null || catalog.multiplayerCameraRigPrefab == null)
        {
            Debug.LogError("[GameplaySceneBootstrap] GameplayPrefabCatalog ausente em Resources ou sem MultiplayerCameraRig.");
            return;
        }

        MultiplayerCameraController spawned =
            Object.Instantiate(catalog.multiplayerCameraRigPrefab)
                .GetComponent<MultiplayerCameraController>();
        ActivateCameraRig(spawned);
    }

    /// <summary>Garante rig + rebind do jogador local + câmera ativa.</summary>
    public static void EnsureCameraRig()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        EnsureCameraRigPresent();
        if (MultiplayerCameraController.Resolve() == null)
            SpawnCameraRigIfMissing();
        RebindLocalPlayerCamera();
    }

    public static void RebindLocalPlayerCamera()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        NetworkPlayerController.RebindLocalPlayerCameras();

        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        controller?.TryFindLocalPlayer();

        EnsureActiveGameplayCamera();
    }

    private static void ActivateCameraRig(MultiplayerCameraController controller)
    {
        if (controller == null)
            return;

        if (!controller.gameObject.activeSelf)
            controller.gameObject.SetActive(true);
    }

    public static void EnableGameplayCameras() => EnsureActiveGameplayCamera();

    /// <summary>Garante ao menos uma câmera ativa para evitar "Display Error" antes do spawn dos jogadores.</summary>
    public static void EnsureActiveGameplayCamera()
    {
        MultiplayerCameraController controller = MultiplayerCameraController.Instance;
        if (controller == null)
            controller = Object.FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include);

        Camera main = controller != null ? controller.MainCamera : null;
        if (main == null)
            main = Camera.main;

        if (main == null)
            main = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

        if (main == null)
            return;

        if (!main.gameObject.activeInHierarchy)
            main.gameObject.SetActive(true);

        if (string.IsNullOrEmpty(main.tag) || main.tag != "MainCamera")
            main.tag = "MainCamera";

        if (!main.isActiveAndEnabled)
            main.enabled = true;

        GameplayCameraSceneUtility.TakeOverGameplayRendering(main);
        TransitionCameraKeeper.Refresh();
    }

    public static bool IsGameplayScene(string sceneName) =>
        !string.IsNullOrEmpty(sceneName)
        && (sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
            || sceneName is "Game" or "Gameplay");
}
