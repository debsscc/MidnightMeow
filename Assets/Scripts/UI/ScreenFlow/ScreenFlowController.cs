using System;
using System.Collections;
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
    private float _fadeTime = 1f;
    private float _minLoadingTime = 2f;

    protected override void Awake()
    {
        _activeSceneName = SceneManager.GetActiveScene().name;
        base.Awake();
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
        IsTransitioning = false;
        TargetSceneName = null;
        _transitionRoutine = null;
        CurrentAsyncLoad = null;

        if (_loadingScreen != null)
            _loadingScreen.SetActive(false);
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
        return RequestScene(route.sceneName, mode, route.loadKind, route.fadeTime, route.minLoadingTime);
    }

    /// <summary>
    /// API legada compatível com <see cref="SceneTransition.TryBeginTransition"/>.
    /// </summary>
    public bool TryBeginTransition(string sceneName, ScreenTransitionMode mode = ScreenTransitionMode.Fade)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (catalog != null && TryResolveRouteForScene(sceneName, out SceneFlowRouteDefinition route))
            return RequestScene(sceneName, ResolveMode(mode, route.transitionMode), route.loadKind, route.fadeTime, route.minLoadingTime);

        return RequestScene(sceneName, mode, SceneLoadKind.SinglePlayer, _fadeTime, _minLoadingTime);
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

        for (int i = 0; i < catalog.routes.Length; i++)
        {
            if (catalog.routes[i] != null && catalog.routes[i].sceneName == sceneName)
            {
                route = catalog.routes[i];
                return true;
            }
        }

        return false;
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
        if (useFade && _fadeImage != null)
            yield return FadeOut(fadeTime);

        if (useLoading && _loadingScreen != null)
            _loadingScreen.SetActive(true);

        if (loadKind == SceneLoadKind.NetcodeHost)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("ScreenFlowController: NetcodeHost exige host. Carga ignorada.");
                if (_loadingScreen != null)
                    _loadingScreen.SetActive(false);
                yield break;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            CurrentAsyncLoad = null;

            while (SceneManager.GetActiveScene().name != sceneName)
                yield return null;
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            CurrentAsyncLoad = asyncLoad;

            if (asyncLoad == null)
            {
                Debug.LogError($"ScreenFlowController: falha ao carregar '{sceneName}'.");
                CurrentAsyncLoad = null;
                if (_loadingScreen != null)
                    _loadingScreen.SetActive(false);
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

                if (_loadingScreen != null)
                    _loadingScreen.SetActive(false);

                asyncLoad.allowSceneActivation = true;
            }

            while (!asyncLoad.isDone)
                yield return null;

            CurrentAsyncLoad = null;
        }

        if (useFade && _fadeImage != null)
            yield return FadeIn(fadeTime);
    }

    private void CompleteTransition(string sceneName)
    {
        IsTransitioning = false;
        _transitionRoutine = null;
        onAnyTransitionCompleted?.Invoke();
        OnTransitionCompleted?.Invoke(sceneName);
    }

    private IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        Color c = _fadeImage.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            _fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        _fadeImage.color = c;
    }

    private IEnumerator FadeIn(float duration)
    {
        float t = duration;
        Color c = _fadeImage.color;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            _fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        _fadeImage.color = c;
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
