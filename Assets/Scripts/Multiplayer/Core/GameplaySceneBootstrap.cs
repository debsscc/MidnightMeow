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

        ScreenFlowController.Instance?.ClearTransitionOverlay();
        TransitionCameraKeeper.EnsureActive();
        EnsureCameraRig();
        TransitionCameraKeeper.Refresh();

        RoundMagiculaTracker.EnsureExists();
        RoundMagiculaTracker.Instance?.ResetRound();

        Canvas hudCanvas = Object.FindFirstObjectByType<Canvas>();
        if (hudCanvas != null)
            PlayerAbilityHud.EnsureOnCanvas(hudCanvas);
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
