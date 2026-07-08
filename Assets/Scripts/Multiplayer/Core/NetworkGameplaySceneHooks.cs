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
        SceneManager.sceneUnloaded -= HandleUnitySceneUnloaded;
        SceneManager.sceneUnloaded += HandleUnitySceneUnloaded;
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

        if (scene.name == "Fase-2")
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening && networkManager.IsServer)
                CarriageSpawner.NotifyClientSceneReady(networkManager.LocalClientId);
        }
    }

    private static void HandleUnitySceneUnloaded(Scene scene)
    {
        if (scene.name == "Fase-2")
            CarriageSpawner.ClearClientSceneReadyState();
    }

    private static void HandleNetworkSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsGameplayScene(sceneEvent.SceneName))
            return;

        if (sceneEvent.SceneEventType is SceneEventType.LoadEventCompleted or SceneEventType.SynchronizeComplete)
        {
            OnGameplaySceneReady(sceneEvent.SceneName);

            if (sceneEvent.SceneName == "Fase-2")
                CarriageSpawner.NotifyClientSceneReady(sceneEvent.ClientId);
        }
    }

    private static void OnGameplaySceneReady(string sceneName)
    {
        GameplaySceneBootstrap.RebindLocalPlayerCamera();
        ScreenFlowController.Instance?.ClearTransitionOverlay();
        GameplayCameraRebindUtility.ScheduleAfterGameplaySceneReady();
    }

    private static bool IsGameplayScene(string sceneName) =>
        !string.IsNullOrEmpty(sceneName)
        && (sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
            || sceneName is "Game" or "Gameplay");
}
