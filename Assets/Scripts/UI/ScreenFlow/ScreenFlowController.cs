using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ponto único para troca de cena (fade, loading, instantâneo, Netcode).
/// Persiste entre cenas (Bootstrap). Designers usam <see cref="ScreenFlowRequest"/> ou rotas pelo ID.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFlowController : Singleton<ScreenFlowController>
{
    [Header("Catálogo")]
    [SerializeField] private SceneFlowCatalog catalog;

    [Header("Padrões (quando a cena não registra visuais)")]
    [SerializeField] private float defaultFadeTime = 1f;
    [SerializeField] private float defaultMinLoadingTime = 2f;

    [Header("Eventos globais (Inspector)")]
    public UnityEvent onAnyTransitionStarted;
    public UnityEvent onAnyTransitionCompleted;

    public event Action<string> OnTransitionStarted;
    public event Action<string> OnTransitionCompleted;

    public bool IsTransitioning { get; private set; }
    public string TargetSceneName { get; private set; }
    public AsyncOperation CurrentAsyncLoad { get; private set; }

    private string _activeSceneName;
    private Coroutine _transitionRoutine;

    private Image _fadeImage;
    private GameObject _loadingScreen;
    private Image _builtInFadeImage;
    private GameObject _builtInLoadingScreen;
    private float _fadeTime = 1f;
    private float _minLoadingTime = 2f;

    protected override void Awake()
    {
        _activeSceneName = SceneManager.GetActiveScene().name;
        base.Awake();
        EnsureBuiltInTransitionVisuals();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        try
        {
            ServiceLocator.RegisterService<ScreenFlowController>(this);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ScreenFlowController: ServiceLocator: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _activeSceneName = scene.name;
        CurrentAsyncLoad = null;

        // Refs da cena anterior deixam de ser válidas; o overlay persistente segue no controller.
        _fadeImage = null;
        _loadingScreen = null;
        SetLoadingScreenActive(false);

        if (scene.name.StartsWith("Fase-", System.StringComparison.Ordinal) || scene.name is "Game" or "Gameplay")
            ClearTransitionOverlay();
    }

    /// <summary>
    /// Chamado por <see cref="SceneTransition"/> na cena (ex.: Menu2) para reutilizar fade/loading do Canvas.
    /// </summary>
    public void RegisterSceneVisuals(Image fadeImage, GameObject loadingScreen, float fadeTime, float minLoadingTime)
    {
        if (fadeImage != null)
            _fadeImage = fadeImage;
        if (loadingScreen != null)
            _loadingScreen = loadingScreen;
        if (fadeTime > 0f)
            _fadeTime = fadeTime;
        if (minLoadingTime > 0f)
            _minLoadingTime = minLoadingTime;
    }

    public void SetCatalog(SceneFlowCatalog flowCatalog) => catalog = flowCatalog;

    public bool RequestRoute(string routeId, ScreenTransitionMode modeOverride = ScreenTransitionMode.UseRouteDefault)
    {
        if (catalog == null || !catalog.TryGetRoute(routeId, out SceneFlowRouteDefinition route))
        {
            Debug.LogError($"ScreenFlowController: rota '{routeId}' não encontrada no catálogo.");
            return false;
        }

        ScreenTransitionMode mode = ResolveMode(modeOverride, route.transitionMode);
        return RequestScene(route.sceneName, mode, ResolveEffectiveLoadKind(route.loadKind), route.fadeTime, route.minLoadingTime);
    }

    /// <summary>
    /// API legada compatível com <see cref="SceneTransition.TryBeginTransition"/>.
    /// </summary>
    public bool TryBeginTransition(string sceneName, ScreenTransitionMode mode = ScreenTransitionMode.Fade)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (catalog != null && TryResolveRouteForScene(sceneName, out SceneFlowRouteDefinition route))
            return RequestScene(sceneName, ResolveMode(mode, route.transitionMode), ResolveEffectiveLoadKind(route.loadKind), route.fadeTime, route.minLoadingTime);

        return RequestScene(sceneName, mode, SceneLoadKind.SinglePlayer, _fadeTime, _minLoadingTime);
    }

    private static SceneLoadKind ResolveEffectiveLoadKind(SceneLoadKind loadKind)
    {
        if (loadKind != SceneLoadKind.NetcodeHost)
            return loadKind;

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
            return SceneLoadKind.NetcodeHost;

        if (GameSessionContext.IsSinglePlayer)
            return SceneLoadKind.SinglePlayer;

        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening)
            return SceneLoadKind.SinglePlayer;

        return SceneLoadKind.NetcodeHost;
    }

    public bool RequestScene(
        string sceneName,
        ScreenTransitionMode mode,
        SceneLoadKind loadKind,
        float fadeTime = -1f,
        float minLoadingTime = -1f)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            return false;

        if (IsTransitioning)
            return false;

        if (_activeSceneName == sceneName)
            return false;

        if (HubSceneNavigator.CanSkipTransition(sceneName))
            return false;

        float ft = fadeTime > 0f ? fadeTime : (_fadeTime > 0f ? _fadeTime : defaultFadeTime);
        float ml = minLoadingTime > 0f ? minLoadingTime : (_minLoadingTime > 0f ? _minLoadingTime : defaultMinLoadingTime);

        IsTransitioning = true;
        TargetSceneName = sceneName;
        onAnyTransitionStarted?.Invoke();
        OnTransitionStarted?.Invoke(sceneName);

        _transitionRoutine = StartCoroutine(RunTransition(sceneName, mode, loadKind, ft, ml));
        return true;
    }

    private bool TryResolveRouteForScene(string sceneName, out SceneFlowRouteDefinition route)
    {
        route = null;
        if (catalog?.routes == null)
            return false;

        SceneFlowRouteDefinition preferred = null;
        SceneFlowRouteDefinition fallback = null;

        for (int i = 0; i < catalog.routes.Length; i++)
        {
            SceneFlowRouteDefinition candidate = catalog.routes[i];
            if (candidate == null || candidate.sceneName != sceneName)
                continue;

            if (candidate.routeId == SceneFlowRouteIds.LobbyToGameplay)
                continue;

            if (candidate.routeId == SceneFlowRouteIds.Loading2ToGameplay)
                preferred = candidate;
            else
                fallback = candidate;
        }

        route = preferred ?? fallback;
        return route != null;
    }

    private static ScreenTransitionMode ResolveMode(ScreenTransitionMode overrideMode, ScreenTransitionMode routeDefault)
    {
        return overrideMode == ScreenTransitionMode.UseRouteDefault ? routeDefault : overrideMode;
    }

    private IEnumerator RunTransition(string sceneName, ScreenTransitionMode mode, SceneLoadKind loadKind, float fadeTime, float minLoadingTime)
    {
        Time.timeScale = 1f;

        switch (mode)
        {
            case ScreenTransitionMode.Instant:
                yield return ExecuteLoad(sceneName, loadKind, fadeTime, minLoadingTime, useFade: false, useLoading: false);
                break;
            case ScreenTransitionMode.Fade:
                yield return ExecuteLoad(sceneName, loadKind, fadeTime, minLoadingTime, useFade: true, useLoading: false);
                break;
            case ScreenTransitionMode.LoadingScreen:
                yield return ExecuteLoad(sceneName, loadKind, fadeTime, minLoadingTime, useFade: true, useLoading: true);
                break;
            default:
                yield return ExecuteLoad(sceneName, loadKind, fadeTime, minLoadingTime, useFade: true, useLoading: false);
                break;
        }

        CompleteTransition(sceneName);
    }

    private IEnumerator ExecuteLoad(string sceneName, SceneLoadKind loadKind, float fadeTime, float minLoadingTime, bool useFade, bool useLoading)
    {
        bool loadSucceeded = false;

        try
        {
            Image fade = ResolveFadeImage();
            if (useFade && fade != null)
                yield return FadeOut(fade, fadeTime);

            if (useLoading)
                SetLoadingScreenActive(true);

            if (loadKind == SceneLoadKind.NetcodeHost)
            {
                NetworkManager net = NetworkManager.Singleton;
                if (net == null || !net.IsListening)
                {
                    Debug.LogWarning($"ScreenFlowController: NetcodeHost sem rede ativa para '{sceneName}'.");
                    yield break;
                }

                if (!net.IsServer)
                {
                    yield return NetworkSceneSyncUtility.WaitForActiveScene(sceneName);
                    loadSucceeded = SceneManager.GetActiveScene().name == sceneName;
                }
                else
                {
                    net.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    CurrentAsyncLoad = null;

                    while (SceneManager.GetActiveScene().name != sceneName)
                        yield return null;

                    loadSucceeded = true;
                }
            }
            else if (HubSceneNavigator.ShouldUseAdditiveNavigation(sceneName, loadKind))
            {
                yield return HubSceneNavigator.RunAdditiveTransition(sceneName, minLoadingTime, useLoading);
                _activeSceneName = sceneName;
                SetLoadingScreenActive(false);
                loadSucceeded = true;
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                CurrentAsyncLoad = asyncLoad;

                if (asyncLoad == null)
                {
                    Debug.LogError($"ScreenFlowController: falha ao carregar '{sceneName}'.");
                    yield break;
                }

                if (useLoading)
                {
                    asyncLoad.allowSceneActivation = false;
                    float loadTimer = 0f;
                    while (asyncLoad.progress < 0.9f || loadTimer < minLoadingTime)
                    {
                        loadTimer += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    SetLoadingScreenActive(false);
                    asyncLoad.allowSceneActivation = true;
                }

                while (!asyncLoad.isDone)
                    yield return null;

                CurrentAsyncLoad = null;
                loadSucceeded = true;
            }

            fade = ResolveFadeImage();
            if (loadSucceeded && useFade && fade != null)
                yield return FadeIn(fade, fadeTime);
        }
        finally
        {
            ClearTransitionOverlay();
        }
    }

    /// <summary>
    /// Garante que fade/loading persistentes não bloqueiem a UI da cena carregada.
    /// </summary>
    public void ClearTransitionOverlay()
    {
        SetLoadingScreenActive(false);
        ResetBuiltInFade();

        if (_builtInFadeImage != null)
            _builtInFadeImage.raycastTarget = false;
    }

    private void CompleteTransition(string sceneName)
    {
        IsTransitioning = false;
        TargetSceneName = null;
        _transitionRoutine = null;
        ClearTransitionOverlay();
        onAnyTransitionCompleted?.Invoke();
        OnTransitionCompleted?.Invoke(sceneName);
    }

    private void EnsureBuiltInTransitionVisuals()
    {
        if (_builtInFadeImage != null)
            return;

        GameObject overlayRoot = new GameObject("ScreenFlowTransitionOverlay");
        overlayRoot.transform.SetParent(transform, false);

        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(overlayRoot.transform, "TransitionOverlay");
        canvas.sortingOrder = 500;

        GameObject fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeGo.transform.SetParent(canvas.transform, false);
        RectTransform fadeRect = fadeGo.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        _builtInFadeImage = fadeGo.GetComponent<Image>();
        _builtInFadeImage.color = new Color(0f, 0f, 0f, 0f);
        _builtInFadeImage.raycastTarget = true;

        _builtInLoadingScreen = ScreenFlowPlaceholderFactory.CreatePanel(
            canvas.transform, "BuiltInLoading", new Color(0.04f, 0.05f, 0.1f, 0.98f));
        ScreenFlowPlaceholderFactory.CreateText(_builtInLoadingScreen.transform, "Carregando...",
            48, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -40f), new Vector2(300f, 40f));
        _builtInLoadingScreen.SetActive(false);
    }

    private Image ResolveFadeImage() => _fadeImage != null ? _fadeImage : _builtInFadeImage;

    private void SetLoadingScreenActive(bool active)
    {
        if (_loadingScreen != null)
            _loadingScreen.SetActive(active);
        else if (_builtInLoadingScreen != null)
            _builtInLoadingScreen.SetActive(active);
    }

    private void ResetBuiltInFade()
    {
        if (_builtInFadeImage == null)
            return;

        Color c = _builtInFadeImage.color;
        c.a = 0f;
        _builtInFadeImage.color = c;
    }

    private static IEnumerator FadeOut(Image fadeImage, float duration)
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    private static IEnumerator FadeIn(Image fadeImage, float duration)
    {
        float t = duration;
        Color c = fadeImage.color;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }

    public bool TryGetRouteLoadKind(string routeId, out SceneLoadKind loadKind)
    {
        loadKind = SceneLoadKind.SinglePlayer;
        if (catalog == null || !catalog.TryGetRoute(routeId, out SceneFlowRouteDefinition route))
            return false;

        loadKind = route.loadKind;
        return true;
    }

    public void ChangeScene(string sceneName) => TryBeginTransition(sceneName);
}
