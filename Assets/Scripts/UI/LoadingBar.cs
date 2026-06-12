using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sincroniza a barra de loading da cena com <see cref="ScreenFlowController.LoadingProgress"/>.
/// Reinicia sempre que a tela de loading é exibida.
/// </summary>
[DisallowMultipleComponent]
public class LoadingBar : MonoBehaviour
{
    [SerializeField] private Image loadingBar;
    [SerializeField] private float fillSpeed = 2.5f;

    private Coroutine _routine;
    private float _smoothedProgress;

    private void Awake()
    {
        if (loadingBar == null)
            loadingBar = GetComponentInChildren<Image>(true);

        if (loadingBar != null)
            loadingBar = LoadingProgressUtility.EnsureFillFromLegacyImage(
                loadingBar,
                LoadingProgressUtility.DefaultTrackColor,
                LoadingProgressUtility.DefaultFillColor);

        ResetBarVisual();
    }

    private void OnEnable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null)
            flow.OnLoadingScreenVisibilityChanged += HandleLoadingScreenVisibilityChanged;

        if (_routine == null)
            _routine = StartCoroutine(SyncWithScreenFlow());
    }

    private void OnDisable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null)
            flow.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void HandleLoadingScreenVisibilityChanged(bool visible)
    {
        if (visible)
            ResetBarVisual();
    }

    private void ResetBarVisual()
    {
        _smoothedProgress = 0f;

        if (loadingBar != null)
            LoadingProgressUtility.ResetProgress(loadingBar);
    }

    private IEnumerator SyncWithScreenFlow()
    {
        while (true)
        {
            ScreenFlowController flow = ScreenFlowController.Instance;
            if (flow != null && flow.IsLoadingScreenVisible && loadingBar != null)
            {
                float target = flow.LoadingProgress;
                _smoothedProgress = Mathf.MoveTowards(
                    _smoothedProgress,
                    target,
                    fillSpeed * Time.unscaledDeltaTime);
                LoadingProgressUtility.SetProgress(loadingBar, _smoothedProgress);
            }

            yield return null;
        }
    }
}

/// <summary>
/// Barras de loading: trilho (vermelho) + fill filho (branco) com largura via âncoras.
/// </summary>
public static class LoadingProgressUtility
{
    public static readonly Color DefaultTrackColor = new Color(0.75f, 0.12f, 0.12f, 1f);
    public static readonly Color DefaultFillColor = Color.white;

    private static Sprite _solidSprite;

    public static Sprite GetSolidSprite()
    {
        if (_solidSprite != null)
            return _solidSprite;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        return _solidSprite;
    }

    public static Image CreateProgressBar(
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Color trackColor,
        Color fillColor)
    {
        GameObject trackGo = new GameObject("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackGo.transform.SetParent(parent, false);

        RectTransform trackRt = trackGo.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.5f, 0.5f);
        trackRt.anchorMax = new Vector2(0.5f, 0.5f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.anchoredPosition = anchoredPosition;
        trackRt.sizeDelta = size;

        Image track = trackGo.GetComponent<Image>();
        track.color = trackColor;
        ApplySolidSprite(track);

        GameObject fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(trackRt, false);

        Image fill = fillGo.GetComponent<Image>();
        fill.color = fillColor;
        ApplySolidSprite(fill);
        ResetProgress(fill);

        return fill;
    }

    public static Image EnsureFillFromLegacyImage(Image legacyBar, Color trackColor, Color fillColor)
    {
        if (legacyBar == null)
            return null;

        if (legacyBar.gameObject.name == "ProgressFill")
            return legacyBar;

        if (legacyBar.gameObject.name == "ProgressTrack")
        {
            Transform existing = legacyBar.transform.Find("ProgressFill");
            if (existing != null && existing.TryGetComponent(out Image trackFill))
                return trackFill;
        }

        Transform parent = legacyBar.transform.parent;
        if (parent != null && parent.name == "ProgressTrack")
            return legacyBar;

        RectTransform trackRt = legacyBar.rectTransform;
        legacyBar.gameObject.name = "ProgressTrack";
        legacyBar.color = trackColor;
        ApplySolidSprite(legacyBar);
        legacyBar.type = Image.Type.Simple;

        Transform fillChild = trackRt.Find("ProgressFill");
        if (fillChild != null && fillChild.TryGetComponent(out Image existingFillImage))
            return existingFillImage;

        GameObject fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(trackRt, false);

        Image fill = fillGo.GetComponent<Image>();
        fill.color = fillColor;
        ApplySolidSprite(fill);
        ResetProgress(fill);
        return fill;
    }

    public static Image ResolveOrCreateFill(Transform parent, Image legacyCandidate = null)
    {
        if (parent == null)
            return null;

        Transform existingFill = parent.Find("ProgressFill");
        if (existingFill == null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == "ProgressFill" && child.TryGetComponent(out Image childFill))
                    return childFill;
            }
        }
        else if (existingFill.TryGetComponent(out Image fill))
        {
            return fill;
        }

        Transform track = parent.Find("ProgressTrack");
        if (track != null)
        {
            Transform trackFill = track.Find("ProgressFill");
            if (trackFill != null && trackFill.TryGetComponent(out Image nestedFill))
                return nestedFill;
        }

        if (legacyCandidate != null)
            return EnsureFillFromLegacyImage(legacyCandidate, DefaultTrackColor, DefaultFillColor);

        return CreateProgressBar(parent, new Vector2(0f, -120f), new Vector2(640f, 18f), DefaultTrackColor, DefaultFillColor);
    }

    public static void ApplySolidSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = GetSolidSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.useSpriteMesh = false;
    }

    public static void SetProgress(Image fill, float progress)
    {
        if (fill == null)
            return;

        progress = Mathf.Clamp01(progress);
        ApplySolidSprite(fill);

        RectTransform rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(progress, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void ResetProgress(Image fill)
    {
        SetProgress(fill, 0f);
    }
}
