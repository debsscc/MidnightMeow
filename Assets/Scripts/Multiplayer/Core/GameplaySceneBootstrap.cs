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
        EnsureCameraRig();
        EnableGameplayCameras();

        Canvas hudCanvas = Object.FindFirstObjectByType<Canvas>();
        if (hudCanvas != null)
            PlayerAbilityHud.EnsureOnCanvas(hudCanvas);
    }

    public static void EnsureCameraRig()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        if (MultiplayerCameraController.Instance != null)
            return;

        MultiplayerCameraController existing =
            Object.FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        GameplayPrefabCatalog catalog = GameplayPrefabCatalog.LoadCached();
        if (catalog == null || catalog.multiplayerCameraRigPrefab == null)
        {
            Debug.LogError("[GameplaySceneBootstrap] GameplayPrefabCatalog ausente em Resources ou sem MultiplayerCameraRig.");
            return;
        }

        Object.Instantiate(catalog.multiplayerCameraRigPrefab);
        EnableGameplayCameras();
        NetworkPlayerController.RebindLocalPlayerCameras();
    }

    public static void EnableGameplayCameras()
    {
        MultiplayerCameraController controller = MultiplayerCameraController.Instance;
        if (controller == null)
            controller = Object.FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include);

        if (controller == null)
            return;

        Camera main = controller.MainCamera;
        if (main != null)
            main.enabled = true;
    }

    public static bool IsGameplayScene(string sceneName) =>
        !string.IsNullOrEmpty(sceneName)
        && (sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
            || sceneName is "Game" or "Gameplay");
}
