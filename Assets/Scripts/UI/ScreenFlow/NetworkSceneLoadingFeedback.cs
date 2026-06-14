using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exibe o overlay de loading imediatamente quando o host inicia troca de cena via NGO.
/// Evita que clientes vejam a tela anterior "travada" até a cena carregar.
/// </summary>
public static class NetworkSceneLoadingFeedback
{
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

        if (sceneEvent.SceneEventType != SceneEventType.Load)
            return;

        if (!ShouldShowLoadingForScene(sceneEvent.SceneName))
            return;

        TransitionFadeOverlay.EnsureExists();
        ScreenFlowController.EnsureExists();
        TransitionFadeOverlay.Instance?.ShowLoading();
        TransitionFadeOverlay.Instance?.SetFadeImmediate(1f);
    }

    private static bool ShouldShowLoadingForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName))
            return true;

        return sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
               || sceneName is "Preparation" or "Loading1" or "Loading2";
    }
}
