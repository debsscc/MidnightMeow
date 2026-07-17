using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Em multiplayer, Preparation ↔ Characters mantém ambas as cenas carregadas e
/// alterna apenas a UI visível (sem unload) para preservar rede e contrato replicado.
/// </summary>
public static class HubSceneNavigator
{
    public const string HubBaseScene = "Preparation";
    public const string HubOverlayScene = "Characters";

    private static bool _showingOverlay;

    public static bool IsShowingOverlay => _showingOverlay;

    public static bool ShouldUseAdditiveNavigation(string sceneName, SceneLoadKind loadKind)
    {
        if (loadKind != SceneLoadKind.SinglePlayer)
            return false;

        if (sceneName is not (HubBaseScene or HubOverlayScene))
            return false;

        bool preparationLoaded = IsBaseLoaded();
        bool overlayLoaded = IsOverlayLoaded();

        if (GameSessionContext.CharactersOrigin == GameSessionContext.CharactersScreenOrigin.Preparation
            && preparationLoaded)
            return true;

        if (preparationLoaded && overlayLoaded)
            return true;

        if (GameSessionContext.IsSinglePlayer)
            return false;

        NetworkManager net = NetworkManager.Singleton;
        return net != null && net.IsListening && preparationLoaded;
    }

    public static bool IsOverlayLoaded()
    {
        Scene overlay = SceneManager.GetSceneByName(HubOverlayScene);
        return overlay.IsValid() && overlay.isLoaded;
    }

    public static bool IsBaseLoaded()
    {
        Scene hub = SceneManager.GetSceneByName(HubBaseScene);
        return hub.IsValid() && hub.isLoaded;
    }

    public static bool CanSkipTransition(string sceneName)
    {
        if (sceneName == HubOverlayScene)
            return _showingOverlay && IsOverlayLoaded();

        if (sceneName == HubBaseScene)
            return IsBaseLoaded() && !_showingOverlay;

        return false;
    }

    public static IEnumerator RunAdditiveTransition(string sceneName, float minLoadingTime, bool useLoading)
    {
        if (sceneName == HubOverlayScene)
        {
            yield return ShowCharacters(minLoadingTime, useLoading);
            yield break;
        }

        if (sceneName == HubBaseScene)
            yield return ShowPreparation(minLoadingTime, useLoading);
    }

    public static void DestroyEventSystemsInScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
                continue;

            EventSystem[] systems = roots[i].GetComponentsInChildren<EventSystem>(true);
            for (int j = 0; j < systems.Length; j++)
            {
                if (systems[j] != null)
                    Object.Destroy(systems[j].gameObject);
            }
        }
    }

    public static void EnsureSingleEventSystem() => EventSystemGlobalBootstrap.Reconcile();

    private static EventSystem PickPrimaryEventSystem(EventSystem[] systems)
    {
        if (systems == null || systems.Length == 0)
            return null;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i].gameObject.scene.name == "DontDestroyOnLoad")
                return systems[i];
        }

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i].gameObject.scene.name == HubBaseScene)
                return systems[i];
        }

        return systems[0];
    }

    private static IEnumerator ShowCharacters(float minLoadingTime, bool useLoading)
    {
        if (!IsBaseLoaded())
        {
            Debug.LogError("[HubSceneNavigator] Preparation precisa estar carregada antes de Characters.");
            yield break;
        }

        if (!IsOverlayLoaded())
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(HubOverlayScene, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError("[HubSceneNavigator] Falha ao carregar Characters em modo aditivo.");
                yield break;
            }

            if (useLoading)
            {
                yield return ReportLoadingProgress(minLoadingTime, load);
            }

            while (!load.isDone)
                yield return null;

            DestroyEventSystemsInScene(HubOverlayScene);
            EventSystemGlobalBootstrap.Reconcile();
        }
        else if (useLoading)
            yield return ReportLoadingProgress(minLoadingTime);

        ApplyHubView(showOverlay: true);
    }

    private static IEnumerator ShowPreparation(float minLoadingTime, bool useLoading)
    {
        if (!IsBaseLoaded())
        {
            Debug.LogError("[HubSceneNavigator] Preparation não está carregada.");
            yield break;
        }

        if (useLoading)
            yield return ReportLoadingProgress(minLoadingTime);

        ApplyHubView(showOverlay: false);
        yield return null;
    }

    private static void ApplyHubView(bool showOverlay)
    {
        SetHubUiVisible(HubBaseScene, !showOverlay);
        if (IsOverlayLoaded())
            SetHubUiVisible(HubOverlayScene, showOverlay);

        _showingOverlay = showOverlay;
        EventSystemGlobalBootstrap.Reconcile();

        Scene active = showOverlay && IsOverlayLoaded()
            ? SceneManager.GetSceneByName(HubOverlayScene)
            : SceneManager.GetSceneByName(HubBaseScene);

        if (active.IsValid())
            SceneManager.SetActiveScene(active);

        PreparationSessionManager.Instance?.ResyncPlayerRoster();

        if (!showOverlay)
            NotifyPreparationVisible();
    }

    private static void SetHubUiVisible(string sceneName, bool visible)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null || !ShouldToggleHubUiRoot(root.name))
                continue;

            root.SetActive(visible);
        }
    }

    private static bool ShouldToggleHubUiRoot(string rootName) =>
        rootName is "Menu" or "UIManager" or "Main Camera" or "Canvas" or "---- ScreenFlow ----";

    private static IEnumerator ReportLoadingProgress(float minLoadingTime, AsyncOperation asyncLoad = null)
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.unscaledDeltaTime;
            float timeProgress = minLoadingTime > 0f ? Mathf.Clamp01(timer / minLoadingTime) : 1f;
            float loadProgress = asyncLoad != null ? Mathf.Clamp01(asyncLoad.progress / 0.9f) : timeProgress;
            ScreenFlowController.Instance?.ReportTransitionLoadingProgress(Mathf.Max(timeProgress, loadProgress));

            bool loadReady = asyncLoad == null || asyncLoad.progress >= 0.9f;
            bool timeReady = timer >= minLoadingTime;
            if (loadReady && timeReady)
                break;

            yield return null;
        }

        ScreenFlowController.Instance?.ReportTransitionLoadingProgress(1f);
    }

    private static void NotifyPreparationVisible()
    {
        PreparationScreenController[] controllers =
            Object.FindObjectsByType<PreparationScreenController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null || controllers[i].gameObject.scene.name != HubBaseScene)
                continue;

            controllers[i].RefreshFromHubNavigation();
        }
    }
}
