using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Utilitário para montar UI placeholder com ancoragem 1920×1080.
/// </summary>
public static class ScreenFlowPlaceholderFactory
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public static Canvas EnsureCanvas(Transform parent, string name = "ScreenFlowCanvas")
    {
        Canvas existing = parent.GetComponentInChildren<Canvas>();
        if (existing != null)
            return existing;

        GameObject canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        StretchFull(canvasGo.GetComponent<RectTransform>());
        return canvas;
    }

    public static GameObject CreatePanel(Transform parent, string name, Color background)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = background;
        StretchFull(panel.GetComponent<RectTransform>());
        return panel;
    }

    public static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject buttonGo = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        button.colors = colors;

        CreateText(buttonGo.transform, label, 28, TextAlignmentOptions.Center, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    public static TMP_Text CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        return tmp;
    }

    public static TMP_InputField CreateInputField(Transform parent, string placeholder, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject root = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        root.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(root.transform, false);
        StretchFull(textArea.GetComponent<RectTransform>());

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGo.transform.SetParent(textArea.transform, false);
        StretchFull(placeholderGo.GetComponent<RectTransform>());
        TMP_Text placeholderTmp = placeholderGo.GetComponent<TextMeshProUGUI>();
        placeholderTmp.text = placeholder;
        placeholderTmp.fontSize = 24;
        placeholderTmp.color = new Color(1f, 1f, 1f, 0.45f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(textArea.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        TMP_Text textTmp = textGo.GetComponent<TextMeshProUGUI>();
        textTmp.fontSize = 24;
        textTmp.color = Color.white;

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.textViewport = textArea.GetComponent<RectTransform>();
        input.textComponent = textTmp;
        input.placeholder = placeholderTmp;
        return input;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void ApplyMenuCursor()
    {
        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().SetDefaultCursor();
    }
}
