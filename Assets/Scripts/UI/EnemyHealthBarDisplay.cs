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
    [SerializeField] private float verticalOffset = 0.15f;
    [SerializeField] private float widthPadding = 0.08f;
    [SerializeField] private float barHeight = 0.08f;
    [SerializeField] private Color backgroundColor = new(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color fillColor = new(0.85f, 0.2f, 0.2f, 0.95f);
    [SerializeField] private bool hideWhenFull = true;

    private HealthComponent _health;
    private Transform _barRoot;
    private Image _fillImage;
    private float _barWidth;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        if (buildIfMissing && _barRoot == null)
            BuildBar();

        _health.OnHealthChanged.AddListener(HandleHealthChanged);
        _health.OnDied.AddListener(HandleDied);
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
        HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private void LateUpdate()
    {
        if (_barRoot == null)
            return;

        Vector3 worldUp = Vector3.up;
        if (Camera.main != null)
            worldUp = Camera.main.transform.up;

        _barRoot.rotation = Quaternion.identity;
        _barRoot.position = GetAnchorPosition() + worldUp * verticalOffset;
    }

    private Vector3 GetAnchorPosition()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            return sprite.bounds.center;

        return transform.position;
    }

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

    private void BuildBar()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        _barWidth = 0.8f;
        if (sprite != null && sprite.sprite != null)
            _barWidth = Mathf.Max(0.4f, sprite.bounds.size.x + widthPadding);

        GameObject root = new GameObject("HealthBar", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _barRoot = root.transform;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRt = root.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(_barWidth, barHeight);
        canvasRt.localScale = Vector3.one * 0.01f;

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
