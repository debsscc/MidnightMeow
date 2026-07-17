/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: LEGADO — barra de loading do FadeManager (Menu2).
Substituída por Loading1/Loading2 + LoadingScreenController.
Mantido como stub para não quebrar cenas até remoção no Editor.
---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Obsoleto. Remova o GameObject <c>FadeManager</c> (Menu2) no Editor —
/// ver docs/editor/guides/remove-legacy-loading-placeholder.md
/// </summary>
[DisallowMultipleComponent]
[System.Obsolete("Use LoadingScreenController em Loading1/Loading2. Remova FadeManager da cena.")]
public class LoadingBar : MonoBehaviour
{
    [SerializeField] private Image loadingBar;
    [SerializeField] private float fillSpeed = 2.5f;
    [SerializeField] private float idleCreepPerSecond = 0.07f;
    [SerializeField] private float maxIdleCreep = 0.88f;

    private void OnEnable()
    {
        // Stub: não sincroniza mais com TransitionFadeOverlay (painel placeholder removido).
        if (loadingBar != null)
            LoadingProgressUtility.ResetProgress(loadingBar);
    }
}

public static class LoadingProgressUtility
{
    public static readonly Color DefaultTrackColor = new Color(0.75f, 0.12f, 0.12f, 1f);
    public static readonly Color DefaultFillColor = Color.white;
    public static readonly Vector2 DefaultBarSize = new Vector2(640f, 24f);

    public const float BottomStatusTextMinY = 56f;
    public const float BottomStatusTextMaxY = 120f;

    public const float BottomBarCenterY = 152f;

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

    public static Image CreateBottomProgressBar(
        Transform parent,
        float centerYFromBottom = BottomBarCenterY,
        Vector2? size = null,
        Color? trackColor = null,
        Color? fillColor = null)
    {
        Vector2 barSize = size ?? DefaultBarSize;
        Image fill = CreateProgressBar(
            parent,
            Vector2.zero,
            barSize,
            trackColor ?? DefaultTrackColor,
            fillColor ?? DefaultFillColor);

        ApplyBottomCenterAnchor(fill.transform.parent as RectTransform, centerYFromBottom);
        return fill;
    }

    public static void ApplyBottomCenterAnchor(RectTransform rectTransform, float centerYFromBottom)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, centerYFromBottom);
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

    /// <summary>Posiciona um ícone ao longo do trilho, na borda direita do preenchimento.</summary>
    public static void SetFollowerAlongTrack(RectTransform track, RectTransform follower, float progress, float yOffset = 0f)
    {
        if (track == null || follower == null)
            return;

        progress = Mathf.Clamp01(progress);
        float width = track.rect.width;
        float x = (-width * 0.5f) + (width * progress);
        follower.anchoredPosition = new Vector2(x, yOffset);
    }
}
