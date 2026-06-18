// ----------------------------------------------------------------------------
// FEITO POR: DEBS CARVALHO
// ÚLTIMA ATUALIZAÇÃO: 2026-06-18
// DESCRIÇÃO: Overlay global de créditos (DDOL). UI em código; fim configurável
// ----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Overlay global de créditos (DDOL). UI em código; fim configurável

[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public class CreditsOverlayController : Singleton<CreditsOverlayController>
{
    private const string CreditsBodyResourcePath = "CreditsBody";

    [Header("Rolagem")]
    [SerializeField] private float scrollSpeedPixelsPerSecond = 55f;

    [Header("Fim (padrão menu/pause)")]
    [SerializeField] private CreditsPresentationConfig defaultPresentation = CreditsPresentationConfig.DefaultMenu;

    [Header("Áudio (opcional)")]
    [SerializeField] private AudioClip creditsMusic;

    private Canvas _canvas;
    private GameObject _panel;
    private CanvasGroup _panelGroup;
    private ScrollRect _scrollRect;
    private RectTransform _topSpacer;
    private RectTransform _bottomSpacer;
    private TMP_Text _bodyText;
    private AudioSource _musicSource;
    private bool _overlayBuilt;
    private bool _isScrolling;
    private float _endNormalizedScroll = 0f;
    private CreditsPresentationConfig _activePresentation;
    private Coroutine _endSequence;
    private bool _restorePauseOnClose;

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

        BuildOverlay();
        ApplyBodyText();

        gameObject.SetActive(true);

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
            _canvas.enabled = true;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 500;
        }

        ResetPanelAlpha();
        _panel.SetActive(true);
        RefreshScrollLayout(resetToStart: true);
        _isScrolling = true;
        PlayMusic();
    }

    public void Hide()
    {
        CancelEndSequence();
        _isScrolling = false;
        StopMusic();
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
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 500;

        _panel = ScreenFlowPlaceholderFactory.CreatePanel(_canvas.transform, "Panel", new Color(0.02f, 0.02f, 0.05f, 0.96f));
        if (_panel.TryGetComponent(out Image panelImage))
            panelImage.raycastTarget = false;

        _panelGroup = _panel.GetComponent<CanvasGroup>();
        if (_panelGroup == null)
            _panelGroup = _panel.AddComponent<CanvasGroup>();

        BuildScrollArea(_panel.transform);

        Button close = ScreenFlowPlaceholderFactory.CreateButton(_panel.transform, "Fechar",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-160f, 40f), new Vector2(160f, 120f));
        close.onClick.AddListener(Hide);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.spatialBlend = 0f;

        _overlayBuilt = true;
    }

    private void BuildScrollArea(Transform panel)
    {
        GameObject scrollGo = new GameObject("CreditsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0.12f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
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
        _scrollRect.scrollSensitivity = 0f;
        _scrollRect.inertia = false;
    }

    private static TMP_Text CreateBodyText(Transform parent)
    {
        GameObject go = new GameObject("CreditsBodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.color = new Color(0.92f, 0.92f, 0.95f, 1f);
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(48f, 0f, 48f, 0f);
        tmp.raycastTarget = false;

        ContentSizeFitter bodyFitter = go.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        go.AddComponent<LayoutElement>();
        return tmp;
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

        TextAsset source = Resources.Load<TextAsset>(CreditsBodyResourcePath);
        if (source == null)
        {
            Debug.LogError("[CreditsOverlay] Assets/Resources/CreditsBody.txt não encontrado.");
            _bodyText.text = "Créditos\n\n(Arquivo CreditsBody.txt não encontrado em Resources)";
            return;
        }

        _bodyText.text = source.text;
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

    private void PlayMusic()
    {
        if (creditsMusic == null || _musicSource == null)
            return;

        _musicSource.clip = creditsMusic;
        _musicSource.Play();
    }

    private void StopMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
            _musicSource.Stop();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
