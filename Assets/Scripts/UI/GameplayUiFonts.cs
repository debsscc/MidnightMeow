using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica Fira Sans em textos de gameplay criados em runtime ou prefabs.
/// </summary>
public static class GameplayUiFonts
{
    private const string ConfigResource = "GameplayUiFontConfig";
    private static GameplayUiFontConfig _config;

    /// <summary>Tamanho unificado dos prompts world-space (selar / reviver / consertar).</summary>
    public const float WorldInteractionFontSize = 0.9f;

    /// <summary>Área do canvas world-space — larga o bastante para a frase inteira.</summary>
    public static readonly Vector2 WorldInteractionCanvasSize = new Vector2(6.5f, 1.1f);

    /// <summary>Acima de buracos (0–1), decoração e zonas de selamento (~250).</summary>
    public const int WorldInteractionSortingOrder = 450;

    /// <summary>Offset Y padrão acima do alvo (buraco / jogador). Carruagem pode usar um valor maior via SO.</summary>
    public static readonly Vector3 WorldInteractionLabelOffset = new Vector3(0f, 1.85f, 0f);

    /// <summary>Cor dos prompts world-space — levemente transparente para não competir com o gameplay.</summary>
    public static readonly Color WorldInteractionColor = new Color(0.85f, 0.95f, 1f, 0.78f);

    public static void Apply(TMP_Text text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = ResolveTmp();
        if (font == null)
            return;

        text.font = font;
    }

    public static void Apply(Text text)
    {
        if (text == null)
            return;

        Font font = ResolveLegacy();
        if (font == null)
            return;

        text.font = font;
    }

    /// <summary>Fonte, tamanho e opacidade padronizados para prompts de interação no mundo.</summary>
    public static void ApplyWorldInteraction(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        label.fontSize = WorldInteractionFontSize;
        label.enableAutoSizing = false;
        label.fontSizeMin = WorldInteractionFontSize;
        label.fontSizeMax = WorldInteractionFontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.Center;
        label.color = WorldInteractionColor;
        Apply(label);
    }

    /// <summary>
    /// Canvas world-space com escala 1 (sem herdar scale do pai) e sorting na frente do mundo.
    /// </summary>
    public static Canvas CreateWorldInteractionCanvas(string name, out RectTransform rect)
    {
        var root = new GameObject(name);
        rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = WorldInteractionCanvasSize;
        rect.localScale = Vector3.one;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = WorldInteractionSortingOrder;
        return canvas;
    }

    /// <summary>Aplica tamanho/sorting padrão a um canvas world-space já existente (prefab).</summary>
    public static void ConfigureWorldInteractionCanvas(RectTransform canvasRect)
    {
        if (canvasRect == null)
            return;

        Canvas canvas = canvasRect.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = WorldInteractionSortingOrder;
        }

        canvasRect.sizeDelta = WorldInteractionCanvasSize;
        canvasRect.localScale = Vector3.one;
    }

    private static TMP_FontAsset ResolveTmp()
    {
        GameplayUiFontConfig config = ResolveConfig();
        return config != null ? config.TmpFont : null;
    }

    private static Font ResolveLegacy()
    {
        GameplayUiFontConfig config = ResolveConfig();
        return config != null ? config.LegacyFont : null;
    }

    private static GameplayUiFontConfig ResolveConfig()
    {
        if (_config != null)
            return _config;

        _config = Resources.Load<GameplayUiFontConfig>(ConfigResource);
        return _config;
    }
}
