/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Overlay global de créditos — rolagem automática, trilha e fundo opaco.
---------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public class CreditsOverlayController : Singleton<CreditsOverlayController>
{
    private const string CreditsBodyResourcePath = "CreditsBody";
    private const string CreditsBodyResourcePathEn = "CreditsBody_en";
    private const string CreditsMusicResourcePath = "CreditsMusicClip";
    private const string CreditsMusicAudioResourcePath = "Audio/CreditsMusic";
    private const string CreditsVisualConfigResourcePath = "CreditsVisualConfig";
    private const string MenuUiAmbienceResourcePath = "UI/MenuUiAmbience";
    private const float DefaultBodyWidthNormalized = 0.38f;

    private static readonly Regex TitleSizeBlockRegex = new(
        @"<size=(\d+%)>([\s\S]*?)</size>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Header("Rolagem")]
    [SerializeField] private float scrollSpeedPixelsPerSecond = 55f;

    [Header("Fim (padrão menu/pause)")]
    [SerializeField] private CreditsPresentationConfig defaultPresentation = CreditsPresentationConfig.DefaultMenu;

    [Header("Visual")]
    [SerializeField] private CreditsVisualConfig visualConfig;

    [Header("Áudio (opcional)")]
    [SerializeField] private AudioClip creditsMusic;

    private Canvas _canvas;
    private GameObject _panel;
    private CanvasGroup _panelGroup;
    private ScrollRect _scrollRect;
    private RectTransform _topSpacer;
    private RectTransform _bottomSpacer;
    private TMP_Text _bodyText;
    private Button _closeButton;
    private bool _overlayBuilt;
    private bool _isScrolling;
    private float _endNormalizedScroll = 0f;
    private CreditsPresentationConfig _activePresentation;
    private Coroutine _endSequence;
    private Coroutine _scrollStartRoutine;
    private bool _restorePauseOnClose;
    private bool _creditsMusicActive;
    private Camera _creditsCamera;
    private bool _ownsCreditsCamera;
    private GameObject _ownedAmbienceRoot;
    private readonly List<ParticleSystemRenderer> _tunedParticleRenderers = new(8);
    private readonly List<int> _tunedParticleSortingOrders = new(8);

    public bool IsVisible => _panel != null && _panel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap() => EnsureExists();

    public static void Open() => Open(null);

    public static void Open(CreditsPresentationConfig? presentation)
    {
        CreditsOverlayController controller = GetController();
        if (controller == null)
        {
            Debug.LogError("[CreditsOverlay] Falha ao abrir créditos.");
            return;
        }

        controller.Show(presentation, restorePauseOnClose: false);
    }

    /// <summary>Abre créditos a partir do pause (solo/MP). Não despausa; ao fechar, restaura o menu de pause.</summary>
    public static void OpenFromPause()
    {
        CreditsOverlayController controller = GetController();
        if (controller == null)
        {
            Debug.LogError("[CreditsOverlay] Falha ao abrir créditos.");
            return;
        }

        controller.Show(CreditsPresentationConfig.DefaultPause, restorePauseOnClose: true);
    }

    private static CreditsOverlayController GetController()
    {
        EnsureExists();

        if (Instance != null)
            return Instance;

        return FindFirstObjectByType<CreditsOverlayController>(FindObjectsInactive.Include);
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        CreditsOverlayController existing =
            FindFirstObjectByType<CreditsOverlayController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf)
                existing.gameObject.SetActive(true);
            return;
        }

        new GameObject(nameof(CreditsOverlayController)).AddComponent<CreditsOverlayController>();
    }

    protected override void Awake()
    {
        base.Awake();
        BuildOverlay();
        Hide();
    }

    public void Show(CreditsPresentationConfig? presentation = null)
    {
        Show(presentation, restorePauseOnClose: false);
    }

    private void Show(CreditsPresentationConfig? presentation, bool restorePauseOnClose)
    {
        CancelEndSequence();
        _activePresentation = presentation ?? defaultPresentation;
        _restorePauseOnClose = restorePauseOnClose;

        if (_restorePauseOnClose)
            SetPauseOverlayVisible(false);

        gameObject.SetActive(true);
        BuildOverlay();
        ApplyBodyText();
        EnsureAmbienceAndCamera();

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
            _canvas.enabled = true;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 500;
        }

        ResetPanelAlpha();
        if (_panel != null)
            _panel.SetActive(true);
        BeginScrollWhenReady();
        PlayCreditsMusic();
    }

    public void Hide()
    {
        CancelEndSequence();
        CancelScrollStart();
        _isScrolling = false;
        RestoreSceneMusic();
        RestoreParticleSorting();
        TeardownOwnedAmbienceAndCamera();
        ResetPanelAlpha();

        if (_panel != null)
            _panel.SetActive(false);

        RestorePauseIfNeeded();
    }

    private void RestorePauseIfNeeded()
    {
        if (!_restorePauseOnClose)
            return;

        _restorePauseOnClose = false;

        if (!IsGamePaused())
            return;

        SetPauseOverlayVisible(true);
    }

    private static bool IsGamePaused()
    {
        if (GameFlowOrchestrator.Instance != null && GameFlowOrchestrator.Instance.IsPauseActive)
            return true;

        if (MultiplayerGameManager.Instance != null
            && MultiplayerGameManager.Instance.CurrentState == GameState.Paused)
            return true;

        GameManager2 local = FindFirstObjectByType<GameManager2>();
        if (local != null && local.CurrentState == GameStates.Paused)
            return true;

        return Time.timeScale <= 0f;
    }

    private static void SetPauseOverlayVisible(bool visible)
    {
        GameManager2 local = FindFirstObjectByType<GameManager2>();
        if (local != null)
        {
            if (visible)
                local.ShowPauseOverlay();
            else
                local.HidePauseOverlay();
            return;
        }

        SceneOverlayController overlay = FindFirstObjectByType<SceneOverlayController>();
        if (overlay == null)
            return;

        if (visible)
            overlay.OpenOverlay("pause");
        else
            overlay.CloseOverlay("pause");
    }

    private void Update()
    {
        if (!_isScrolling || _scrollRect == null || _scrollRect.content == null)
            return;

        float delta = Time.unscaledDeltaTime;
        float scrollable = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        if (scrollable <= 1f)
        {
            FinishScroll();
            return;
        }

        float next = _scrollRect.verticalNormalizedPosition - (scrollSpeedPixelsPerSecond * delta) / scrollable;
        if (next <= _endNormalizedScroll)
        {
            _scrollRect.verticalNormalizedPosition = _endNormalizedScroll;
            FinishScroll();
            return;
        }

        _scrollRect.verticalNormalizedPosition = next;
    }

    private void FinishScroll()
    {
        _isScrolling = false;

        if (_activePresentation.EndBehavior == CreditsEndBehavior.HoldThenFadeClose)
            _endSequence = StartCoroutine(HoldFadeAndClose());
    }

    private IEnumerator HoldFadeAndClose()
    {
        float hold = Mathf.Max(0f, _activePresentation.HoldAtEndSeconds);
        if (hold > 0f)
        {
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float fade = Mathf.Max(0f, _activePresentation.FadeOutSeconds);
        if (fade > 0f && _panelGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _panelGroup.alpha = 1f - Mathf.Clamp01(elapsed / fade);
                yield return null;
            }
        }

        Hide();
    }

    private void CancelEndSequence()
    {
        if (_endSequence == null)
            return;

        StopCoroutine(_endSequence);
        _endSequence = null;
    }

    private void BeginScrollWhenReady()
    {
        CancelScrollStart();
        _isScrolling = false;
        _scrollStartRoutine = StartCoroutine(StartScrollWhenReady());
    }

    private void CancelScrollStart()
    {
        if (_scrollStartRoutine == null)
            return;

        StopCoroutine(_scrollStartRoutine);
        _scrollStartRoutine = null;
    }

    private IEnumerator StartScrollWhenReady()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        RefreshScrollLayout(resetToStart: true);
        _isScrolling = true;
        _scrollStartRoutine = null;
    }

    private void ResetPanelAlpha()
    {
        if (_panelGroup != null)
            _panelGroup.alpha = 1f;
    }

    private void BuildOverlay()
    {
        if (_overlayBuilt)
            return;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        GameObject root = new GameObject("OverlayRoot");
        root.transform.SetParent(transform, false);

        _canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(root.transform, "CreditsOverlay");
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.planeDistance = 100f;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 500;

        _panel = ScreenFlowPlaceholderFactory.CreatePanel(_canvas.transform, "Panel", new Color(0.02f, 0.02f, 0.05f, 1f));
        ApplyPanelBackground();

        _panelGroup = _panel.GetComponent<CanvasGroup>();
        if (_panelGroup == null)
            _panelGroup = _panel.AddComponent<CanvasGroup>();

        BuildScrollArea(_panel.transform);

        _closeButton = ScreenFlowPlaceholderFactory.CreateButton(_panel.transform, "Fechar",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-160f, 40f), new Vector2(160f, 120f));
        _closeButton.onClick.AddListener(Hide);
        ApplyCloseButtonVisual();
        UiButtonFeedbackUtility.ApplyToSelectable(_closeButton);

        _overlayBuilt = true;
    }

    private void BuildScrollArea(Transform panel)
    {
        float widthNorm = DefaultBodyWidthNormalized;
        CreditsVisualConfig config = ResolveVisualConfig();
        if (config != null && config.BodyWidthNormalized > 0.05f)
            widthNorm = Mathf.Clamp(config.BodyWidthNormalized, 0.2f, 1f);

        float side = (1f - widthNorm) * 0.5f;

        GameObject scrollGo = new GameObject("CreditsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(side, 0.12f);
        scrollRt.anchorMax = new Vector2(1f - side, 1f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _topSpacer = CreateSpacer(content.transform, "TopSpacer");
        _bodyText = CreateBodyText(content.transform);
        _bottomSpacer = CreateSpacer(content.transform, "BottomSpacer");

        _scrollRect = scrollGo.GetComponent<ScrollRect>();
        _scrollRect.viewport = viewport.GetComponent<RectTransform>();
        _scrollRect.content = contentRt;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 24f;
        _scrollRect.inertia = false;
    }

    private TMP_Text CreateBodyText(Transform parent)
    {
        GameObject go = new GameObject("CreditsBodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(8f, 0f, 8f, 0f);
        tmp.raycastTarget = false;
        ApplyBodyTypography(tmp);

        ContentSizeFitter bodyFitter = go.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        go.AddComponent<LayoutElement>();
        return tmp;
    }

    private void ApplyBodyTypography(TMP_Text tmp)
    {
        if (tmp == null)
            return;

        CreditsVisualConfig config = ResolveVisualConfig();
        if (config == null)
        {
            tmp.color = Color.black;
            return;
        }

        if (config.BodyFont != null)
        {
            MaterialReferenceManager.AddFontAsset(config.BodyFont);
            tmp.font = config.BodyFont;
        }

        if (config.BodyFontSize > 0f)
            tmp.fontSize = config.BodyFontSize;

        tmp.color = config.BodyTextColor;

        if (config.TitleFont != null)
            MaterialReferenceManager.AddFontAsset(config.TitleFont);
    }

    private void ApplyPanelBackground()
    {
        if (_panel == null || !_panel.TryGetComponent(out Image panelImage))
            return;

        CreditsVisualConfig config = ResolveVisualConfig();
        if (config != null && config.BackgroundSprite != null)
        {
            panelImage.sprite = config.BackgroundSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;
        }

        if (config != null && config.LitBackgroundMaterial != null)
            panelImage.material = config.LitBackgroundMaterial;

        panelImage.raycastTarget = false;
    }

    private void EnsureAmbienceAndCamera()
    {
        if (TryBindSceneAmbienceCamera())
        {
            BringMenuParticlesInFrontOfCredits();
            return;
        }

        SpawnOwnedAmbienceAndCamera();
        BringMenuParticlesInFrontOfCredits();
    }

    private bool TryBindSceneAmbienceCamera()
    {
        if (!TryFindSceneRoot("Light", out GameObject light)
            || !TryFindSceneRoot("ParticleSystem", out GameObject particles))
            return false;

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
            sceneCamera = FindFirstObjectByType<Camera>();
        if (sceneCamera == null)
            return false;

        TeardownOwnedAmbienceAndCamera();
        light.SetActive(true);
        particles.SetActive(true);
        BindCanvasCamera(sceneCamera);
        return true;
    }

    private void SpawnOwnedAmbienceAndCamera()
    {
        if (_ownedAmbienceRoot != null && _creditsCamera != null)
        {
            BindCanvasCamera(_creditsCamera);
            _ownedAmbienceRoot.SetActive(true);
            return;
        }

        TeardownOwnedAmbienceAndCamera();

        GameObject prefab = Resources.Load<GameObject>(MenuUiAmbienceResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning(
                "[CreditsOverlay] Prefab Resources/UI/MenuUiAmbience não encontrado. " +
                "Rode MidnightMeow/UI/Build MenuUiAmbience Prefab from Menu2.");
            BindCanvasCamera(Camera.main);
            return;
        }

        _ownedAmbienceRoot = Instantiate(prefab, transform);
        _ownedAmbienceRoot.name = "MenuUiAmbience";
        _ownedAmbienceRoot.SetActive(true);

        GameObject cameraGo = new GameObject("CreditsAmbienceCamera");
        cameraGo.transform.SetParent(transform, false);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        _creditsCamera = cameraGo.AddComponent<Camera>();
        _creditsCamera.orthographic = true;
        _creditsCamera.orthographicSize = 5f;
        _creditsCamera.clearFlags = CameraClearFlags.SolidColor;
        _creditsCamera.backgroundColor = Color.black;
        _creditsCamera.depth = 100f;
        _creditsCamera.cullingMask = ~0;
        _creditsCamera.nearClipPlane = 0.3f;
        _creditsCamera.farClipPlane = 1000f;
        _ownsCreditsCamera = true;

        BindCanvasCamera(_creditsCamera);
    }

    /// <summary>
    /// No Menu2 as partículas usam sortingOrder 1 e o Canvas 0 — ficam na frente.
    /// Nos créditos o Canvas está em 500, então as partículas precisam de order &gt; 500.
    /// </summary>
    private void BringMenuParticlesInFrontOfCredits()
    {
        RestoreParticleSorting();

        int targetOrder = (_canvas != null ? _canvas.sortingOrder : 500) + 10;
        Transform searchRoot = _ownedAmbienceRoot != null
            ? _ownedAmbienceRoot.transform
            : null;

        ParticleSystemRenderer[] renderers;
        if (searchRoot != null)
        {
            renderers = searchRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
        }
        else if (TryFindSceneRoot("ParticleSystem", out GameObject sceneParticles))
        {
            renderers = sceneParticles.GetComponentsInChildren<ParticleSystemRenderer>(true);
            sceneParticles.SetActive(true);
        }
        else
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            ParticleSystemRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            _tunedParticleRenderers.Add(renderer);
            _tunedParticleSortingOrders.Add(renderer.sortingOrder);
            renderer.sortingOrder = targetOrder;
            renderer.sortingLayerID = 0;

            ParticleSystem ps = renderer.GetComponent<ParticleSystem>();
            if (ps != null && !ps.isPlaying)
                ps.Play(true);
        }
    }

    private void RestoreParticleSorting()
    {
        for (int i = 0; i < _tunedParticleRenderers.Count; i++)
        {
            ParticleSystemRenderer renderer = _tunedParticleRenderers[i];
            if (renderer == null)
                continue;

            if (i < _tunedParticleSortingOrders.Count)
                renderer.sortingOrder = _tunedParticleSortingOrders[i];
        }

        _tunedParticleRenderers.Clear();
        _tunedParticleSortingOrders.Clear();
    }

    private void BindCanvasCamera(Camera camera)
    {
        if (_canvas == null || camera == null)
            return;

        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = camera;
        _canvas.planeDistance = 100f;
    }

    private void TeardownOwnedAmbienceAndCamera()
    {
        RestoreParticleSorting();

        if (_ownedAmbienceRoot != null)
        {
            Destroy(_ownedAmbienceRoot);
            _ownedAmbienceRoot = null;
        }

        if (_ownsCreditsCamera && _creditsCamera != null)
        {
            Destroy(_creditsCamera.gameObject);
            _creditsCamera = null;
            _ownsCreditsCamera = false;
        }
        else
        {
            _creditsCamera = null;
            _ownsCreditsCamera = false;
        }

        if (_canvas != null)
            _canvas.worldCamera = null;
    }

    private static bool TryFindSceneRoot(string objectName, out GameObject root)
    {
        root = null;
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || go.transform.parent != null)
                continue;
            if (!go.name.Equals(objectName, System.StringComparison.Ordinal))
                continue;

            root = go;
            return true;
        }

        return false;
    }

    private void ApplyCloseButtonVisual()
    {
        if (_closeButton == null)
            return;

        CreditsVisualConfig config = ResolveVisualConfig();
        if (config != null)
        {
            ScreenThemeApplier.ApplyButton(_closeButton, config.CloseButtonSprite, config.CloseButtonColor);
            if (config.CloseButtonSprite != null)
            {
                Image image = _closeButton.GetComponent<Image>();
                if (image != null)
                    image.color = config.CloseButtonColor;
            }
        }

        TMP_Text label = _closeButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        label.color = Color.black;
        label.fontStyle = FontStyles.Bold;
        ApplyBodyTypography(label);
        // Botão: um pouco menor que o corpo dos créditos, para ficar conciso.
        if (config != null && config.BodyFontSize > 0f)
            label.fontSize = Mathf.Max(18f, config.BodyFontSize * 0.85f);
    }

    private CreditsVisualConfig ResolveVisualConfig()
    {
        if (visualConfig != null)
            return visualConfig;

        visualConfig = Resources.Load<CreditsVisualConfig>(CreditsVisualConfigResourcePath);
        return visualConfig;
    }

    private static string ApplyTitleFontTags(string text, TMP_FontAsset titleFont)
    {
        if (string.IsNullOrEmpty(text) || titleFont == null)
            return text;

        MaterialReferenceManager.AddFontAsset(titleFont);
        string fontName = titleFont.name;
        return TitleSizeBlockRegex.Replace(
            text,
            match => $"<font=\"{fontName}\"><size={match.Groups[1].Value}>{match.Groups[2].Value}</size></font>");
    }

    private static RectTransform CreateSpacer(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = 0f;
        return go.GetComponent<RectTransform>();
    }

    private void ApplyBodyText()
    {
        if (_bodyText == null)
            return;

        // Escolhe o arquivo pelo idioma ativo; cai para o PT se a versão EN não existir.
        string path = IsPortuguese() ? CreditsBodyResourcePath : CreditsBodyResourcePathEn;
        TextAsset source = Resources.Load<TextAsset>(path);
        if (source == null && path != CreditsBodyResourcePath)
            source = Resources.Load<TextAsset>(CreditsBodyResourcePath);

        if (source == null)
        {
            Debug.LogError("[CreditsOverlay] Assets/Resources/CreditsBody.txt não encontrado.");
            _bodyText.text = "Créditos\n\n(Arquivo CreditsBody.txt não encontrado em Resources)";
            return;
        }

        CreditsVisualConfig config = ResolveVisualConfig();
        ApplyBodyTypography(_bodyText);
        string body = source.text;
        if (config != null)
            body = ApplyTitleFontTags(body, config.TitleFont);
        _bodyText.text = body;
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshScrollLayout(bool resetToStart)
    {
        if (_scrollRect == null || _scrollRect.viewport == null || _scrollRect.content == null)
            return;

        float viewportHeight = _scrollRect.viewport.rect.height;
        if (viewportHeight < 1f)
            viewportHeight = ScreenFlowPlaceholderFactory.ReferenceHeight * 0.88f;

        if (_topSpacer != null)
            _topSpacer.GetComponent<LayoutElement>().preferredHeight = viewportHeight;

        if (_bottomSpacer != null)
            _bottomSpacer.GetComponent<LayoutElement>().preferredHeight = _activePresentation.EndScrollPadding;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);

        _endNormalizedScroll = ComputeEndNormalizedScrollPosition(_activePresentation.EndScrollPadding);

        if (resetToStart)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private float ComputeEndNormalizedScrollPosition(float bottomPadding)
    {
        RectTransform content = _scrollRect.content;
        RectTransform viewport = _scrollRect.viewport;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float scrollable = contentHeight - viewportHeight;
        if (scrollable <= 1f)
            return 1f;

        Bounds bodyBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, _bodyText.rectTransform);
        float bodyBottomFromTop = -bodyBounds.min.y;
        float targetScroll = bodyBottomFromTop - viewportHeight + bottomPadding;
        targetScroll = Mathf.Clamp(targetScroll, 0f, scrollable);

        return 1f - (targetScroll / scrollable);
    }

    private AudioClip ResolveCreditsMusic()
    {
        if (creditsMusic != null)
            return creditsMusic;

        CreditsMusicClipReference reference =
            Resources.Load<CreditsMusicClipReference>(CreditsMusicResourcePath);
        if (reference != null && reference.clip != null)
        {
            creditsMusic = reference.clip;
            return creditsMusic;
        }

        creditsMusic = Resources.Load<AudioClip>(CreditsMusicAudioResourcePath);
        if (creditsMusic == null)
            Debug.LogError("[CreditsOverlay] Não foi possível carregar a trilha dos créditos.");

        return creditsMusic;
    }

    private void PlayCreditsMusic()
    {
        AudioClip clip = ResolveCreditsMusic();
        if (clip == null)
            return;

        _creditsMusicActive = true;
        MusicCrossfadeController.EnsureExists();
        MusicCrossfadeController music = MusicCrossfadeController.Instance;
        if (music == null)
            return;

        music.BeginExternalOverride(clip, loop: true, duration: 0.75f);
    }

    private void RestoreSceneMusic()
    {
        if (!_creditsMusicActive)
            return;

        _creditsMusicActive = false;

        MusicCrossfadeController.EnsureExists();
        MusicCrossfadeController music = MusicCrossfadeController.Instance;
        if (music == null)
            return;

        music.EndExternalOverride();
        Scene scene = SceneManager.GetActiveScene();
        music.PrepareSceneMusic(scene);
        music.FadeInPending(1f);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
