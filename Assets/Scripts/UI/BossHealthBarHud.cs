//--------------------------------------------------
// FUNÇÃO: Barra de vida cinematográfica do boss (HUD) — só fase KillBoss.
// Layout: banner Objetivo + barra larga (moldura / fill vermelho), estilo Fase 1–2.
//--------------------------------------------------

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class BossHealthBarHud : MonoBehaviour
{
    private const float BarFrameWidth = 1100f;
    private const float BarFrameHeight = 100f;
    private const float BarTrackWidth = 820f;
    private const float BarTrackHeight = 40f;
    private const float BarTrackYOffset = -8f;
    private const float BarFillWidth = 590.2f;
    private const float BarFillHeight = 40f;

    [SerializeField] private string defaultBossName = "Rei Rato";
    [SerializeField] private PhaseObjectiveHudVisuals visuals;
    [SerializeField] private float pollInterval = 0.25f;
    [SerializeField] private float mainLerpSpeed = 14f;
    [SerializeField] private float trailLerpSpeed = 4.5f;
    [SerializeField] private float trailDelay = 0.12f;

    private RectTransform _root;
    private CanvasGroup _canvasGroup;
    private Image _bannerImage;
    private TextMeshProUGUI _titleLabel;
    private Image _barBackground;
    private Image _fill;
    private Image _trail;
    private Image _barFrame;
    private TextMeshProUGUI _valueLabel;

    private HealthComponent _boundHealth;
    private BossEnemyMarker _boundBoss;
    private float _pollTimer;
    private float _targetNormalized = 1f;
    private float _displayNormalized = 1f;
    private float _trailNormalized = 1f;
    private float _trailHoldTimer;
    private bool _styledUiBuilt;

    private static BossHealthBarHud _instance;

    public static bool HasActiveBinding =>
        _instance != null && _instance._boundHealth != null && !_instance._boundHealth.IsDead;

    public static bool IsBoundToBoss(GameObject bossRoot)
    {
        return HasActiveBinding
               && bossRoot != null
               && _instance._boundBoss != null
               && _instance._boundBoss.gameObject == bossRoot;
    }

    public static BossHealthBarHud EnsureOnLayer(Transform layer)
    {
        if (layer == null)
            return null;

        BossHealthBarHud existing = layer.GetComponentInChildren<BossHealthBarHud>(true);
        if (existing != null)
        {
            existing.EnsureConfigured();
            return existing;
        }

        GameObject go = new GameObject(
            "BossHealthBarHud",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(BossHealthBarHud));
        go.transform.SetParent(layer, false);
        BossHealthBarHud hud = go.GetComponent<BossHealthBarHud>();
        hud.EnsureConfigured();
        return hud;
    }

    public void EnsureConfigured()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (!enabled)
            enabled = true;

        if (visuals == null)
            visuals = PhaseObjectiveHudVisuals.LoadCached();

        EnsureCanvasGroup();
        BuildStyledUiIfNeeded();
        RefreshTitle();
        TryBindBoss();
    }

    private void Awake() => EnsureConfigured();

    private void OnEnable()
    {
        _instance = this;
        EnsureCanvasGroup();
        BossEnemyMarker.OnBossAvailable += HandleBossAvailable;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        _pollTimer = 0f;
        RefreshTitle();
        TryBindBoss();
    }

    private void OnDisable()
    {
        BossEnemyMarker.OnBossAvailable -= HandleBossAvailable;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        UnbindHealth();
        if (_instance == this)
            _instance = null;
    }

    private void HandleBossAvailable(BossEnemyMarker _) => TryBindBoss();
    private void HandleLocaleChanged(Locale _) => RefreshTitle();

    private void Update()
    {
        if (!BossPhaseUtility.IsKillBossPhaseActive())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        _pollTimer -= Time.unscaledDeltaTime;
        if (_pollTimer <= 0f)
        {
            _pollTimer = pollInterval;
            if (_boundHealth == null || _boundHealth.IsDead || !_boundHealth.gameObject.activeInHierarchy)
                TryBindBoss();
        }

        if (_boundHealth != null)
            TickBarAnimation();
    }

    private void TryBindBoss()
    {
        BossEnemyMarker best = null;
        HealthComponent bestHealth = null;

        IReadOnlyList<BossEnemyMarker> active = BossEnemyMarker.ActiveBosses;
        for (int i = 0; i < active.Count; i++)
            ConsiderBoss(active[i], ref best, ref bestHealth);

        if (best == null)
        {
            BossEnemyMarker[] bosses = Object.FindObjectsByType<BossEnemyMarker>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
                ConsiderBoss(bosses[i], ref best, ref bestHealth);
        }

        if (best == null || bestHealth == null)
        {
            UnbindHealth();
            ApplyUnboundPlaceholder();
            return;
        }

        if (best == _boundBoss && _boundHealth == bestHealth)
            return;

        UnbindHealth();
        _boundBoss = best;
        _boundHealth = bestHealth;
        _boundHealth.OnHealthChanged.AddListener(HandleHealthChanged);
        _boundHealth.OnDied.AddListener(HandleDied);

        RefreshTitle();

        float current = _boundHealth.CurrentHealth;
        float max = _boundHealth.MaxHealth;
        if (max <= 0f)
            max = 1f;
        if (current <= 0f && !_boundHealth.IsDead)
            current = max;

        HandleHealthChanged(current, max);
        _displayNormalized = _targetNormalized;
        _trailNormalized = _targetNormalized;
        ApplyBarsImmediate();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[BossHealthBarHud] Bind OK → {_boundBoss.name} HP={current}/{max}");
#endif
    }

    private void ApplyUnboundPlaceholder()
    {
        RefreshTitle();
        _targetNormalized = 1f;
        _displayNormalized = 1f;
        _trailNormalized = 1f;
        ApplyBarsImmediate();

        if (_valueLabel != null)
            _valueLabel.text = "—/—";
    }

    private void RefreshTitle()
    {
        if (_titleLabel != null)
            _titleLabel.text = UiLocalization.GetObjectiveDefeatBossTitle();
    }

    private static void ConsiderBoss(
        BossEnemyMarker marker,
        ref BossEnemyMarker best,
        ref HealthComponent bestHealth)
    {
        if (marker == null || !marker.gameObject.activeInHierarchy)
            return;

        HealthComponent health = marker.GetComponent<HealthComponent>();
        if (health == null)
            health = marker.GetComponentInChildren<HealthComponent>(true);
        if (health == null || health.IsDead)
            return;

        if (bestHealth == null || health.CurrentHealth > bestHealth.CurrentHealth)
        {
            best = marker;
            bestHealth = health;
        }
    }

    private void UnbindHealth()
    {
        if (_boundHealth != null)
        {
            _boundHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            _boundHealth.OnDied.RemoveListener(HandleDied);
        }

        _boundHealth = null;
        _boundBoss = null;
    }

    private void HandleHealthChanged(float current, float max)
    {
        float next = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (next < _targetNormalized)
            _trailHoldTimer = trailDelay;

        _targetNormalized = next;
        if (_valueLabel != null)
            _valueLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, current))}/{Mathf.CeilToInt(Mathf.Max(0f, max))}";
    }

    private void HandleDied()
    {
        _targetNormalized = 0f;
        if (_valueLabel != null)
            _valueLabel.text = "0";
    }

    private void TickBarAnimation()
    {
        float dt = Time.unscaledDeltaTime;
        _displayNormalized = Mathf.MoveTowards(_displayNormalized, _targetNormalized, mainLerpSpeed * dt);

        if (_trailHoldTimer > 0f)
            _trailHoldTimer -= dt;
        else
            _trailNormalized = Mathf.MoveTowards(_trailNormalized, _displayNormalized, trailLerpSpeed * dt);

        if (_trailNormalized < _displayNormalized)
            _trailNormalized = _displayNormalized;

        if (_fill != null)
            _fill.fillAmount = _displayNormalized;
        if (_trail != null)
            _trail.fillAmount = _trailNormalized;
    }

    private void ApplyBarsImmediate()
    {
        if (_fill != null)
            _fill.fillAmount = _displayNormalized;
        if (_trail != null)
            _trail.fillAmount = _trailNormalized;
    }

    private void SetVisible(bool visible)
    {
        EnsureCanvasGroup();
        if (_canvasGroup == null)
            return;

        _canvasGroup.ignoreParentGroups = true;
        float alpha = visible ? 1f : 0f;
        if (!Mathf.Approximately(_canvasGroup.alpha, alpha))
            _canvasGroup.alpha = alpha;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void BuildStyledUiIfNeeded()
    {
        _root = GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(0.5f, 1f);
        _root.anchorMax = new Vector2(0.5f, 1f);
        _root.pivot = new Vector2(0.5f, 1f);
        _root.anchoredPosition = new Vector2(0f, -16f);
        _root.sizeDelta = new Vector2(BarFrameWidth + 40f, 220f);

        EnsureCanvasGroup();
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _canvasGroup.ignoreParentGroups = true;
        }

        // Remove Image no root (layout antigo: frame escuro no próprio GO).
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
            Destroy(rootImage);

        if (_styledUiBuilt && transform.Find("ObjectiveBanner") != null && transform.Find("BossBar") != null)
        {
            CacheStyledRefs();
            ApplyBarLayoutMetrics();
            if (BossPhaseUtility.IsKillBossPhaseActive())
            {
                if (_boundHealth == null)
                    ApplyUnboundPlaceholder();
                SetVisible(true);
            }

            return;
        }

        // Migra de layout antigo (Trail/Fill soltos) → limpa e reconstrói.
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        PhaseObjectiveHudVisuals v = visuals != null ? visuals : PhaseObjectiveHudVisuals.LoadCached();

        // Banner Objetivo (mesmo padrão Fase 1/2).
        _bannerImage = CreateImageChild(transform, "ObjectiveBanner", new Vector2(0.5f, 1f), new Vector2(0f, -4f),
            new Vector2(560f, 92f));
        if (v != null && v.BossBanner != null)
            _bannerImage.sprite = v.BossBanner;
        _bannerImage.preserveAspect = true;
        _bannerImage.color = Color.white;
        _bannerImage.raycastTarget = false;

        _titleLabel = CreateTmpChild(_bannerImage.transform, "ObjectiveTitle", Vector2.zero, Vector2.one,
            new Vector2(24f, 15f), new Vector2(-24f, -1f));
        StyleTitleLabel(_titleLabel);
        RefreshTitle();

        // Barra larga: fundo → trail → fill vermelho → moldura.
        RectTransform barRoot = CreateChild(transform, "BossBar");
        barRoot.anchorMin = new Vector2(0.5f, 1f);
        barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = new Vector2(0f, -100f);
        barRoot.sizeDelta = new Vector2(BarFrameWidth, BarFrameHeight);

        RectTransform track = CreateChild(barRoot, "BarTrack");
        track.anchorMin = new Vector2(0.5f, 0.5f);
        track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.anchoredPosition = new Vector2(0f, BarTrackYOffset);
        track.sizeDelta = new Vector2(BarTrackWidth, BarTrackHeight);

        _barBackground = CreateImageChild(track, "BarBackground", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(BarFillWidth, BarFillHeight));
        if (v != null && v.BossBarBackground != null)
        {
            _barBackground.sprite = v.BossBarBackground;
            _barBackground.preserveAspect = false;
            _barBackground.color = new Color(0.15f, 0.08f, 0.08f, 1f);
        }
        else
        {
            LoadingProgressUtility.ApplySolidSprite(_barBackground);
            _barBackground.color = new Color(0.12f, 0.08f, 0.08f, 0.95f);
        }

        _barBackground.raycastTarget = false;

        _trail = CreateImageChild(track, "Trail", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(BarFillWidth, BarFillHeight));
        LoadingProgressUtility.ApplySolidSprite(_trail);
        _trail.color = new Color(0.95f, 0.45f, 0.35f, 0.75f);
        _trail.type = Image.Type.Filled;
        _trail.fillMethod = Image.FillMethod.Horizontal;
        _trail.fillOrigin = (int)Image.OriginHorizontal.Left;
        _trail.fillAmount = 1f;
        _trail.raycastTarget = false;

        _fill = CreateImageChild(track, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(BarFillWidth, BarFillHeight));
        LoadingProgressUtility.ApplySolidSprite(_fill);
        _fill.color = new Color(0.86f, 0.12f, 0.16f, 1f);
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fill.fillAmount = 1f;
        _fill.raycastTarget = false;

        _barFrame = CreateImageChild(barRoot, "BarFrame", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(BarFrameWidth, BarFrameHeight));
        if (v != null && v.BossBarFrame != null)
            _barFrame.sprite = v.BossBarFrame;
        _barFrame.preserveAspect = true;
        _barFrame.color = Color.white;
        _barFrame.raycastTarget = false;

        _valueLabel = CreateTmpChild(track, "Value", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _valueLabel.alignment = TextAlignmentOptions.Center;
        _valueLabel.fontSize = 18f;
        _valueLabel.fontStyle = FontStyles.Bold;
        _valueLabel.color = new Color(1f, 1f, 1f, 0.9f);
        _valueLabel.raycastTarget = false;
        GameplayUiFonts.Apply(_valueLabel);

        _styledUiBuilt = true;

        if (BossPhaseUtility.IsKillBossPhaseActive())
        {
            ApplyUnboundPlaceholder();
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    private void CacheStyledRefs()
    {
        if (_bannerImage == null)
        {
            Transform banner = transform.Find("ObjectiveBanner");
            if (banner != null)
                _bannerImage = banner.GetComponent<Image>();
        }

        if (_titleLabel == null && _bannerImage != null)
        {
            Transform title = _bannerImage.transform.Find("ObjectiveTitle");
            if (title != null)
                _titleLabel = title.GetComponent<TextMeshProUGUI>();
        }

        Transform bar = transform.Find("BossBar");
        if (bar == null)
            return;

        Transform track = bar.Find("BarTrack");
        if (track != null)
        {
            if (_barBackground == null)
            {
                Transform bg = track.Find("BarBackground");
                if (bg != null)
                    _barBackground = bg.GetComponent<Image>();
            }

            if (_trail == null)
            {
                Transform trail = track.Find("Trail");
                if (trail != null)
                    _trail = trail.GetComponent<Image>();
            }

            if (_fill == null)
            {
                Transform fill = track.Find("Fill");
                if (fill != null)
                    _fill = fill.GetComponent<Image>();
            }

            if (_valueLabel == null)
            {
                Transform value = track.Find("Value");
                if (value != null)
                    _valueLabel = value.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_barFrame == null)
        {
            Transform frame = bar.Find("BarFrame");
            if (frame != null)
                _barFrame = frame.GetComponent<Image>();
        }
    }

    private void ApplyBarLayoutMetrics()
    {
        Transform bar = transform.Find("BossBar");
        if (bar == null)
            return;

        RectTransform barRt = bar as RectTransform;
        if (barRt != null)
            barRt.sizeDelta = new Vector2(BarFrameWidth, BarFrameHeight);

        Transform track = bar.Find("BarTrack");
        if (track != null)
        {
            RectTransform trackRt = track as RectTransform;
            trackRt.anchoredPosition = new Vector2(0f, BarTrackYOffset);
            trackRt.sizeDelta = new Vector2(BarTrackWidth, BarTrackHeight);

            SetChildSize(track, "BarBackground", BarFillWidth, BarFillHeight);
            SetChildSize(track, "Trail", BarFillWidth, BarFillHeight);
            SetChildSize(track, "Fill", BarFillWidth, BarFillHeight);
        }

        if (_barFrame != null)
            _barFrame.rectTransform.sizeDelta = new Vector2(BarFrameWidth, BarFrameHeight);

        if (_valueLabel != null)
            _valueLabel.fontStyle = FontStyles.Bold;
    }

    private static void SetChildSize(Transform parent, string childName, float width, float height)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            return;

        RectTransform rt = child as RectTransform;
        if (rt != null)
            rt.sizeDelta = new Vector2(width, height);
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
        GameplayUiFonts.Apply(label);
    }

    private static RectTransform CreateChild(Transform parent, string name)
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
        rt.pivot = Mathf.Approximately(anchor.y, 1f) ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
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
}
