// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Indicação para aliado vivo: permanecer na zona de reviver (sem botão de interação). Traduzido
// ---------------------------------------------------------------- 

using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

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
        if (_canvas == null || !_selfHealth.IsSpawned || downedConfig == null) return;

        bool nearDowned = _selfHealth.CanFight && HasDownedTeammateNearby();
        bool inZone = _revive.IsContributingToRevive;
        bool show = nearDowned || inZone;

        SetVisible(show);
        if (!show) return;

        _canvas.transform.position = transform.position + offset;

        if (_promptLabel != null)
            _promptLabel.gameObject.SetActive(nearDowned && !inZone);

        if (_progressFill != null)
        {
            _progressFill.transform.parent.gameObject.SetActive(inZone);
            if (inZone)
                _progressFill.fillAmount = GetActiveDownedProgress();
        }
    }

    private float GetActiveDownedProgress()
    {
        foreach (var h in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!h.IsSpawned || !h.CanBeRevived) continue;
            if (DownedReviveZoneSystem.IsAllyInsideReviveZone(h, _selfHealth, downedConfig))
                return h.ReviveProgress;
        }

        return 0f;
    }

    private bool HasDownedTeammateNearby()
    {
        float range = downedConfig.reviveZoneRadius * 1.5f;
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
        rect.sizeDelta = new Vector2(2.4f, 0.6f);

        var labelGo = new GameObject("Prompt");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(2.4f, 0.3f);
        labelRect.anchoredPosition = new Vector2(0f, 0.15f);
        _promptLabel = labelGo.AddComponent<TextMeshProUGUI>();
        _promptLabel.text = IsPortuguese() ? "Entre na área verde para reviver" : "Enter the green area to revive";
        _promptLabel.fontSize = 2f;
        _promptLabel.alignment = TextAlignmentOptions.Center;
        _promptLabel.color = new Color(0.85f, 1f, 0.9f, 1f);

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
        _progressFill.color = new Color(0.35f, 1f, 0.55f, 1f);
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }
}
