//--------------------------------------------------
// FUNÇÃO: Barra de vida cinematográfica do boss (HUD) — só fase KillBoss.
//--------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHealthBarHud : MonoBehaviour
{
    [SerializeField] private string defaultBossName = "Rei Rato";
    [SerializeField] private float pollInterval = 0.25f;
    [SerializeField] private float mainLerpSpeed = 14f;
    [SerializeField] private float trailLerpSpeed = 4.5f;
    [SerializeField] private float trailDelay = 0.12f;

    private RectTransform _root;
    private CanvasGroup _canvasGroup;
    private Image _fill;
    private Image _trail;
    private TextMeshProUGUI _nameLabel;
    private TextMeshProUGUI _valueLabel;

    private HealthComponent _boundHealth;
    private BossEnemyMarker _boundBoss;
    private float _pollTimer;
    private float _targetNormalized = 1f;
    private float _displayNormalized = 1f;
    private float _trailNormalized = 1f;
    private float _trailHoldTimer;

    public static BossHealthBarHud EnsureOnLayer(Transform layer)
    {
        if (layer == null)
            return null;

        BossHealthBarHud existing = layer.GetComponentInChildren<BossHealthBarHud>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        GameObject go = new GameObject("BossHealthBarHud", typeof(RectTransform), typeof(BossHealthBarHud));
        go.transform.SetParent(layer, false);
        BossHealthBarHud hud = go.GetComponent<BossHealthBarHud>();
        hud.BuildUi();
        return hud;
    }

    private void Awake()
    {
        if (_root == null)
            BuildUi();
    }

    private void OnEnable()
    {
        _pollTimer = 0f;
        TryBindBoss();
    }

    private void OnDisable() => UnbindHealth();

    private void Update()
    {
        _pollTimer -= Time.unscaledDeltaTime;
        if (_pollTimer <= 0f)
        {
            _pollTimer = pollInterval;
            if (_boundHealth == null || !_boundHealth.isActiveAndEnabled || _boundHealth.IsDead)
                TryBindBoss();
        }

        if (_boundHealth == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        TickBarAnimation();
    }

    private void TryBindBoss()
    {
        BossEnemyMarker[] bosses = Object.FindObjectsByType<BossEnemyMarker>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        BossEnemyMarker best = null;
        HealthComponent bestHealth = null;
        for (int i = 0; i < bosses.Length; i++)
        {
            BossEnemyMarker marker = bosses[i];
            if (marker == null)
                continue;

            if (!marker.TryGetComponent<HealthComponent>(out var health) || health.IsDead)
                continue;

            if (bestHealth == null || health.CurrentHealth > bestHealth.CurrentHealth)
            {
                best = marker;
                bestHealth = health;
            }
        }

        if (best == null)
        {
            UnbindHealth();
            SetVisible(false);
            return;
        }

        if (best == _boundBoss)
            return;

        UnbindHealth();
        _boundBoss = best;
        _boundHealth = best.GetComponent<HealthComponent>();
        if (_boundHealth == null)
            return;

        _boundHealth.OnHealthChanged.AddListener(HandleHealthChanged);
        _boundHealth.OnDied.AddListener(HandleDied);

        if (_nameLabel != null)
            _nameLabel.text = ResolveBossName(best);

        HandleHealthChanged(_boundHealth.CurrentHealth, _boundHealth.MaxHealth);
        _displayNormalized = _targetNormalized;
        _trailNormalized = _targetNormalized;
        ApplyBarsImmediate();
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
        // Nunca desativar este GameObject: o boss spawna com delay e o Update
        // precisa continuar fazendo poll via TryBindBoss.
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.ignoreParentGroups = true;
        float alpha = visible ? 1f : 0f;
        if (!Mathf.Approximately(_canvasGroup.alpha, alpha))
            _canvasGroup.alpha = alpha;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    private static string ResolveBossName(BossEnemyMarker marker)
    {
        if (marker != null && !string.IsNullOrWhiteSpace(marker.DisplayName))
            return marker.DisplayName;

        return "Rei Rato";
    }

    private void BuildUi()
    {
        _root = GetComponent<RectTransform>();
        StretchCenterBar(_root, height: 72f, widthPad: 120f);

        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _canvasGroup.ignoreParentGroups = true;

        Image frame = EnsureImage(gameObject, new Color(0.08f, 0.06f, 0.08f, 0.82f));
        frame.raycastTarget = false;

        GameObject trailGo = CreateChild("Trail", transform);
        Stretch(trailGo.GetComponent<RectTransform>(), 10f, 18f, 10f, 18f);
        _trail = EnsureImage(trailGo, new Color(0.95f, 0.45f, 0.35f, 0.85f));
        _trail.type = Image.Type.Filled;
        _trail.fillMethod = Image.FillMethod.Horizontal;
        _trail.fillOrigin = (int)Image.OriginHorizontal.Left;
        _trail.raycastTarget = false;

        GameObject fillGo = CreateChild("Fill", transform);
        Stretch(fillGo.GetComponent<RectTransform>(), 10f, 18f, 10f, 18f);
        _fill = EnsureImage(fillGo, new Color(0.78f, 0.12f, 0.18f, 0.95f));
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fill.raycastTarget = false;

        GameObject nameGo = CreateChild("Name", transform);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.sizeDelta = new Vector2(-40f, 28f);
        nameRt.anchoredPosition = new Vector2(0f, 4f);
        _nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
        _nameLabel.text = defaultBossName;
        _nameLabel.fontSize = 22f;
        _nameLabel.alignment = TextAlignmentOptions.Center;
        _nameLabel.color = new Color(1f, 0.92f, 0.85f, 1f);
        _nameLabel.raycastTarget = false;
        GameplayUiFonts.Apply(_nameLabel);

        GameObject valueGo = CreateChild("Value", transform);
        RectTransform valueRt = valueGo.GetComponent<RectTransform>();
        Stretch(valueRt, 0f, 0f, 0f, 0f);
        _valueLabel = valueGo.AddComponent<TextMeshProUGUI>();
        _valueLabel.text = "";
        _valueLabel.fontSize = 16f;
        _valueLabel.alignment = TextAlignmentOptions.Center;
        _valueLabel.color = new Color(1f, 1f, 1f, 0.9f);
        _valueLabel.raycastTarget = false;
        GameplayUiFonts.Apply(_valueLabel);
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image EnsureImage(GameObject go, Color color)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(image);
        image.color = color;
        return image;
    }

    private static void StretchCenterBar(RectTransform rt, float height, float widthPad)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Max(320f, 900f - widthPad * 2f), height);
        rt.anchoredPosition = Vector2.zero;
    }

    private static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
