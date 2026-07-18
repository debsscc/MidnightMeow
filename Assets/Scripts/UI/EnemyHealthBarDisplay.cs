using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida world-space acima do inimigo. Usa sprites opcionais ou fallback procedural
/// dimensionado pelo <see cref="SpriteRenderer"/> do inimigo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public class EnemyHealthBarDisplay : MonoBehaviour
{
    [SerializeField] private bool buildIfMissing = true;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private float verticalOffset = 0.25f;
    [SerializeField] private float widthPadding = 0.12f;
    [SerializeField] private float barHeight = 0.18f;
    [SerializeField] private Color backgroundColor = new(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color fillColor = new(0.85f, 0.2f, 0.2f, 0.95f);
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private int sortingOrder = 200;

    private HealthComponent _health;
    private Transform _barRoot;
    private Image _fillImage;
    private float _barWidth;
    private bool _cinematicBossFallback;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();

        // Fase-3: preferir HUD de tela, mas manter world-space como fallback até o bind.
        if (BossPhaseUtility.UsesCinematicBossPresentation(gameObject))
        {
            _cinematicBossFallback = true;
        }
        else
        {
            ApplyBossOverrides();
        }

        if (buildIfMissing && _barRoot == null)
            BuildBar();

        if (_health != null)
        {
            _health.OnHealthChanged.AddListener(HandleHealthChanged);
            _health.OnDied.AddListener(HandleDied);
        }
    }

    private void ApplyBossOverrides()
    {
        BossEnemyMarker boss = GetComponent<BossEnemyMarker>();
        if (boss == null)
            return;

        hideWhenFull = false;
        barHeight *= boss.HealthBarHeightMultiplier;
        widthPadding *= boss.HealthBarWidthMultiplier;
        verticalOffset *= 1.15f;
    }

    private void OnDestroy()
    {
        if (_health == null)
            return;

        _health.OnHealthChanged.RemoveListener(HandleHealthChanged);
        _health.OnDied.RemoveListener(HandleDied);
    }

    private void Start()
    {
        if (_health != null)
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private void OnEnable()
    {
        if (_health != null)
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private void LateUpdate()
    {
        if (_cinematicBossFallback)
        {
            bool hudBound = BossHealthBarHud.IsBoundToBoss(gameObject);
            if (_barRoot != null && _barRoot.gameObject.activeSelf == hudBound)
                _barRoot.gameObject.SetActive(!hudBound);

            if (hudBound)
                return;
        }

        if (_barRoot == null)
            return;

        Vector3 worldUp = Vector3.up;
        if (Camera.main != null)
            worldUp = Camera.main.transform.up;

        _barRoot.rotation = Quaternion.identity;
        _barRoot.position = GetAnchorPosition() + worldUp * GetVerticalOffset();
    }

    private Vector3 GetAnchorPosition()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            return sprite.bounds.center;

        return transform.position;
    }

    private float GetVerticalOffset()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            return sprite.bounds.extents.y + verticalOffset;

        return verticalOffset;
    }

    /// <summary>Posição world-space atual (ou estimada) do centro da barra de vida.</summary>
    public Vector3 GetBarWorldPosition()
    {
        if (_barRoot != null)
            return _barRoot.position;

        Vector3 worldUp = Camera.main != null ? Camera.main.transform.up : Vector3.up;
        return GetAnchorPosition() + worldUp * GetVerticalOffset();
    }

    /// <summary>Altura visual da barra (world units).</summary>
    public float GetBarHeight() => Mathf.Max(0.05f, barHeight);

    private void HandleHealthChanged(float current, float max)
    {
        if (_fillImage == null)
            return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _fillImage.fillAmount = ratio;

        if (_barRoot != null)
            _barRoot.gameObject.SetActive(!hideWhenFull || ratio < 0.999f);
    }

    private void HandleDied()
    {
        if (_barRoot != null)
            _barRoot.gameObject.SetActive(false);
    }

    public void HideImmediately()
    {
        if (_barRoot != null)
            _barRoot.gameObject.SetActive(false);
    }

    private void BuildBar()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        _barWidth = 1f;
        if (sprite != null && sprite.sprite != null)
            _barWidth = Mathf.Max(0.65f, sprite.bounds.size.x + widthPadding);

        GameObject root = new GameObject("HealthBar", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _barRoot = root.transform;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder;

        RectTransform canvasRt = root.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(_barWidth, barHeight);
        canvasRt.localScale = Vector3.one;
        canvasRt.pivot = new Vector2(0.5f, 0.5f);

        GameObject background = CreateBarImage("Background", root.transform, backgroundSprite, backgroundColor);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fill = CreateBarImage("Fill", background.transform, fillSprite, fillColor);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        Stretch(fillRt);
        _fillImage = fill.GetComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static GameObject CreateBarImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        if (sprite != null)
            image.sprite = sprite;
        else
            LoadingProgressUtility.ApplySolidSprite(image);

        image.color = color;
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
