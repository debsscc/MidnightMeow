// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: Timer de bleed-out exibido para ambos os jogadores durante reviver MP.
// ----------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DownedReviveTimerHud : MonoBehaviour
{
    public static DownedReviveTimerHud Instance { get; private set; }

    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _titleLabel;
    private TextMeshProUGUI _timerLabel;
    private TextMeshProUGUI _subtitleLabel;
    private Image _panelImage;
    private float _topOffset = -168f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUiIfNeeded();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static DownedReviveTimerHud EnsureOnLayer(Transform parent, DownedPlayerConfig config = null)
    {
        if (Instance != null)
        {
            if (Instance.transform.parent != parent)
                Instance.transform.SetParent(parent, false);

            Instance.ApplyLayout(config);
            Instance.BuildUiIfNeeded();
            return Instance;
        }

        GameObject go = new GameObject(nameof(DownedReviveTimerHud), typeof(RectTransform), typeof(DownedReviveTimerHud));
        go.transform.SetParent(parent, false);
        DownedReviveTimerHud hud = go.GetComponent<DownedReviveTimerHud>();
        hud.ApplyLayout(config);
        hud.BuildUiIfNeeded();
        return hud;
    }

    public void SetVisible(bool visible)
    {
        BuildUiIfNeeded();
        if (_canvasGroup != null)
            _canvasGroup.alpha = visible ? 1f : 0f;

        gameObject.SetActive(visible);
    }

    public void Refresh(
        DownedPlayerConfig config,
        bool visible,
        int secondsRemaining,
        bool paused,
        bool isLocalDowned,
        float pulse01)
    {
        BuildUiIfNeeded();
        ApplyLayout(config);

        if (!visible)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        secondsRemaining = Mathf.Max(0, secondsRemaining);
        _timerLabel.text = secondsRemaining.ToString("00");

        DownedPlayerConfig resolved = config ?? DownedPlayerConfigUtility.Resolve();
        _titleLabel.text = isLocalDowned
            ? resolved.GetScreenTimerOwnerTitle()
            : resolved.GetScreenTimerAllyTitle();

        _subtitleLabel.text = paused
            ? resolved.GetScreenTimerRevivingSubtitle()
            : isLocalDowned
                ? resolved.GetScreenTimerOwnerSubtitle()
                : resolved.GetScreenTimerAllySubtitle();

        float scale = 1f + pulse01 * 0.08f;
        _timerLabel.rectTransform.localScale = Vector3.one * scale;

        Color timerColor = Color.Lerp(new Color(1f, 0.82f, 0.82f, 1f), new Color(1f, 0.35f, 0.35f, 1f), pulse01);
        _timerLabel.color = timerColor;

        if (_panelImage != null)
        {
            Color panel = new Color(0.12f, 0.02f, 0.04f, 0.72f + pulse01 * 0.12f);
            _panelImage.color = panel;
        }
    }

    private void ApplyLayout(DownedPlayerConfig config)
    {
        DownedPlayerConfig resolved = config ?? DownedPlayerConfigUtility.Resolve();
        _topOffset = resolved != null ? resolved.screenTimerTopOffset : -168f;

        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(420f, 132f);
        root.anchoredPosition = new Vector2(0f, _topOffset);
    }

    private void BuildUiIfNeeded()
    {
        if (_timerLabel != null)
            return;

        ApplyLayout(null);

        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        Stretch(panelRect);
        _panelImage = panelGo.GetComponent<Image>();
        _panelImage.color = new Color(0.12f, 0.02f, 0.04f, 0.78f);
        _panelImage.raycastTarget = false;

        _titleLabel = CreateLabel("Title", new Vector2(0f, -16f), 26f, FontStyles.Bold);
        _timerLabel = CreateLabel("Timer", new Vector2(0f, -58f), 52f, FontStyles.Bold);
        _subtitleLabel = CreateLabel("Subtitle", new Vector2(0f, -104f), 20f, FontStyles.Normal);

        _titleLabel.color = new Color(1f, 0.72f, 0.72f, 1f);
        _timerLabel.color = new Color(1f, 0.45f, 0.45f, 1f);
        _subtitleLabel.color = new Color(0.95f, 0.88f, 0.88f, 0.95f);
    }

    private TextMeshProUGUI CreateLabel(string name, Vector2 anchoredPosition, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(380f, fontSize + 12f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.raycastTarget = false;
        label.text = string.Empty;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
