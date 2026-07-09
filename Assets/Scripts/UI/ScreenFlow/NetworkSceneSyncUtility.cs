using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Clientes aguardam cenas carregadas pelo host via NGO Scene Management.
/// </summary>
public static class NetworkSceneSyncUtility
{
    public static bool IsNetworkClientAwaitingHost =>
        NetworkManager.Singleton != null
        && NetworkManager.Singleton.IsListening
        && !NetworkManager.Singleton.IsServer;

    /// <param name="fadeInOnArrival">
    /// Quando true, executa fade-in via <see cref="ScreenFlowController"/> após a cena ativa.
    /// Use false se o caller já fará fade-in (ex.: <see cref="ScreenFlowController"/> NetcodeHost client path).
    /// </param>
    public static IEnumerator WaitForActiveScene(
        string sceneName,
        float timeoutSeconds = 45f,
        bool fadeInOnArrival = true)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            yield return AfterSceneArrived(sceneName, fadeInOnArrival);
            yield break;
        }

        bool loadCompleted = false;
        void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted
                && sceneEvent.SceneName == sceneName)
            {
                loadCompleted = true;
            }
        }

        NetworkGameplaySceneHooks.EnsureSubscribed();

        NetworkManager net = NetworkManager.Singleton;
        if (net != null && net.SceneManager != null)
            net.SceneManager.OnSceneEvent += OnSceneEvent;

        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (loadCompleted || SceneManager.GetActiveScene().name == sceneName)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (net != null && net.SceneManager != null)
            net.SceneManager.OnSceneEvent -= OnSceneEvent;

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            Debug.LogWarning(
                $"[NetworkSceneSyncUtility] Timeout aguardando cena '{sceneName}' via rede (ativa: '{SceneManager.GetActiveScene().name}').");
            yield break;
        }

        yield return AfterSceneArrived(sceneName, fadeInOnArrival);
    }

    private static IEnumerator AfterSceneArrived(string sceneName, bool fadeInOnArrival)
    {
        if (GameplaySceneBootstrap.IsGameplayScene(sceneName))
        {
            GameplaySceneBootstrap.RebindLocalPlayerCamera();
            GameplayCameraRebindUtility.ScheduleAfterGameplaySceneReady();
        }

        if (!fadeInOnArrival)
            yield break;

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null)
            yield break;

        yield return flow.TryFadeInAfterNetworkSceneArrival(sceneName);
    }
}
