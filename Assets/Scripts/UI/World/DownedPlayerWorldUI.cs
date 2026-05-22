using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI world-space acima do jogador: timer de inconsciência e barra de progresso de reviver.
/// </summary>
[RequireComponent(typeof(NetworkPlayerHealth))]
public class DownedPlayerWorldUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.12f);

    private NetworkPlayerHealth _health;
    private Canvas _canvas;
    private Image _timerFill;
    private Image _reviveFill;
    private TextMeshProUGUI _statusLabel;

    private void Awake()
    {
        _health = GetComponent<NetworkPlayerHealth>();
        BuildUI();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (_canvas == null) return;

        bool show = _health.IsSpawned && (_health.IsUnconscious || _health.ReviveProgress > 0f);
        SetVisible(show);
        if (!show) return;

        _canvas.transform.position = transform.position + offset;

        float duration = Mathf.Max(0.01f, _health.UnconsciousDuration);
        float timerNorm = _health.IsBleedingOut
            ? 0f
            : Mathf.Clamp01(_health.UnconsciousTimeRemaining / duration);

        if (_timerFill != null)
            _timerFill.fillAmount = timerNorm;

        if (_reviveFill != null)
            _reviveFill.fillAmount = _health.ReviveProgress;

        if (_statusLabel != null)
        {
            if (_health.IsBleedingOut)
                _statusLabel.text = "Sem tempo";
            else if (_health.IsReviveTimerPaused)
                _statusLabel.text = "Sendo revivido";
            else
                _statusLabel.text = "Inconsciente";
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        var root = new GameObject("DownedUI");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = offset;

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 40f;

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 0.8f);

        _statusLabel = CreateLabel(root.transform, "Inconsciente", new Vector2(0f, 0.35f));
        _timerFill = CreateBar(root.transform, "TimerBar", new Color(0.85f, 0.2f, 0.2f, 0.9f), new Vector2(0f, 0.05f));
        _reviveFill = CreateBar(root.transform, "ReviveBar", new Color(0.2f, 0.85f, 0.35f, 0.95f), new Vector2(0f, -0.15f));
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 anchoredPos)
    {
        var go = new GameObject("StatusLabel");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 0.25f);
        rect.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 2.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private Image CreateBar(Transform parent, string name, Color fillColor, Vector2 anchoredPos)
    {
        var bgGo = new GameObject(name);
        bgGo.transform.SetParent(parent, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.sizeDelta = barSize;
        bgRect.anchoredPosition = anchoredPos;
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.55f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fill = fillGo.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        return fill;
    }
}
