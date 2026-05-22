using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prompt e barra de reviver acima do jogador que está reanimando um aliado.
/// </summary>
[RequireComponent(typeof(NetworkPlayerRevive), typeof(NetworkPlayerHealth))]
public class RevivePromptWorldUI : MonoBehaviour
{
    [SerializeField] private DownedPlayerConfig downedConfig;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1.1f, 0.1f);

    private NetworkPlayerRevive _revive;
    private NetworkPlayerHealth _selfHealth;

    private Canvas _canvas;
    private TextMeshProUGUI _promptLabel;
    private Image _progressFill;

    private void Awake()
    {
        _revive = GetComponent<NetworkPlayerRevive>();
        _selfHealth = GetComponent<NetworkPlayerHealth>();

        if (downedConfig == null)
            downedConfig = _selfHealth.DownedConfig;

        BuildUI();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (_canvas == null || !_selfHealth.IsSpawned) return;

        bool showPrompt = _selfHealth.CanFight && HasDownedTeammateNearby();
        bool showProgress = _revive.IsReviving;

        SetVisible(showPrompt || showProgress);
        if (!showPrompt && !showProgress) return;

        _canvas.transform.position = transform.position + offset;

        if (_promptLabel != null)
            _promptLabel.gameObject.SetActive(showPrompt && !showProgress);

        if (_progressFill != null)
        {
            _progressFill.transform.parent.gameObject.SetActive(showProgress);
            if (showProgress)
                _progressFill.fillAmount = FindTargetProgress();
        }
    }

    private float FindTargetProgress()
    {
        float best = 0f;
        foreach (var h in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!h.IsSpawned || h.OwnerClientId == _selfHealth.OwnerClientId) continue;
            if (h.ReviveProgress > best)
                best = h.ReviveProgress;
        }

        return best;
    }

    private bool HasDownedTeammateNearby()
    {
        float range = downedConfig != null ? downedConfig.reviveRange : 2.5f;
        Vector2 pos = transform.position;

        foreach (var h in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!h.IsSpawned || h.OwnerClientId == _selfHealth.OwnerClientId) continue;
            if (!h.CanBeRevived) continue;
            if (Vector2.Distance(pos, h.transform.position) <= range)
                return true;
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        var root = new GameObject("RevivePromptUI");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 40f;

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.2f, 0.6f);

        var labelGo = new GameObject("Prompt");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(2.2f, 0.3f);
        labelRect.anchoredPosition = new Vector2(0f, 0.15f);
        _promptLabel = labelGo.AddComponent<TextMeshProUGUI>();
        _promptLabel.text = "Interagir para Ressuscitar";
        _promptLabel.fontSize = 2.2f;
        _promptLabel.alignment = TextAlignmentOptions.Center;
        _promptLabel.color = new Color(0.9f, 0.95f, 1f, 1f);

        var barBg = new GameObject("ReviveProgressBg");
        barBg.transform.SetParent(root.transform, false);
        var barRect = barBg.AddComponent<RectTransform>();
        barRect.sizeDelta = barSize;
        barRect.anchoredPosition = new Vector2(0f, -0.1f);
        barBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barBg.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        _progressFill = fillGo.AddComponent<Image>();
        _progressFill.color = new Color(0.25f, 0.8f, 1f, 1f);
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
}
