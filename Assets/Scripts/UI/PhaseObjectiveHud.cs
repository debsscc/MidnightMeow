// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: HUD de objetivo — Fase 1 buracos, Fase 2 carruagem (banner + barra).
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

    [Header("Legacy fallback")]
    [SerializeField] private Text legacyStatusText;
    [SerializeField] private bool buildLegacyTextIfMissing = true;

    private string _legacyStatus = "Buracos: -/-";
    private float _carriageProgressNormalized;
    private int _holesSealed;
    private int _totalHoles;
    private int _enemiesAlive;
    private CarriageController _subscribedCarriage;

    private RectTransform _holesRoot;
    private Image _holesBannerImage;
    private TextMeshProUGUI _holesTitleLabel;
    private Image _holesCounterImage;
    private TextMeshProUGUI _holesCounterLabel;
    private bool _holesUiBuilt;

    private RectTransform _carriageRoot;
    private Image _carriageBannerImage;
    private TextMeshProUGUI _carriageTitleLabel;
    private RectTransform _carriageBarTrack;
    private Image _carriageBarBackground;
    private Image _carriageBarRemaining;
    private Image _carriageBarFrame;
    private RectTransform _carriageFollower;
    private bool _carriageUiBuilt;

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
        _carriageProgressNormalized = Mathf.Clamp01(current);
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
        EnsureCarriageVisualLayout();
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
        rt.sizeDelta = new Vector2(720f, 260f);
    }

    private void EnsureHolesVisualLayout()
    {
        if (_holesUiBuilt || !buildVisualsIfMissing)
            return;

        if (visuals == null ||
            (visuals.SealHolesBanner == null && visuals.SealHolesCounterFrame == null))
            return;

        _holesRoot = CreateChild("SealHolesHud", stretch: true);

        _holesBannerImage = CreateImageChild(_holesRoot, "ObjectiveBanner", new Vector2(0.5f, 1f), new Vector2(0f, -4f),
            new Vector2(520f, 92f));
        if (visuals.SealHolesBanner != null)
            _holesBannerImage.sprite = visuals.SealHolesBanner;
        _holesBannerImage.preserveAspect = true;
        _holesBannerImage.raycastTarget = false;

        _holesTitleLabel = CreateTmpChild(_holesBannerImage.transform, "ObjectiveTitle", Vector2.zero, Vector2.one,
            new Vector2(24f, 15f), new Vector2(-24f, -1f));
        StyleTitleLabel(_holesTitleLabel);

        _holesCounterImage = CreateImageChild(_holesRoot, "HolesCounter", new Vector2(0.5f, 1f), new Vector2(0f, -108f),
            new Vector2(340f, 124f));
        if (visuals.SealHolesCounterFrame != null)
            _holesCounterImage.sprite = visuals.SealHolesCounterFrame;
        _holesCounterImage.preserveAspect = true;
        _holesCounterImage.raycastTarget = false;

        _holesCounterLabel = CreateTmpChild(_holesCounterImage.transform, "HolesCount",
            new Vector2(0.32f, 0f), Vector2.one,
            new Vector2(10f, 10f), new Vector2(-16f, -26f));
        _holesCounterLabel.alignment = TextAlignmentOptions.Center;
        _holesCounterLabel.fontSize = 42f;
        _holesCounterLabel.fontStyle = FontStyles.Bold;
        _holesCounterLabel.color = Color.black;
        _holesCounterLabel.enableAutoSizing = true;
        _holesCounterLabel.fontSizeMin = 28f;
        _holesCounterLabel.fontSizeMax = 48f;
        _holesCounterLabel.raycastTarget = false;

        _holesUiBuilt = true;
    }

    private void EnsureCarriageVisualLayout()
    {
        if (_carriageUiBuilt || !buildVisualsIfMissing)
            return;

        if (visuals == null ||
            (visuals.CarriageBanner == null && visuals.CarriageBarFrame == null && visuals.CarriageBarBackground == null))
            return;

        _carriageRoot = CreateChild("CarriageHud", stretch: true);

        _carriageBannerImage = CreateImageChild(_carriageRoot, "ObjectiveBanner", new Vector2(0.5f, 1f), new Vector2(0f, -4f),
            new Vector2(520f, 92f));
        if (visuals.CarriageBanner != null)
            _carriageBannerImage.sprite = visuals.CarriageBanner;
        _carriageBannerImage.preserveAspect = true;
        _carriageBannerImage.raycastTarget = false;

        _carriageTitleLabel = CreateTmpChild(_carriageBannerImage.transform, "ObjectiveTitle", Vector2.zero, Vector2.one,
            new Vector2(24f, 15f), new Vector2(-24f, -1f));
        StyleTitleLabel(_carriageTitleLabel);

        // Trilho: Baixo (fundo) → branco restante → Moldura → ícone follower (como loading).
        RectTransform barRoot = CreateChildUnder(_carriageRoot, "CarriageProgress");
        barRoot.anchorMin = new Vector2(0.5f, 1f);
        barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = new Vector2(0f, -100f);
        barRoot.sizeDelta = new Vector2(640f, 96f);

        _carriageBarTrack = CreateChildUnder(barRoot, "BarTrack");
        _carriageBarTrack.anchorMin = new Vector2(0.5f, 0.5f);
        _carriageBarTrack.anchorMax = new Vector2(0.5f, 0.5f);
        _carriageBarTrack.pivot = new Vector2(0.5f, 0.5f);
        _carriageBarTrack.anchoredPosition = new Vector2(0f, -2f);
        _carriageBarTrack.sizeDelta = new Vector2(528f, 44f);

        _carriageBarBackground = CreateImageChild(_carriageBarTrack, "BarBackground", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(560f, 48f));
        if (visuals.CarriageBarBackground != null)
            _carriageBarBackground.sprite = visuals.CarriageBarBackground;
        _carriageBarBackground.preserveAspect = false;
        _carriageBarBackground.color = new Color(0.92f, 0.18f, 0.18f, 1f);
        _carriageBarBackground.raycastTarget = false;

        _carriageBarRemaining = CreateImageChild(_carriageBarTrack, "BarRemaining", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(528f, 44f));
        LoadingProgressUtility.ApplySolidSprite(_carriageBarRemaining);
        _carriageBarRemaining.color = Color.white;
        _carriageBarRemaining.type = Image.Type.Filled;
        _carriageBarRemaining.fillMethod = Image.FillMethod.Horizontal;
        _carriageBarRemaining.fillOrigin = (int)Image.OriginHorizontal.Left;
        _carriageBarRemaining.fillAmount = 1f;
        _carriageBarRemaining.raycastTarget = false;

        _carriageBarFrame = CreateImageChild(barRoot, "BarFrame", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(640f, 96f));
        if (visuals.CarriageBarFrame != null)
            _carriageBarFrame.sprite = visuals.CarriageBarFrame;
        _carriageBarFrame.preserveAspect = true;
        _carriageBarFrame.raycastTarget = false;

        if (visuals.CarriageFollowerIcon != null)
        {
            Image followerImage = CreateImageChild(barRoot, "CarriageFollower", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(96f, 72f));
            followerImage.sprite = visuals.CarriageFollowerIcon;
            followerImage.preserveAspect = true;
            followerImage.raycastTarget = false;
            _carriageFollower = followerImage.rectTransform;
            _carriageFollower.pivot = new Vector2(0.5f, 0.35f);
            _carriageFollower.SetAsLastSibling();
        }

        _carriageUiBuilt = true;
    }

    private static void StyleTitleLabel(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        label.color = new Color(0.28f, 0.28f, 0.3f, 1f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 30f;
        label.raycastTarget = false;
    }

    private void EnsureLegacyText()
    {
        if (legacyStatusText == null)
            legacyStatusText = GetComponent<Text>();

        bool hasVisual = _holesUiBuilt || _carriageUiBuilt;
        if (legacyStatusText == null && buildLegacyTextIfMissing && !hasVisual)
            legacyStatusText = CreateFallbackLegacyText();

        if (legacyStatusText != null)
        {
            HudMode mode = ResolveMode();
            bool useLegacy = mode == HudMode.KillBoss
                             || (mode == HudMode.SealHoles && !_holesUiBuilt)
                             || (mode == HudMode.Carriage && !_carriageUiBuilt);
            legacyStatusText.gameObject.SetActive(useLegacy);
        }
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
                _legacyStatus = UiLocalization.Format(
                    "objective.carriage_progress",
                    "Carruagem: {0}%",
                    Mathf.RoundToInt(_carriageProgressNormalized * 100f));
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
        bool showCarriageVisual = mode == HudMode.Carriage && _carriageUiBuilt;

        if (_holesRoot != null)
            _holesRoot.gameObject.SetActive(showHolesVisual);

        if (_carriageRoot != null)
            _carriageRoot.gameObject.SetActive(showCarriageVisual);

        if (showHolesVisual)
        {
            if (_holesTitleLabel != null)
                _holesTitleLabel.text = UiLocalization.GetObjectiveSealHolesTitle();

            if (_holesCounterLabel != null)
                _holesCounterLabel.text = UiLocalization.FormatObjectiveHolesCount(_holesSealed, _totalHoles);
        }

        if (showCarriageVisual)
        {
            if (_carriageTitleLabel != null)
                _carriageTitleLabel.text = UiLocalization.GetObjectiveProtectCarriageTitle();

            float remaining = 1f - _carriageProgressNormalized;
            if (_carriageBarRemaining != null)
                _carriageBarRemaining.fillAmount = remaining;

            // Ícone acompanha a ponta do trajeto (0→1), igual ao follower do loading.
            if (_carriageBarTrack != null && _carriageFollower != null)
                LoadingProgressUtility.SetFollowerAlongTrack(
                    _carriageBarTrack, _carriageFollower, _carriageProgressNormalized, -8f);
        }

        if (legacyStatusText != null)
        {
            bool useLegacy = !showHolesVisual && !showCarriageVisual;
            legacyStatusText.gameObject.SetActive(useLegacy);
            if (useLegacy)
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

    private static RectTransform CreateChildUnder(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
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
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (Mathf.Approximately(anchor.y, 1f) && Mathf.Approximately(anchor.x, 0.5f))
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
