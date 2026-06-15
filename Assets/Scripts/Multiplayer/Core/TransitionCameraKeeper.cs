using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mantém uma câmera fallback ativa durante transições de cena para evitar "Display 1: No cameras rendering".
/// </summary>
public static class TransitionCameraKeeper
{
    private static Camera _fallbackCamera;
    private static bool _subscribed;
    private static bool _quitting;

    public static Camera FallbackCamera => _fallbackCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureSubscribed();
        Refresh();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _fallbackCamera = null;
        _subscribed = false;
        _quitting = false;
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
        Application.quitting += HandleApplicationQuitting;
        _subscribed = true;
    }

    private static void HandleApplicationQuitting()
    {
        _quitting = true;
        DestroyFallbackCamera();
    }

    private static void DestroyFallbackCamera()
    {
        if (_fallbackCamera == null)
            return;

        Object.Destroy(_fallbackCamera.gameObject);
        _fallbackCamera = null;
    }

    private static void HandleSceneLoaded(Scene _, LoadSceneMode __) => Refresh();

    private static void HandleSceneUnloaded(Scene _)
    {
        if (_quitting)
            return;

        EnsureActive();
    }

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
