// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: HUD de objetivo da fase. Fase 1: banner + contador x/y de buracos.
// ----------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PhaseObjectiveHud : MonoBehaviour
{
    [SerializeField] private PhaseObjectiveHudVisuals visuals;
    [SerializeField] private bool buildVisualsIfMissing = true;

    [Header("Legacy fallback (Fase 2+)")]
    [SerializeField] private Text legacyStatusText;
    [SerializeField] private bool buildLegacyTextIfMissing = true;

    private string _legacyStatus = "Buracos: -/-";
    private float _carriageProgressPercent;
    private int _holesSealed;
    private int _totalHoles;
    private int _enemiesAlive;
    private CarriageController _subscribedCarriage;

    private RectTransform _holesRoot;
    private Image _bannerImage;
    private TextMeshProUGUI _titleLabel;
    private Image _counterImage;
    private TextMeshProUGUI _counterLabel;
    private bool _holesUiBuilt;

    private void Awake() => EnsureConfigured();

    private void OnEnable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged += HandleObjectiveStatusChanged;
        CarriageController.OnInstanceAvailable += HandleCarriageAvailable;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        if (PhaseObjectiveStatusUtility.HasNetworkObjectiveStatus)
            _enemiesAlive = PhaseObjectiveStatusUtility.CachedEnemiesAlive;

        PhaseObjectiveStatusUtility.CountSealedHoles(out _holesSealed, out _totalHoles);

        TrySubscribeCarriage(CarriageController.Instance);
        RebuildStatusText();
    }

    private void OnDisable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged -= HandleObjectiveStatusChanged;
        CarriageController.OnInstanceAvailable -= HandleCarriageAvailable;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        UnsubscribeCarriage();
    }

    private void HandleCarriageAvailable(CarriageController carriage) => TrySubscribeCarriage(carriage);

    private void TrySubscribeCarriage(CarriageController carriage)
    {
        if (carriage == null || carriage == _subscribedCarriage)
            return;

        UnsubscribeCarriage();
        _subscribedCarriage = carriage;
        _subscribedCarriage.PathProgressVariable.OnValueChanged += HandleCarriageProgressChanged;
        HandleCarriageProgressChanged(0f, _subscribedCarriage.PathProgress);
    }

    private void UnsubscribeCarriage()
    {
        if (_subscribedCarriage == null)
            return;

        _subscribedCarriage.PathProgressVariable.OnValueChanged -= HandleCarriageProgressChanged;
        _subscribedCarriage = null;
    }

    private void HandleLocaleChanged(Locale _) => RebuildStatusText();

    private void HandleObjectiveStatusChanged(int holesSealed, int totalHoles, int enemiesAlive)
    {
        _holesSealed = holesSealed;
        _totalHoles = totalHoles;
        _enemiesAlive = enemiesAlive;
        RebuildStatusText();
    }

    private void HandleCarriageProgressChanged(float previous, float current)
    {
        _carriageProgressPercent = Mathf.Clamp01(current) * 100f;
        RebuildStatusText();
    }

    public void EnsureConfigured()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (visuals == null)
            visuals = PhaseObjectiveHudVisuals.LoadCached();

        EnsureRootRect();
        EnsureHolesVisualLayout();
        EnsureLegacyText();
        UpdateUI();
    }

    private void EnsureRootRect()
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        rt.sizeDelta = new Vector2(640f, 240f);
    }

    private void EnsureHolesVisualLayout()
    {
        if (_holesUiBuilt || !buildVisualsIfMissing)
            return;

        if (visuals == null ||
            (visuals.SealHolesBanner == null && visuals.SealHolesCounterFrame == null))
            return;

        _holesRoot = CreateChild("SealHolesHud", stretch: true);

        _bannerImage = CreateImageChild(_holesRoot, "ObjectiveBanner", new Vector2(0.5f, 1f), new Vector2(0f, -4f),
            new Vector2(520f, 92f));
        if (visuals.SealHolesBanner != null)
            _bannerImage.sprite = visuals.SealHolesBanner;
        _bannerImage.preserveAspect = true;
        _bannerImage.raycastTarget = false;

        _titleLabel = CreateTmpChild(_bannerImage.transform, "ObjectiveTitle", Vector2.zero, Vector2.one,
            new Vector2(24f, 15f), new Vector2(-24f, -1f));
        _titleLabel.alignment = TextAlignmentOptions.Center;
        _titleLabel.fontSize = 28f;
        _titleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _titleLabel.color = new Color(0.28f, 0.28f, 0.3f, 1f);
        _titleLabel.enableAutoSizing = true;
        _titleLabel.fontSizeMin = 18f;
        _titleLabel.fontSizeMax = 30f;
        _titleLabel.raycastTarget = false;

        _counterImage = CreateImageChild(_holesRoot, "HolesCounter", new Vector2(0.5f, 1f), new Vector2(0f, -108f),
            new Vector2(340f, 124f));
        if (visuals.SealHolesCounterFrame != null)
            _counterImage.sprite = visuals.SealHolesCounterFrame;
        _counterImage.preserveAspect = true;
        _counterImage.raycastTarget = false;

        // Ícone já vem na sprite à esquerda; o x/y fica na área branca à direita.
        // Nudge: +2px direita, +8px baixo (4 + 4) em relação ao centro da área.
        _counterLabel = CreateTmpChild(_counterImage.transform, "HolesCount",
            new Vector2(0.32f, 0f), Vector2.one,
            new Vector2(10f, 10f), new Vector2(-16f, -26f));
        _counterLabel.alignment = TextAlignmentOptions.Center;
        _counterLabel.fontSize = 42f;
        _counterLabel.fontStyle = FontStyles.Bold;
        _counterLabel.color = Color.black;
        _counterLabel.enableAutoSizing = true;
        _counterLabel.fontSizeMin = 28f;
        _counterLabel.fontSizeMax = 48f;
        _counterLabel.raycastTarget = false;

        _holesUiBuilt = true;
    }

    private void EnsureLegacyText()
    {
        if (legacyStatusText == null)
            legacyStatusText = GetComponent<Text>();

        if (legacyStatusText == null && buildLegacyTextIfMissing && !_holesUiBuilt)
            legacyStatusText = CreateFallbackLegacyText();

        if (legacyStatusText != null)
            legacyStatusText.gameObject.SetActive(!_holesUiBuilt || ResolveMode() != HudMode.SealHoles);
    }

    private Text CreateFallbackLegacyText()
    {
        GameObject textGo = new GameObject("PhaseObjectiveText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text label = textGo.GetComponent<Text>();
        label.alignment = TextAnchor.UpperCenter;
        label.color = Color.white;
        GameplayUiFonts.Apply(label);
        label.fontSize = 22;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = _legacyStatus;
        return label;
    }

    private void RebuildStatusText()
    {
        HudMode mode = ResolveMode();
        switch (mode)
        {
            case HudMode.KillBoss:
                _legacyStatus = UiLocalization.FormatObjectiveDefeatBoss(_enemiesAlive);
                break;
            case HudMode.Carriage:
                int remainingCarriage = Mathf.Max(0, _totalHoles - _holesSealed);
                _legacyStatus = UiLocalization.FormatObjectiveCarriageStatus(
                    _carriageProgressPercent,
                    _holesSealed,
                    _totalHoles,
                    remainingCarriage,
                    _enemiesAlive);
                break;
            default:
                _legacyStatus = UiLocalization.FormatObjectiveHolesCount(_holesSealed, _totalHoles);
                break;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        HudMode mode = ResolveMode();
        bool showHolesVisual = mode == HudMode.SealHoles && _holesUiBuilt;

        if (_holesRoot != null)
            _holesRoot.gameObject.SetActive(showHolesVisual);

        if (showHolesVisual)
        {
            if (_titleLabel != null)
                _titleLabel.text = UiLocalization.GetObjectiveSealHolesTitle();

            if (_counterLabel != null)
                _counterLabel.text = UiLocalization.FormatObjectiveHolesCount(_holesSealed, _totalHoles);
        }

        if (legacyStatusText != null)
        {
            legacyStatusText.gameObject.SetActive(!showHolesVisual);
            if (!showHolesVisual)
                legacyStatusText.text = _legacyStatus;
        }
    }

    private static HudMode ResolveMode()
    {
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (catalog != null && catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry))
        {
            if (entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss)
                return HudMode.KillBoss;
            if (entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.CarriageReachEnd)
                return HudMode.Carriage;
        }

        return HudMode.SealHoles;
    }

    private RectTransform CreateChild(string name, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        return rt;
    }

    private static Image CreateImageChild(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 anchoredPos,
        Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = Color.white;
        return image;
    }

    private static TextMeshProUGUI CreateTmpChild(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        GameplayUiFonts.Apply(label);
        return label;
    }

    private enum HudMode
    {
        SealHoles,
        Carriage,
        KillBoss
    }
}
