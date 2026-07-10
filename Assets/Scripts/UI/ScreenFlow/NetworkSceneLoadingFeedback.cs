using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exibe fade-out no cliente quando o host inicia troca de cena via NGO.
/// Fade-in de chegada fica em <see cref="NetworkSceneSyncUtility"/> ou <see cref="ScreenFlowController"/>.
/// </summary>
public static class NetworkSceneLoadingFeedback
{
    private const float ClientFadeOutSeconds = 1f;
    private static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureSubscribed();
    }

    public static void EnsureSubscribed() => TrySubscribe();

    private static void TrySubscribe()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || net.SceneManager == null || _subscribed)
            return;

        net.SceneManager.OnSceneEvent -= HandleSceneEvent;
        net.SceneManager.OnSceneEvent += HandleSceneEvent;
        _subscribed = true;
    }

    private static void HandleSceneLoaded(Scene _, LoadSceneMode __) => EnsureSubscribed();

    private static void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
            return;

        if (sceneEvent.SceneEventType == SceneEventType.Load)
            HandleClientSceneLoadStarted(sceneEvent.SceneName);
        else if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            HandleClientSceneLoadCompleted(sceneEvent.SceneName);
    }

    private static void HandleClientSceneLoadStarted(string sceneName)
    {
        if (!ShouldCoverOnLoad(sceneName))
            return;

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.IsTransitioning)
            return;

        TransitionFadeOverlay overlay = TransitionFadeOverlay.EnsureExists();
        if (overlay == null)
            return;

        if (ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName))
        {
            overlay.HandoffToDedicatedLoadingScene();
            return;
        }

        overlay.ShowLoading();
        overlay.BeginAnimatedFadeOut(ClientFadeOutSeconds);
    }

    private static void HandleClientSceneLoadCompleted(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName))
            return;

        if (!ShouldFadeInOnArrival(sceneName))
            return;

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null || flow.IsTransitioning)
            return;

        flow.TryBeginFadeInAfterNetworkSceneArrival(sceneName);
    }

    private static bool ShouldCoverOnLoad(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName))
            return true;

        return sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
               || sceneName is "Preparation" or "Loading1" or "Loading2"
               || sceneName is "VictoryScene" or "GameOver";
    }

    private static bool ShouldFadeInOnArrival(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName))
            return false;

        return sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
               || sceneName is "Preparation"
               || sceneName is "VictoryScene" or "GameOver";
    }
}
