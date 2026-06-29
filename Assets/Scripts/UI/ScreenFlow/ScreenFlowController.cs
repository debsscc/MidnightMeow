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
/// Persiste entre cenas (Bootstrap). Visuais de fade/loading ficam em <see cref="TransitionFadeOverlay"/>.
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
    public bool IsLoadingScreenVisible => TransitionFadeOverlay.Instance != null && TransitionFadeOverlay.Instance.IsLoadingVisible;
    public float LoadingProgress => TransitionFadeOverlay.Instance != null ? TransitionFadeOverlay.Instance.LoadingProgress : 0f;
    public string TargetSceneName { get; private set; }
    public AsyncOperation CurrentAsyncLoad { get; private set; }

    public event Action<bool> OnLoadingScreenVisibilityChanged;

    private string _activeSceneName;
    private Coroutine _transitionRoutine;
    private float _fadeTime = 1f;
    private float _minLoadingTime = 2f;

    public static void EnsureExists()
    {
        TransitionFadeOverlay.EnsureExists();
        GameAudioSettings.EnsureExists();
        MusicCrossfadeController.EnsureExists();

        if (Instance != null)
        {
            Instance.EnsureCatalogLoaded();
            return;
        }

        var go = new GameObject(nameof(ScreenFlowController));
        var controller = go.AddComponent<ScreenFlowController>();
        controller.EnsureCatalogLoaded();
    }

    protected override void Awake()
    {
        _activeSceneName = SceneManager.GetActiveScene().name;
        base.Awake();
        TransitionFadeOverlay.EnsureExists();
        EnsureCatalogLoaded();
        BindOverlayEvents();
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
        UnbindOverlayEvents();
    }

    private void BindOverlayEvents()
    {
        if (TransitionFadeOverlay.Instance == null)
            return;

        TransitionFadeOverlay.Instance.OnLoadingVisibilityChanged -= HandleOverlayLoadingVisibilityChanged;
        TransitionFadeOverlay.Instance.OnLoadingVisibilityChanged += HandleOverlayLoadingVisibilityChanged;
    }

    private void UnbindOverlayEvents()
    {
        if (TransitionFadeOverlay.Instance == null)
            return;

        TransitionFadeOverlay.Instance.OnLoadingVisibilityChanged -= HandleOverlayLoadingVisibilityChanged;
    }

    private void HandleOverlayLoadingVisibilityChanged(bool visible)
    {
        OnLoadingScreenVisibilityChanged?.Invoke(visible);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _activeSceneName = scene.name;
        CurrentAsyncLoad = null;
        ScreenFlowSceneReadiness.MarkReadyIfPending(scene.name);

        if (scene.name.StartsWith("Fase-", StringComparison.Ordinal) || scene.name is "Game" or "Gameplay")
            ClearTransitionOverlay();
    }

    /// <summary>
    /// Chamado por <see cref="SceneTransition"/> na cena (ex.: Menu2) para tempos de fade/loading.
    /// </summary>
    public void RegisterSceneVisuals(UnityEngine.UI.Image fadeImage, GameObject loadingScreen, float fadeTime, float minLoadingTime)
    {
        if (fadeTime > 0f)
            _fadeTime = fadeTime;
        if (minLoadingTime > 0f)
            _minLoadingTime = minLoadingTime;

        TransitionFadeOverlay.Instance?.RegisterSceneVisuals(fadeImage, loadingScreen);
    }

    public void SetCatalog(SceneFlowCatalog flowCatalog) => catalog = flowCatalog;

    public void EnsureCatalogLoaded()
    {
        if (catalog != null)
            return;

        catalog = Resources.Load<SceneFlowCatalog>("ScreenFlowCatalog");
    }

    public bool RequestRoute(string routeId, ScreenTransitionMode modeOverride = ScreenTransitionMode.UseRouteDefault)
    {
        EnsureCatalogLoaded();

        if (routeId == SceneFlowRouteIds.MenuToLobby)
        {
            ScreenFlowStateMachine.EnterPhase(ScreenFlowPhase.Lobby);
            GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToLobby;
        }

        if (catalog == null || !catalog.TryGetRoute(routeId, out SceneFlowRouteDefinition route))
        {
            Debug.LogError($"ScreenFlowController: rota '{routeId}' não encontrada no catálogo.");
            return false;
        }

        ScreenTransitionMode mode = ResolveMode(modeOverride, route.transitionMode);
        return RequestScene(route.sceneName, mode, ResolveEffectiveLoadKind(route.loadKind), route.fadeTime, route.minLoadingTime);
    }

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

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            return SceneLoadKind.NetcodeHost;

        if (GameSessionContext.IsSinglePlayer)
            return SceneLoadKind.SinglePlayer;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
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

        TransitionFadeOverlay.EnsureExists();

        float ft = fadeTime > 0f ? fadeTime : (_fadeTime > 0f ? _fadeTime : defaultFadeTime);
        float ml = minLoadingTime > 0f ? minLoadingTime : (_minLoadingTime > 0f ? _minLoadingTime : defaultMinLoadingTime);

        bool dedicatedLoadingScene = ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName);
        if (dedicatedLoadingScene)
            ml = 0f;

        IsTransitioning = true;
        TargetSceneName = sceneName;
        onAnyTransitionStarted?.Invoke();
        OnTransitionStarted?.Invoke(sceneName);

        bool useLoading = ResolveUsesLoadingScreen(mode);
        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        overlay?.SetUseLegacyLoading(!dedicatedLoadingScene);
        if (useLoading)
            overlay?.ShowLoading();

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

    private static bool ResolveUsesLoadingScreen(ScreenTransitionMode mode) =>
        mode == ScreenTransitionMode.LoadingScreen;

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
        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        if (overlay == null)
        {
            Debug.LogError("ScreenFlowController: TransitionFadeOverlay não encontrado.");
            yield break;
        }

        bool loadSucceeded = false;
        float loadTimer = 0f;

        if (useLoading)
        {
            TransitionCameraKeeper.EnsureActive();
            ScreenFlowSceneReadiness.BeginAwaiting(sceneName);
            overlay.ShowLoading();
        }

        bool instantLoadingFeedback = useLoading && ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName);
        MusicCrossfadeController music = MusicCrossfadeController.Instance;
        if (useFade && !instantLoadingFeedback)
        {
            music?.HandleTransitionFadeOut(fadeTime);
            yield return overlay.FadeOut(fadeTime, delta =>
            {
                if (!useLoading)
                    return;

                loadTimer += delta;
                UpdateTransitionLoadingProgress(overlay, loadTimer, minLoadingTime);
            });
        }
        else if (useFade && instantLoadingFeedback)
        {
            float quickFade = fadeTime > 0f ? fadeTime : defaultFadeTime;
            music?.HandleTransitionFadeOut(quickFade);
            overlay.SetFadeImmediate(1f);
        }

        if (loadKind == SceneLoadKind.NetcodeHost)
        {
            NetworkManager net = NetworkManager.Singleton;
            if (net == null || !net.IsListening)
            {
                Debug.LogWarning($"ScreenFlowController: NetcodeHost sem rede ativa para '{sceneName}'.");
                ScreenFlowSceneReadiness.CancelAwaiting();
                overlay.ResetOverlay();
                ConnectionManager.Instance?.BeginLobbyRecoveryAfterNetworkFailure(
                    "Conexão perdida. Voltando ao lobby...");
                yield break;
            }

            if (!net.IsServer)
            {
                if (useLoading)
                    yield return WaitForLoadingProgress(overlay, minLoadingTime, loadTimer);

                yield return NetworkSceneSyncUtility.WaitForActiveScene(sceneName);
                loadSucceeded = SceneManager.GetActiveScene().name == sceneName;
            }
            else
            {
                net.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                CurrentAsyncLoad = null;

                if (useLoading)
                {
                    bool skipOverlayTimer = ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName);
                    while (SceneManager.GetActiveScene().name != sceneName
                           || (!skipOverlayTimer && loadTimer < minLoadingTime))
                    {
                        loadTimer += Time.unscaledDeltaTime;
                        float timeProgress = minLoadingTime > 0f ? Mathf.Clamp01(loadTimer / minLoadingTime) : 1f;
                        float sceneProgress = SceneManager.GetActiveScene().name == sceneName ? 1f : 0.5f;
                        overlay.SetLoadingProgress(Mathf.Max(timeProgress, sceneProgress));
                        yield return null;
                    }

                    overlay.SetLoadingProgress(1f);
                }
                else
                {
                    while (SceneManager.GetActiveScene().name != sceneName)
                        yield return null;
                }

                loadSucceeded = true;
            }
        }
        else if (HubSceneNavigator.ShouldUseAdditiveNavigation(sceneName, loadKind))
        {
            yield return HubSceneNavigator.RunAdditiveTransition(sceneName, minLoadingTime, useLoading);
            _activeSceneName = sceneName;
            loadSucceeded = true;
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            CurrentAsyncLoad = asyncLoad;

            if (asyncLoad == null)
            {
                Debug.LogError($"ScreenFlowController: falha ao carregar '{sceneName}'.");
                ScreenFlowSceneReadiness.CancelAwaiting();
                overlay.ResetOverlay();
                yield break;
            }

            if (useLoading)
            {
                asyncLoad.allowSceneActivation = false;
                yield return WaitForLoadingProgress(overlay, minLoadingTime, loadTimer, asyncLoad);
                asyncLoad.allowSceneActivation = true;
            }

            while (!asyncLoad.isDone)
                yield return null;

            CurrentAsyncLoad = null;
            loadSucceeded = true;
        }

        if (!loadSucceeded)
        {
            ScreenFlowSceneReadiness.CancelAwaiting();
            overlay.ResetOverlay();
            yield break;
        }

        TransitionCameraKeeper.EnsureActive();
        yield return null;

        if (useLoading)
            yield return ScreenFlowSceneReadiness.WaitUntilReady(sceneName);

        bool handoffToDedicatedLoadingScene = useLoading && ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName);
        if (useLoading && !handoffToDedicatedLoadingScene)
            overlay.HideLoading();

        music = MusicCrossfadeController.Instance;
        music?.PrepareSceneMusic(SceneManager.GetActiveScene());

        if (useFade && !handoffToDedicatedLoadingScene)
        {
            music?.FadeInPending(fadeTime);
            yield return overlay.FadeIn(fadeTime);
        }
        else if (!handoffToDedicatedLoadingScene)
        {
            music?.FadeInPending(defaultFadeTime);
            overlay.ResetFade();
        }
        else
        {
            music?.FadeInPending(fadeTime > 0f ? fadeTime : defaultFadeTime);
        }
    }

    public void ClearTransitionOverlay()
    {
        TransitionFadeOverlay.Instance?.ResetOverlay();
    }

    private void CompleteTransition(string sceneName)
    {
        IsTransitioning = false;
        TargetSceneName = null;
        _transitionRoutine = null;

        if (sceneName is not ("Loading1" or "Loading2"))
            ClearTransitionOverlay();

        onAnyTransitionCompleted?.Invoke();
        OnTransitionCompleted?.Invoke(sceneName);
    }

    public void ReportTransitionLoadingProgress(float progress)
    {
        if (!IsLoadingScreenVisible)
            return;

        TransitionFadeOverlay.Instance?.SetLoadingProgress(progress);
    }

    private static void UpdateTransitionLoadingProgress(
        TransitionFadeOverlay overlay,
        float loadTimer,
        float minLoadingTime,
        AsyncOperation asyncLoad = null)
    {
        float timeProgress = minLoadingTime > 0f ? Mathf.Clamp01(loadTimer / minLoadingTime) : 1f;
        float loadProgress = asyncLoad != null ? Mathf.Clamp01(asyncLoad.progress / 0.9f) : 0f;
        overlay.SetLoadingProgress(Mathf.Max(timeProgress, loadProgress));
    }

    private static IEnumerator WaitForLoadingProgress(
        TransitionFadeOverlay overlay,
        float minLoadingTime,
        float loadTimerStart = 0f,
        AsyncOperation asyncLoad = null)
    {
        float loadTimer = loadTimerStart;
        while (true)
        {
            loadTimer += Time.unscaledDeltaTime;
            UpdateTransitionLoadingProgress(overlay, loadTimer, minLoadingTime, asyncLoad);

            bool loadReady = asyncLoad == null || asyncLoad.progress >= 0.9f;
            bool timeReady = loadTimer >= minLoadingTime;
            if (loadReady && timeReady)
                break;

            yield return null;
        }

        overlay.SetLoadingProgress(1f);
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

/// <summary>
/// Overlay persistente (fade + loading) para todas as transições de cena.
/// Singleton DDOL — inicializado no Bootstrap ou ao abrir Menu2.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class TransitionFadeOverlay : Singleton<TransitionFadeOverlay>
{
    public bool IsLoadingVisible { get; private set; }
    public float LoadingProgress { get; private set; }
    public bool IsFadeOpaque => _fadeImage != null && _fadeImage.color.a >= 0.98f;

    public event Action<bool> OnLoadingVisibilityChanged;

    private Canvas _canvas;
    private Image _fadeImage;
    private GameObject _loadingRoot;
    private Image _progressFill;
    private TMP_Text _statusText;

    private Image _legacyFadeImage;
    private GameObject _legacyLoadingScreen;
    private Canvas _legacyCanvas;
    private string _legacySceneName;
    private bool _useLegacyLoading = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBeforeSceneLoad()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        TransitionFadeOverlay existing = FindFirstObjectByType<TransitionFadeOverlay>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var go = new GameObject(nameof(TransitionFadeOverlay));
        go.AddComponent<TransitionFadeOverlay>();
    }

    protected override void Awake()
    {
        base.Awake();
        BuildOverlay();
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    public void SetUseLegacyLoading(bool useLegacy) => _useLegacyLoading = useLegacy;

    public void RegisterSceneVisuals(Image fadeImage, GameObject loadingScreen)
    {
        _legacyFadeImage = fadeImage;
        _legacyLoadingScreen = loadingScreen;

        if (_legacyLoadingScreen != null)
        {
            _legacyCanvas = _legacyLoadingScreen.GetComponentInParent<Canvas>(true);
            _legacySceneName = _legacyLoadingScreen.scene.name;
        }
        else if (_legacyFadeImage != null)
        {
            _legacyCanvas = _legacyFadeImage.GetComponentInParent<Canvas>(true);
            _legacySceneName = _legacyFadeImage.gameObject.scene.name;
        }
        else
        {
            _legacyCanvas = null;
            _legacySceneName = null;
        }
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (string.IsNullOrEmpty(_legacySceneName) || scene.name != _legacySceneName)
            return;

        ClearLegacyVisuals();
    }

    private void ClearLegacyVisuals()
    {
        _legacyFadeImage = null;
        _legacyLoadingScreen = null;
        _legacyCanvas = null;
        _legacySceneName = null;
    }

    private bool HasValidLegacyVisuals()
    {
        if (_legacyLoadingScreen != null)
            return true;

        if (_legacyFadeImage != null || _legacyCanvas != null || !string.IsNullOrEmpty(_legacySceneName))
            ClearLegacyVisuals();

        return false;
    }

    private Image GetActiveFadeImage()
    {
        EnsureOverlayBuilt();
        return _fadeImage;
    }

    private void EnsureLegacyCanvasFront()
    {
        if (!HasValidLegacyVisuals())
            return;

        if (_legacyCanvas == null)
            return;

        if (!_legacyCanvas.gameObject.activeSelf)
            _legacyCanvas.gameObject.SetActive(true);

        _legacyCanvas.enabled = true;
        _legacyCanvas.overrideSorting = true;
        _legacyCanvas.sortingOrder = 15000;
    }

    private void SetBuiltInOverlayVisible(bool visible)
    {
        if (_canvas == null)
            return;

        _canvas.enabled = visible;
        if (!visible && _loadingRoot != null)
            _loadingRoot.SetActive(false);
    }

    public IEnumerator FadeOut(float duration, Action<float> onUnscaledTick = null)
    {
        EnsureFadeReady();
        SetBuiltInOverlayVisible(true);
        EnsureLegacyCanvasFront();

        Image fadeTarget = GetActiveFadeImage();
        if (fadeTarget == null)
            yield break;

        fadeTarget.raycastTarget = true;

        float t = 0f;
        Color c = fadeTarget.color;
        while (t < duration)
        {
            float delta = Time.unscaledDeltaTime;
            t += delta;
            onUnscaledTick?.Invoke(delta);
            c.a = Mathf.Clamp01(t / duration);
            fadeTarget.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeTarget.color = c;
    }

    public IEnumerator FadeIn(float duration)
    {
        SetBuiltInOverlayVisible(true);

        Image fadeTarget = GetActiveFadeImage();
        if (fadeTarget == null)
            yield break;

        float t = duration;
        Color c = fadeTarget.color;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fadeTarget.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeTarget.color = c;
        fadeTarget.raycastTarget = false;
    }

    public void ShowLoading()
    {
        EnsureOverlayBuilt();
        ResetLoadingProgress();

        SetBuiltInOverlayVisible(true);

        if (_fadeImage != null)
            _fadeImage.transform.SetAsFirstSibling();

        _loadingRoot.SetActive(true);
        _loadingRoot.transform.SetAsLastSibling();

        if (_useLegacyLoading && HasValidLegacyVisuals())
        {
            EnsureLegacyCanvasFront();
            _legacyLoadingScreen.SetActive(true);
            _legacyLoadingScreen.transform.SetAsLastSibling();
        }
        else if (_legacyLoadingScreen != null)
            _legacyLoadingScreen.SetActive(false);

        if (!IsLoadingVisible)
        {
            IsLoadingVisible = true;
            OnLoadingVisibilityChanged?.Invoke(true);
        }

        SetLoadingProgress(0.04f);
    }

    public void HideLoading()
    {
        if (_legacyLoadingScreen != null)
            _legacyLoadingScreen.SetActive(false);
        else
            ClearLegacyVisuals();

        if (_loadingRoot != null)
            _loadingRoot.SetActive(false);

        if (IsLoadingVisible)
        {
            IsLoadingVisible = false;
            OnLoadingVisibilityChanged?.Invoke(false);
        }
    }

    public void SetLoadingProgress(float progress)
    {
        LoadingProgress = Mathf.Clamp01(progress);

        if (_progressFill != null)
            LoadingProgressUtility.SetProgress(_progressFill, LoadingProgress);

        if (_statusText != null)
            _statusText.text = LocaleText.IsPortuguese()
                ? $"Carregando... {LoadingProgress:P0}"
                : $"Loading... {LoadingProgress:P0}";
    }

    public void ResetLoadingProgress() => SetLoadingProgress(0f);

    public void HandoffToDedicatedLoadingScene(float progress = -1f)
    {
        HideLoading();
        SetFadeImmediate(0f);

        if (progress >= 0f)
            SetLoadingProgress(progress);
    }

    public void ResetOverlay()
    {
        HideLoading();
        ResetFade();
        _useLegacyLoading = true;
    }

    public void ResetFade()
    {
        Image fadeTarget = GetActiveFadeImage();
        if (fadeTarget == null)
            return;

        SetFadeImmediate(0f);

        if (_fadeImage != null && _fadeImage != fadeTarget)
            ResetFadeImage(_fadeImage);
    }

    public void SetFadeImmediate(float alpha)
    {
        Image fadeTarget = GetActiveFadeImage();
        if (fadeTarget == null)
            return;

        Color c = fadeTarget.color;
        c.a = Mathf.Clamp01(alpha);
        fadeTarget.color = c;
        fadeTarget.raycastTarget = c.a > 0.01f;
    }

    private static void ResetFadeImage(Image image)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = 0f;
        image.color = c;
        image.raycastTarget = false;
    }

    private void BuildOverlay()
    {
        if (_fadeImage != null)
            return;

        GameObject root = new GameObject("OverlayRoot");
        root.transform.SetParent(transform, false);

        _canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(root.transform, "TransitionOverlay");
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 32767;

        GameObject fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeGo.transform.SetParent(_canvas.transform, false);
        RectTransform fadeRect = fadeGo.GetComponent<RectTransform>();
        ScreenFlowPlaceholderFactory.StretchFull(fadeRect);

        _fadeImage = fadeGo.GetComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(_fadeImage);
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        _loadingRoot = ScreenFlowPlaceholderFactory.CreatePanel(
            _canvas.transform, "Loading", new Color(0.04f, 0.05f, 0.1f, 0.98f));
        if (_loadingRoot.TryGetComponent(out Image loadingBackground))
            LoadingProgressUtility.ApplySolidSprite(loadingBackground);

        _statusText = ScreenFlowPlaceholderFactory.CreateText(_loadingRoot.transform, "Carregando... 0%",
            48, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-300f, LoadingProgressUtility.BottomStatusTextMinY),
            new Vector2(300f, LoadingProgressUtility.BottomStatusTextMaxY));
        _progressFill = LoadingProgressUtility.CreateBottomProgressBar(_loadingRoot.transform);
        _loadingRoot.SetActive(false);
    }

    private void EnsureOverlayBuilt()
    {
        if (_fadeImage == null)
            BuildOverlay();
    }

    private void EnsureFadeReady()
    {
        EnsureOverlayBuilt();
        LoadingProgressUtility.ApplySolidSprite(_fadeImage);
    }
}
