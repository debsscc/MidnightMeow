using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mantém uma câmera fallback ativa durante transições de cena para evitar "Display 1: No cameras rendering".
/// </summary>
public static class TransitionCameraKeeper
{
    private static Camera _fallbackCamera;
    private static bool _subscribed;

    public static Camera FallbackCamera => _fallbackCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureSubscribed();
        Refresh();
    }

    public static void EnsureActive()
    {
        EnsureSubscribed();
        if (_fallbackCamera == null)
            CreateFallbackCamera();

        if (_fallbackCamera != null && !_fallbackCamera.enabled)
            _fallbackCamera.enabled = true;
    }

    public static void Refresh()
    {
        if (_fallbackCamera == null)
            return;

        bool gameplayHasCamera = GameplaySceneBootstrap.IsGameplayScene(SceneManager.GetActiveScene().name)
                                 && HasEnabledGameplayCamera();

        _fallbackCamera.enabled = !gameplayHasCamera && !HasAnyEnabledCamera();
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
        _subscribed = true;
    }

    private static void HandleSceneLoaded(Scene _, LoadSceneMode __) => Refresh();

    private static void HandleSceneUnloaded(Scene _) => EnsureActive();

    private static void CreateFallbackCamera()
    {
        var go = new GameObject("TransitionFallbackCamera");
        Object.DontDestroyOnLoad(go);
        _fallbackCamera = go.AddComponent<Camera>();
        _fallbackCamera.clearFlags = CameraClearFlags.SolidColor;
        _fallbackCamera.backgroundColor = Color.black;
        _fallbackCamera.orthographic = true;
        _fallbackCamera.orthographicSize = 5f;
        _fallbackCamera.depth = -100f;
        _fallbackCamera.cullingMask = 0;
        _fallbackCamera.tag = "Untagged";
    }

    private static bool HasEnabledGameplayCamera()
    {
        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        if (controller == null)
            return false;

        Camera main = controller.MainCamera;
        return main != null && main.isActiveAndEnabled;
    }

    private static bool HasAnyEnabledCamera()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || cam == _fallbackCamera)
                continue;

            if (cam.isActiveAndEnabled)
                return true;
        }

        return false;
    }
}
