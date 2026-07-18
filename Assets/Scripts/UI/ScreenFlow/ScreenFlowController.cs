using System;
using System.Collections;
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
    private Coroutine _networkFadeInRoutine;
    private string _networkFadeInScene;
    private float _fadeTime = 1f;
    private float _minLoadingTime = 2f;

    public static void EnsureExists()
    {
        AspectLetterboxController.EnsureExists();
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
    }

    /// <summary>
    /// Registra tempos e loading legado da cena. O fade anima apenas o overlay DDOL built-in.
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
        {
            Debug.LogWarning($"ScreenFlowController: RequestScene('{sceneName}') bloqueado — controller inativo.");
            return false;
        }

        if (IsTransitioning)
        {
            Debug.LogWarning($"ScreenFlowController: RequestScene('{sceneName}') bloqueado — transição em andamento.");
            return false;
        }

        if (_activeSceneName == sceneName)
            return false;

        if (HubSceneNavigator.CanSkipTransition(sceneName))
            return false;

        if (TransitionFadeOverlay.EnsureExists() == null)
        {
            Debug.LogError("ScreenFlowController: TransitionFadeOverlay não encontrado.");
            return false;
        }

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
        // Rotas Loading1/Loading2 usam UI oficial da cena — sem painel DDOL de loading.
        if (useLoading && !dedicatedLoadingScene)
            TransitionFadeOverlay.Instance?.ShowLoading();

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

        bool enteringDedicatedLoading = ScreenFlowLoadingScenes.IsDedicatedLoadingScene(sceneName);

        if (useLoading)
        {
            TransitionCameraKeeper.EnsureActive();
            ScreenFlowSceneReadiness.BeginAwaiting(sceneName);
            if (!enteringDedicatedLoading)
                overlay.ShowLoading();
        }

        MusicCrossfadeController music = MusicCrossfadeController.Instance;
        // Cenas Loading1/Loading2 têm UI própria — sem fade antes de abrir.
        if (useFade && !enteringDedicatedLoading)
        {
            music?.HandleTransitionFadeOut(fadeTime);
            overlay.CancelFadeCoroutines();
            yield return overlay.FadeOut(fadeTime, delta =>
            {
                if (!useLoading)
                    return;

                loadTimer += delta;
                UpdateTransitionLoadingProgress(overlay, loadTimer, minLoadingTime);
            });
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

                yield return NetworkSceneSyncUtility.WaitForActiveScene(sceneName, fadeInOnArrival: false);
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
                // Single-player: não bloqueia ativação da cena — progresso acompanha o AsyncOperation.
                asyncLoad.allowSceneActivation = true;
                yield return WaitForLoadingProgress(overlay, minLoadingTime, loadTimer, asyncLoad);
            }

            yield return ScreenFlowSceneReadiness.WaitUntilLoadComplete(asyncLoad);

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

        if (useLoading)
            yield return ScreenFlowSceneReadiness.WaitUntilReady(sceneName);

        if (useLoading && !enteringDedicatedLoading)
            overlay.HideLoading();

        music = MusicCrossfadeController.Instance;
        music?.PrepareSceneMusic(SceneManager.GetActiveScene());

        if (enteringDedicatedLoading)
        {
            overlay.HandoffToDedicatedLoadingScene();
        }
        else if (useFade)
        {
            yield return ScreenFlowSceneReadiness.WaitForRenderPadding();
            music?.FadeInPending(fadeTime);
            overlay.CancelFadeCoroutines();
            yield return overlay.FadeIn(fadeTime);
        }
        else
        {
            music?.FadeInPending(defaultFadeTime);
            overlay.ResetFade();
        }
    }

    /// <summary>
    /// Limpa o overlay de transição (fade/loading). Não afeta pause (<see cref="SceneOverlayController"/>).
    /// Ignorado enquanto <see cref="IsTransitioning"/> — use <see cref="ForceClearTransitionOverlay"/> em recovery.
    /// </summary>
    public void ClearTransitionOverlay()
    {
        if (IsTransitioning)
            return;

        TransitionFadeOverlay.Instance?.ResetOverlay();
    }

    public void ForceClearTransitionOverlay() => TransitionFadeOverlay.Instance?.ResetOverlay();

    /// <summary>
    /// Clientes que recebem cena via NGO sem <see cref="RequestScene"/> local ainda precisam de fade-in
    /// quando o overlay ficou opaco (ex.: <see cref="NetworkSceneLoadingFeedback"/>).
    /// </summary>
    public IEnumerator TryFadeInAfterNetworkSceneArrival(string sceneName, float fadeTime = -1f)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        // Não abortar só porque outra transição local ainda marca IsTransitioning —
        // o fade NGO do cliente precisa concluir mesmo assim.
        if (SceneManager.GetActiveScene().name != sceneName)
            yield break;

        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        if (overlay == null)
            yield break;

        overlay.HideLoading();
        // Cancela fade-out em andamento (race: cena chega antes do alpha atingir 1).
        overlay.CancelFadeCoroutines();

        float ft = fadeTime > 0f ? fadeTime : (_fadeTime > 0f ? _fadeTime : defaultFadeTime);

        yield return ScreenFlowSceneReadiness.WaitUntilReady(sceneName);
        yield return ScreenFlowSceneReadiness.WaitForRenderPadding();

        if (overlay.GetFadeAlpha() <= 0.01f)
        {
            overlay.ResetFade();
            yield break;
        }

        MusicCrossfadeController music = MusicCrossfadeController.Instance;
        music?.PrepareSceneMusic(SceneManager.GetActiveScene());
        music?.FadeInPending(ft);
        overlay.CancelFadeCoroutines();
        yield return overlay.FadeIn(ft);
    }

    public void TryBeginFadeInAfterNetworkSceneArrival(string sceneName, float fadeTime = -1f)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (_networkFadeInRoutine != null && _networkFadeInScene == sceneName)
            return;

        // Se já há rotina para outra cena, substitui (ex.: Victory → Preparation rápido).
        if (_networkFadeInRoutine != null)
        {
            StopCoroutine(_networkFadeInRoutine);
            _networkFadeInRoutine = null;
            _networkFadeInScene = null;
        }

        _networkFadeInScene = sceneName;
        _networkFadeInRoutine = StartCoroutine(RunNetworkFadeInRoutine(sceneName, fadeTime));
    }

    private IEnumerator RunNetworkFadeInRoutine(string sceneName, float fadeTime)
    {
        yield return TryFadeInAfterNetworkSceneArrival(sceneName, fadeTime);
        _networkFadeInRoutine = null;
        _networkFadeInScene = null;
    }

    private void CompleteTransition(string sceneName)
    {
        IsTransitioning = false;
        TargetSceneName = null;
        _transitionRoutine = null;

        // ExecuteLoad já deixa o overlay no estado final (fade-in concluído ou handoff na loading).

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

    public bool TryGetRouteSceneName(string routeId, out string sceneName)
    {
        sceneName = null;
        EnsureCatalogLoaded();
        if (catalog == null || !catalog.TryGetRoute(routeId, out SceneFlowRouteDefinition route))
            return false;

        sceneName = route.sceneName;
        return !string.IsNullOrEmpty(sceneName);
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
    public bool IsFadeOpaque => GetFadeAlpha() >= 0.98f;

    public float GetFadeAlpha()
    {
        float alpha = _fadeImage != null ? _fadeImage.color.a : 0f;
        if (_legacyFadeImage != null)
            alpha = Mathf.Max(alpha, _legacyFadeImage.color.a);
        return alpha;
    }

    public event Action<bool> OnLoadingVisibilityChanged;

    private Canvas _canvas;
    private Image _fadeImage;

    private Image _legacyFadeImage;
    private Canvas _legacyCanvas;
    private string _legacySceneName;
    private Coroutine _animatedFadeOutRoutine;
    private Coroutine _fadeInRoutine;
    private bool _fadeRoutineRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBeforeSceneLoad()
    {
        EnsureExists();
    }

    /// <returns>Instância pronta ou null se a criação falhar.</returns>
    public static TransitionFadeOverlay EnsureExists()
    {
        if (Instance != null)
            return Instance;

        TransitionFadeOverlay existing = FindFirstObjectByType<TransitionFadeOverlay>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeInHierarchy)
                existing.gameObject.SetActive(true);

            if (Instance != null)
                return Instance;
        }

        var go = new GameObject(nameof(TransitionFadeOverlay));
        go.AddComponent<TransitionFadeOverlay>();
        return Instance;
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

    public void SetUseLegacyLoading(bool _) { }

    public void RegisterSceneVisuals(Image fadeImage, GameObject loadingScreen)
    {
        // loadingScreen legado ignorado — Loading1/Loading2 são as telas oficiais.
        _legacyFadeImage = fadeImage;

        if (_legacyFadeImage != null)
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
        _legacyCanvas = null;
        _legacySceneName = null;
    }

    private bool HasLegacyFadeImage() =>
        _legacyFadeImage != null && _legacyFadeImage.gameObject != null;

    private void EnsureLegacyFadeCanvasFront()
    {
        if (!HasLegacyFadeImage())
            return;

        if (_legacyCanvas == null)
            _legacyCanvas = _legacyFadeImage.GetComponentInParent<Canvas>(true);

        if (_legacyCanvas == null)
            return;

        if (!_legacyCanvas.gameObject.activeSelf)
            _legacyCanvas.gameObject.SetActive(true);

        _legacyCanvas.enabled = true;
        _legacyCanvas.overrideSorting = true;
        _legacyCanvas.sortingOrder = 32000;
    }

    private void ApplyFadeAlpha(float alpha, bool raycastWhenVisible)
    {
        alpha = Mathf.Clamp01(alpha);
        bool blockInput = raycastWhenVisible && alpha > 0.01f;

        EnsureOverlayBuilt();
        SetBuiltInOverlayVisible(true);

        if (_fadeImage != null)
        {
            LoadingProgressUtility.ApplySolidSprite(_fadeImage);
            Color builtIn = _fadeImage.color;
            builtIn.a = alpha;
            _fadeImage.color = builtIn;
            _fadeImage.raycastTarget = blockInput;
        }

        if (HasLegacyFadeImage())
        {
            LoadingProgressUtility.ApplySolidSprite(_legacyFadeImage);
            EnsureLegacyFadeCanvasFront();
            Color legacy = _legacyFadeImage.color;
            legacy.a = alpha;
            _legacyFadeImage.color = legacy;
            _legacyFadeImage.raycastTarget = blockInput;
        }
    }

    private void SetBuiltInOverlayVisible(bool visible)
    {
        if (_canvas == null)
            return;

        if (visible)
        {
            if (!_canvas.gameObject.activeInHierarchy)
                _canvas.gameObject.SetActive(true);

            _canvas.enabled = true;
            return;
        }

        _canvas.enabled = false;
    }

    public IEnumerator FadeOut(float duration, Action<float> onUnscaledTick = null)
    {
        if (duration <= 0f)
        {
            SetFadeImmediate(1f);
            yield break;
        }

        EnsureFadeReady();
        float startAlpha = GetFadeAlpha();
        float t = 0f;

        while (t < duration)
        {
            float delta = Time.unscaledDeltaTime;
            t += delta;
            onUnscaledTick?.Invoke(delta);
            ApplyFadeAlpha(Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(t / duration)), raycastWhenVisible: true);
            yield return null;
        }

        ApplyFadeAlpha(1f, raycastWhenVisible: true);
    }

    public IEnumerator FadeIn(float duration)
    {
        if (_fadeRoutineRunning)
            yield break;

        if (duration <= 0f)
        {
            SetFadeImmediate(0f);
            yield break;
        }

        float startAlpha = GetFadeAlpha();
        if (startAlpha <= 0.01f)
        {
            SetFadeImmediate(0f);
            yield break;
        }

        _fadeRoutineRunning = true;
        try
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                ApplyFadeAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / duration)), raycastWhenVisible: true);
                yield return null;
            }

            ApplyFadeAlpha(0f, raycastWhenVisible: false);
        }
        finally
        {
            _fadeRoutineRunning = false;
        }
    }

    /// <summary>
    /// Marca estado de loading para listeners. Sem painel visual DDOL —
    /// progresso oficial fica em Loading1/Loading2.
    /// </summary>
    public void ShowLoading()
    {
        EnsureOverlayBuilt();
        ResetLoadingProgress();
        SetBuiltInOverlayVisible(true);

        if (!IsLoadingVisible)
        {
            IsLoadingVisible = true;
            OnLoadingVisibilityChanged?.Invoke(true);
        }
    }

    public void HideLoading()
    {
        if (IsLoadingVisible)
        {
            IsLoadingVisible = false;
            OnLoadingVisibilityChanged?.Invoke(false);
        }
    }

    public void SetLoadingProgress(float progress)
    {
        LoadingProgress = Mathf.Clamp01(progress);
    }

    public void ResetLoadingProgress() => SetLoadingProgress(0f);

    public void HandoffToDedicatedLoadingScene(float progress = -1f)
    {
        CancelFadeCoroutines();
        HideLoading();

        // DDOL fade (sort 32767) cobriria a UI oficial — libera alpha.
        SetFadeImmediate(0f);

        if (progress >= 0f)
            SetLoadingProgress(progress);
    }

    public void ResetOverlay()
    {
        CancelFadeCoroutines();
        HideLoading();
        ResetFade();
    }

    public void ResetFade() => ApplyFadeAlpha(0f, raycastWhenVisible: false);

    public void SetFadeImmediate(float alpha)
    {
        CancelFadeCoroutines();
        ApplyFadeAlpha(alpha, raycastWhenVisible: alpha > 0.01f);
    }

    public void CancelFadeCoroutines()
    {
        if (_animatedFadeOutRoutine != null)
        {
            StopCoroutine(_animatedFadeOutRoutine);
            _animatedFadeOutRoutine = null;
        }

        if (_fadeInRoutine != null)
        {
            StopCoroutine(_fadeInRoutine);
            _fadeInRoutine = null;
        }

        _fadeRoutineRunning = false;
    }

    public void BeginAnimatedFadeOut(float duration)
    {
        CancelFadeCoroutines();

        if (duration <= 0f)
        {
            SetFadeImmediate(1f);
            return;
        }

        _animatedFadeOutRoutine = StartCoroutine(AnimatedFadeOutRoutine(duration));
    }

    private IEnumerator AnimatedFadeOutRoutine(float duration)
    {
        yield return FadeOut(duration);
        _animatedFadeOutRoutine = null;
    }

    public void BeginAnimatedFadeIn(float duration)
    {
        CancelFadeCoroutines();

        if (duration <= 0f)
        {
            SetFadeImmediate(0f);
            return;
        }

        _fadeInRoutine = StartCoroutine(AnimatedFadeInRoutine(duration));
    }

    private IEnumerator AnimatedFadeInRoutine(float duration)
    {
        yield return FadeIn(duration);
        _fadeInRoutine = null;
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
        if (_canvas.GetComponent<LetterboxExempt>() == null)
            _canvas.gameObject.AddComponent<LetterboxExempt>();

        GameObject fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeGo.transform.SetParent(_canvas.transform, false);
        RectTransform fadeRect = fadeGo.GetComponent<RectTransform>();
        ScreenFlowPlaceholderFactory.StretchFull(fadeRect);

        _fadeImage = fadeGo.GetComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(_fadeImage);
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;
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
