using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante rig de câmera e overlay limpo quando Fase-* carrega (host ou cliente via NGO).
/// </summary>
public static class NetworkGameplaySceneHooks
{
    private static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleUnitySceneLoaded;
        SceneManager.sceneLoaded += HandleUnitySceneLoaded;
        EnsureSubscribed();
    }

    public static void EnsureSubscribed() => TrySubscribeNetworkSceneEvents();

    private static void TrySubscribeNetworkSceneEvents()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || net.SceneManager == null || _subscribed)
            return;

        net.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
        net.SceneManager.OnSceneEvent += HandleNetworkSceneEvent;
        _subscribed = true;
    }

    private static void HandleUnitySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribeNetworkSceneEvents();

        if (IsGameplayScene(scene.name))
            OnGameplaySceneReady(scene.name);
    }

    private static void HandleNetworkSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsGameplayScene(sceneEvent.SceneName))
            return;

        if (sceneEvent.SceneEventType is SceneEventType.LoadEventCompleted or SceneEventType.SynchronizeComplete)
            OnGameplaySceneReady(sceneEvent.SceneName);
    }

    private static void OnGameplaySceneReady(string sceneName)
    {
        GameplaySceneBootstrap.EnsureCameraRig();
        GameplaySceneBootstrap.EnableGameplayCameras();
        ScreenFlowController.Instance?.ClearTransitionOverlay();
        NetworkPlayerController.RebindLocalPlayerCameras();
    }

    private static bool IsGameplayScene(string sceneName) =>
        !string.IsNullOrEmpty(sceneName)
        && (sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
            || sceneName is "Game" or "Gameplay");
}
