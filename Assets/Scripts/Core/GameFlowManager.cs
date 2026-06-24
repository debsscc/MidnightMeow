using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// API legada de fluxo. Delega para <see cref="ScreenFlowController"/> quando disponível.
/// Mantido para botões que ainda chamam LoadMenu/LoadLobby diretamente.
/// </summary>
[DisallowMultipleComponent]
public class GameFlowManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "Menu2";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string defaultGameplaySceneName = "Fase-1";

    [Header("Screen Flow")]
    [SerializeField] private SceneFlowCatalog catalog;

    public event Action OnSceneLoadStarted;
    public event Action OnSceneLoadCompleted;

    public string MenuSceneName => menuSceneName;
    public string LobbySceneName => lobbySceneName;
    public string DefaultGameplaySceneName => defaultGameplaySceneName;

    private void OnEnable()
    {
        if (ScreenFlowController.Instance != null)
        {
            ScreenFlowController.Instance.OnTransitionCompleted += HandleScreenFlowCompleted;
            if (catalog != null)
                ScreenFlowController.Instance.SetCatalog(catalog);
        }
    }

    private void OnDisable()
    {
        if (ScreenFlowController.Instance != null)
            ScreenFlowController.Instance.OnTransitionCompleted -= HandleScreenFlowCompleted;
    }

    private void HandleScreenFlowCompleted(string _) => OnSceneLoadCompleted?.Invoke();

    public void LoadMenu() => RequestRouteOrFallback(SceneFlowRouteIds.BootstrapToMenu, menuSceneName);

    public void LoadLobby() => RequestRouteOrFallback(SceneFlowRouteIds.MenuToLobby, "Loading2");

    public void LoadDefaultGameplay() => LobbyMatchFlow.TryBeginMatchFromLobby();

    public void LoadPhase(string phaseName)
    {
        if (string.IsNullOrEmpty(phaseName))
        {
            Debug.LogError("GameFlowManager: phaseName is empty.");
            return;
        }

        RequestSceneOrFallback(phaseName);
    }

    public void LoadPhase(int buildIndex)
    {
        OnSceneLoadStarted?.Invoke();
        StartCoroutine(LoadBuildIndexFallback(buildIndex));
    }

    private void RequestRouteOrFallback(string routeId, string sceneFallback)
    {
        OnSceneLoadStarted?.Invoke();

        if (TryRequestRoute(routeId))
            return;

        RequestSceneOrFallback(sceneFallback);
    }

    private bool TryRequestRoute(string routeId)
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null)
            return false;

        if (catalog != null)
            flow.SetCatalog(catalog);

        if (flow.RequestRoute(routeId))
            return true;

        return false;
    }

    private void RequestSceneOrFallback(string sceneName)
    {
        OnSceneLoadStarted?.Invoke();

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.TryBeginTransition(sceneName))
            return;

        StartCoroutine(LoadSceneFallback(sceneName));
    }

    private IEnumerator LoadSceneFallback(string sceneName)
    {
        var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: falha ao carregar '{sceneName}'.");
            yield break;
        }

        while (!async.isDone)
            yield return null;

        OnSceneLoadCompleted?.Invoke();
    }

    private IEnumerator LoadBuildIndexFallback(int buildIndex)
    {
        var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: falha buildIndex {buildIndex}.");
            yield break;
        }

        while (!async.isDone)
            yield return null;

        OnSceneLoadCompleted?.Invoke();
    }
}
