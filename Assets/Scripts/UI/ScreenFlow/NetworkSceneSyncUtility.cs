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

    public static IEnumerator WaitForActiveScene(string sceneName, float timeoutSeconds = 45f)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            ScreenFlowController.Instance?.ClearTransitionOverlay();
            if (GameplaySceneBootstrap.IsGameplayScene(sceneName))
            {
                GameplaySceneBootstrap.RebindLocalPlayerCamera();
                GameplayCameraRebindUtility.ScheduleAfterGameplaySceneReady();
            }

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

        ScreenFlowController.Instance?.ClearTransitionOverlay();

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            Debug.LogWarning(
                $"[NetworkSceneSyncUtility] Timeout aguardando cena '{sceneName}' via rede (ativa: '{SceneManager.GetActiveScene().name}').");
            yield break;
        }

        if (GameplaySceneBootstrap.IsGameplayScene(sceneName))
        {
            GameplaySceneBootstrap.RebindLocalPlayerCamera();
            GameplayCameraRebindUtility.ScheduleAfterGameplaySceneReady();
        }
    }
}
