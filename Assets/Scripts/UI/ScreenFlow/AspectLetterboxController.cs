using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Letterbox 16:9 URP-safe — Camera.rect nas câmeras existentes +
// barras Overlay (sprite sólido) + OnGUI (garantia visual no Editor/build).
// ---------------------------------------------------------------- */

/// <summary>
/// Força aspect 16:9: letterbox nas câmeras da cena + barras pretas + safe area Overlay.
/// </summary>
[DisallowMultipleComponent]
public sealed class AspectLetterboxController : Singleton<AspectLetterboxController>
{
    public const bool IsEnabled = true;

    public const float TargetAspect = LetterboxAspectMath.DefaultTargetAspect;
    public const int BarCanvasSortOrder = 31000;
    public const string SafeAreaName = "LetterboxSafeArea";

    private Canvas _barCanvas;
    private RectTransform _barLeft;
    private RectTransform _barRight;
    private RectTransform _barTop;
    private RectTransform _barBottom;

    private int _lastWidth = -1;
    private int _lastHeight = -1;
    private Rect _viewport = new Rect(0f, 0f, 1f, 1f);
    private int _reconcileFrames;
    private float _fitTimer;
    private bool _loggedScreenSize;

    /// <summary>Viewport normalizado (0–1) aplicado em <see cref="Camera.rect"/> e nas barras.</summary>
    public Rect CurrentViewport => _viewport;

    /// <summary>
    /// Tamanho real do Game View / janela. No Editor, <see cref="Screen"/> pode
    /// colapsar para o pixelRect letterboxed e desligar as barras por engano.
    /// </summary>
    public static void GetOutputSize(out int width, out int height)
    {
#if UNITY_EDITOR
        Vector2 gameView = UnityEditor.Handles.GetMainGameViewSize();
        width = Mathf.Max(1, Mathf.RoundToInt(gameView.x));
        height = Mathf.Max(1, Mathf.RoundToInt(gameView.y));
        if (width > 1 && height > 1)
            return;
#endif
        width = Mathf.Max(1, Screen.width);
        height = Mathf.Max(1, Screen.height);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBeforeSceneLoad() => EnsureExists();

    public static AspectLetterboxController EnsureExists()
    {
        if (!IsEnabled)
            return null;

        if (Instance != null)
            return Instance;

        AspectLetterboxController existing =
            FindFirstObjectByType<AspectLetterboxController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeInHierarchy)
                existing.gameObject.SetActive(true);
            return Instance != null ? Instance : existing;
        }

        var go = new GameObject(nameof(AspectLetterboxController));
        return go.AddComponent<AspectLetterboxController>();
    }

    protected override void Awake()
    {
        if (!IsEnabled)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
        EnsureBarCanvas();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyAll();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void LateUpdate()
    {
        if (!IsEnabled)
            return;

        // Sempre recalcula: no Game View Free Aspect o Screen muda sem eventos confiáveis.
        GetOutputSize(out int width, out int height);
        bool sizeChanged = width != _lastWidth || height != _lastHeight;

        if (sizeChanged || _reconcileFrames > 0)
        {
            if (_reconcileFrames > 0)
                _reconcileFrames--;
            ApplyAll();
            _fitTimer = 0f;
            return;
        }

        // Mantém Camera.rect mesmo se algum sistema resetar a câmera.
        ApplyCameraRects(_viewport);

        _fitTimer += Time.unscaledDeltaTime;
        if (_fitTimer >= 0.25f)
        {
            _fitTimer = 0f;
            FitOverlayCanvases();
        }
    }

    /// <summary>
    /// Barras via IMGUI — visíveis mesmo se o Canvas UGUI estiver off/sem sprite.
    /// Coordenadas GUI: origem topo-esquerda.
    /// </summary>
    private void OnGUI()
    {
        if (!IsEnabled)
            return;

        GetOutputSize(out int width, out int height);
        Rect viewport = LetterboxAspectMath.CalculateNormalizedViewport(width, height, TargetAspect);
        if (!LetterboxAspectMath.HasBars(viewport))
            return;

        float left = viewport.x * width;
        float right = (1f - viewport.xMax) * width;
        float bottom = viewport.y * height;
        float top = (1f - viewport.yMax) * height;

        Color prev = GUI.color;
        GUI.color = Color.black;
        Texture2D tex = Texture2D.whiteTexture;

        if (left > 0.5f)
            GUI.DrawTexture(new Rect(0f, 0f, left, height), tex);
        if (right > 0.5f)
            GUI.DrawTexture(new Rect(width - right, 0f, right, height), tex);
        if (top > 0.5f)
            GUI.DrawTexture(new Rect(0f, 0f, width, top), tex);
        if (bottom > 0.5f)
            GUI.DrawTexture(new Rect(0f, height - bottom, width, bottom), tex);

        GUI.color = prev;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _reconcileFrames = 6;
        _loggedScreenSize = false;
        ApplyAll();
    }

    /// <summary>Reaplica letterbox (barras, câmeras e safe areas).</summary>
    public void Reapply() => ApplyAll();

    private void ApplyAll()
    {
        EnsureBarCanvas();

        GetOutputSize(out int width, out int height);
        Rect viewport = LetterboxAspectMath.CalculateNormalizedViewport(width, height, TargetAspect);

        if (!_loggedScreenSize || width != _lastWidth || height != _lastHeight)
        {
            _loggedScreenSize = true;
            float aspect = (float)width / height;
            bool showBars = LetterboxAspectMath.HasBars(viewport);
            Debug.Log(
                $"[AspectLetterbox] Output={width}x{height} (Screen={Screen.width}x{Screen.height}) " +
                $"aspect={aspect:F3} target={TargetAspect:F3} viewport={viewport} showBars={showBars}");
        }

        _lastWidth = width;
        _lastHeight = height;
        _viewport = viewport;

        ApplyCameraRects(viewport);
        UpdateBars(viewport, width, height);
        FitOverlayCanvases();
    }

    /// <summary>
    /// Letterbox só em câmeras já existentes — não cria câmeras novas (URP Base extras = tela preta).
    /// </summary>
    private void ApplyCameraRects(Rect viewport)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
                continue;

            if (cam == TransitionCameraKeeper.FallbackCamera)
            {
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                continue;
            }

            cam.rect = viewport;
        }
    }

    private void EnsureBarCanvas()
    {
        if (_barCanvas != null)
        {
            // Garante que um disable manual no Hierarchy não “mate” o letterbox.
            if (!_barCanvas.gameObject.activeSelf)
                _barCanvas.gameObject.SetActive(true);
            return;
        }

        GameObject canvasGo = new GameObject(
            "LetterboxBars",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        _barCanvas = canvasGo.GetComponent<Canvas>();
        _barCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _barCanvas.overrideSorting = true;
        _barCanvas.sortingOrder = BarCanvasSortOrder;
        canvasGo.AddComponent<LetterboxExempt>();

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        _barLeft = CreateBar("BarLeft");
        _barRight = CreateBar("BarRight");
        _barTop = CreateBar("BarTop");
        _barBottom = CreateBar("BarBottom");
    }

    private RectTransform CreateBar(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(_barCanvas.transform, false);
        Image image = go.GetComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(image);
        image.color = Color.black;
        image.raycastTarget = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        go.SetActive(false);
        return rt;
    }

    private void UpdateBars(Rect viewport, int width, int height)
    {
        if (_barCanvas == null)
            return;

        // Canvas UGUI fica sempre ativo (raycast); filhos ligam/desligam conforme o excesso.
        if (!_barCanvas.gameObject.activeSelf)
            _barCanvas.gameObject.SetActive(true);
        _barCanvas.enabled = true;

        bool showBars = LetterboxAspectMath.HasBars(viewport);
        if (!showBars)
        {
            SetBarInactive(_barLeft);
            SetBarInactive(_barRight);
            SetBarInactive(_barTop);
            SetBarInactive(_barBottom);
            return;
        }

        float leftPx = viewport.x * width;
        float rightPx = (1f - viewport.xMax) * width;
        float bottomPx = viewport.y * height;
        float topPx = (1f - viewport.yMax) * height;

        SetBar(_barLeft, new Vector2(0f, 0f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(leftPx, 0f), leftPx > 0.5f);
        SetBar(_barRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-rightPx, 0f), Vector2.zero, rightPx > 0.5f);
        SetBar(_barBottom, new Vector2(0f, 0f), new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, bottomPx), bottomPx > 0.5f);
        SetBar(_barTop, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -topPx), Vector2.zero, topPx > 0.5f);
    }

    private static void SetBarInactive(RectTransform bar)
    {
        if (bar != null)
            bar.gameObject.SetActive(false);
    }

    private static void SetBar(
        RectTransform bar,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        bool active)
    {
        if (bar == null)
            return;

        bar.gameObject.SetActive(active);
        if (!active)
            return;

        Image image = bar.GetComponent<Image>();
        if (image != null)
        {
            LoadingProgressUtility.ApplySolidSprite(image);
            image.color = Color.black;
            image.enabled = true;
        }

        bar.anchorMin = anchorMin;
        bar.anchorMax = anchorMax;
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.offsetMin = offsetMin;
        bar.offsetMax = offsetMax;
    }

    private void FitOverlayCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == _barCanvas)
                continue;

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;

            if (IsExempt(canvas))
                continue;

            EnsureSafeArea(canvas);
        }
    }

    private static void EnsureSafeArea(Canvas canvas)
    {
        Transform existing = canvas.transform.Find(SafeAreaName);
        if (existing != null)
        {
            EnsureAspectFitter(existing.gameObject);
            return;
        }

        var safeGo = new GameObject(SafeAreaName, typeof(RectTransform), typeof(AspectRatioFitter));
        safeGo.transform.SetParent(canvas.transform, false);

        RectTransform safeRt = safeGo.GetComponent<RectTransform>();
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = Vector2.zero;
        safeRt.offsetMax = Vector2.zero;
        safeRt.pivot = new Vector2(0.5f, 0.5f);

        EnsureAspectFitter(safeGo);

        List<Transform> toMove = new List<Transform>(canvas.transform.childCount);
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child == safeGo.transform)
                continue;
            toMove.Add(child);
        }

        for (int i = 0; i < toMove.Count; i++)
            toMove[i].SetParent(safeGo.transform, false);

        safeGo.transform.SetAsFirstSibling();
    }

    private static void EnsureAspectFitter(GameObject safeGo)
    {
        AspectRatioFitter fitter = safeGo.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = safeGo.AddComponent<AspectRatioFitter>();

        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = TargetAspect;
    }

    private static bool IsExempt(Canvas canvas)
    {
        if (canvas.GetComponent<LetterboxExempt>() != null)
            return true;

        if (canvas.GetComponentInParent<LetterboxExempt>() != null)
            return true;

        if (canvas.sortingOrder >= 32000)
            return true;

        return false;
    }
}
