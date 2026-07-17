//--------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 16/07/2026
// DESCRIÇÃO: Botão de feedback do playtest posicionado no canto inferior direito.
//--------------------------------

using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class PlaytestFeedbackButton : MonoBehaviour
{
    [SerializeField] private string formUrl =
        "https://docs.google.com/forms/d/e/1FAIpQLScqrERAjHtXbsp-kTXYh86otM1uvqKOICOwL0JFGYLe5203aw/viewform?usp=sharing&ouid=104196659444550947531";

    [SerializeField] private ScreenVisualTheme theme;
    [SerializeField] private bool buildIfMissing = true;

    private Button _button;

    private void Awake()
    {
        if (theme == null)
            theme = Resources.Load<ScreenVisualTheme>("DefaultScreenVisualTheme");

        if (buildIfMissing && _button == null)
            BuildUi();

        if (_button != null)
            _button.onClick.AddListener(OpenForm);
    }

    public void ApplyTheme(ScreenVisualTheme visualTheme)
    {
        theme = visualTheme;
    }

    public static void EnsureOnCanvas(Canvas canvas, ScreenVisualTheme visualTheme = null)
    {
        if (canvas == null || canvas.GetComponentInChildren<PlaytestFeedbackButton>(true) != null)
            return;

        GameObject go = new GameObject("PlaytestFeedbackButton", typeof(RectTransform), typeof(PlaytestFeedbackButton));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        PlaytestFeedbackButton component = go.GetComponent<PlaytestFeedbackButton>();
        if (visualTheme != null)
            component.theme = visualTheme;
    }

    private void BuildUi()
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 24f);
        Vector2 size = theme != null ? theme.feedbackButtonSize : new Vector2(280f, 72f);
        rt.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        if (theme != null && theme.feedbackButtonSprite != null)
        {
            image.sprite = theme.feedbackButtonSprite;
            image.color = Color.white;
        }
        else
        {
            image.color = theme != null ? theme.feedbackButtonColor : new Color(0.2f, 0.55f, 0.95f, 0.95f);
        }

        LoadingProgressUtility.ApplySolidSprite(image);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.transform.SetParent(transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        Text label = labelGo.GetComponent<Text>();
        label.text = "Feedback Playtest";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.resizeTextForBestFit = true;

        _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = image;
    }

    private void OpenForm()
    {
        if (!string.IsNullOrEmpty(formUrl))
            Application.OpenURL(formUrl);
    }
}
