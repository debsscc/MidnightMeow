using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida world-space para jogadores aliados.
/// Não altera a barra de inimigos.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkPlayerHealth))]
public class AllyPlayerHealthBarDisplay : NetworkBehaviour
{
    [Header("Build")]
    [SerializeField] private bool buildIfMissing = true;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;

    [Header("Layout")]
    [SerializeField] private float verticalOffset = 0.32f;
    [SerializeField] private float widthPadding = 0.12f;
    [SerializeField] private float barHeight = 0.26f; // mais grossa que inimigo (0.18)
    [SerializeField] private int sortingOrder = 220;

    [Header("Style (Ally)")]
    [SerializeField] private Color backgroundColor = new(0.10f, 0.12f, 0.14f, 0.9f);
    [SerializeField] private Color fillColor = new(0.22f, 0.86f, 0.35f, 0.97f); // verde

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = false;

    private NetworkPlayerHealth _networkHealth;
    private Transform _barRoot;
    private Image _fillImage;

    private void Awake()
    {
        _networkHealth = GetComponent<NetworkPlayerHealth>();

        if (buildIfMissing && _barRoot == null)
            BuildBar();
    }

    public override void OnNetworkSpawn()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged += HandleNetworkHealthChanged;
        NetworkPlayerHealth.OnNetworkPlayerDowned += HandleAllyDowned;
        NetworkPlayerHealth.OnNetworkPlayerRevived += HandleAllyRevived;

        // Regra principal: nunca mostrar barra flutuante para o dono local
        if (IsLocalPlayer)
        {
            SetBarVisible(false);
            return;
        }

        RefreshFromNetworkHealth();
    }

    public override void OnNetworkDespawn()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged -= HandleNetworkHealthChanged;
        NetworkPlayerHealth.OnNetworkPlayerDowned -= HandleAllyDowned;
        NetworkPlayerHealth.OnNetworkPlayerRevived -= HandleAllyRevived;
    }

    private void LateUpdate()
    {
        if (_barRoot == null || IsLocalPlayer)
            return;

        Vector3 worldUp = Camera.main != null ? Camera.main.transform.up : Vector3.up;
        _barRoot.rotation = Quaternion.identity;
        _barRoot.position = GetAnchorPosition() + worldUp * GetVerticalOffset();
    }

    private void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (_networkHealth == null || clientId != _networkHealth.OwnerClientId)
            return;

        if (IsLocalPlayer)
        {
            SetBarVisible(false);
            return;
        }

        RefreshFromNetworkHealth(current, max);
    }

    private void HandleAllyDowned(ulong clientId)
    {
        if (_networkHealth == null || clientId != _networkHealth.OwnerClientId || IsLocalPlayer)
            return;

        SetBarVisible(false);
    }

    private void HandleAllyRevived(ulong clientId)
    {
        if (_networkHealth == null || clientId != _networkHealth.OwnerClientId || IsLocalPlayer)
            return;

        RefreshFromNetworkHealth();
    }

    private void RefreshFromNetworkHealth(float? current = null, float? max = null)
    {
        if (_networkHealth == null || IsLocalPlayer)
            return;

        if (!_networkHealth.CanFight)
        {
            SetBarVisible(false);
            return;
        }

        ApplyHealth(
            current ?? _networkHealth.CurrentHealth,
            max ?? _networkHealth.MaxHealth);
    }

    private void ApplyHealth(float current, float max)
    {
        if (_fillImage == null)
            return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _fillImage.fillAmount = ratio;

        SetBarVisible(!hideWhenFull || ratio < 0.999f);
    }

    public void InitializeAsAlly(Color? allyFill = null, float? allyBarHeight = null)
    {
        if (allyFill.HasValue)
            fillColor = allyFill.Value;

        if (allyBarHeight.HasValue)
            barHeight = Mathf.Max(0.08f, allyBarHeight.Value);

        if (_fillImage != null)
            _fillImage.color = fillColor;

        if (_barRoot != null && _barRoot.TryGetComponent<RectTransform>(out var rt))
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, barHeight);
    }

    private void BuildBar()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        float barWidth = 1f;
        if (sprite != null && sprite.sprite != null)
            barWidth = Mathf.Max(0.65f, sprite.bounds.size.x + widthPadding);

        GameObject root = new("AllyHealthBar", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _barRoot = root.transform;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder;

        RectTransform canvasRt = root.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRt.localScale = Vector3.one;
        canvasRt.pivot = new Vector2(0.5f, 0.5f);

        GameObject bg = CreateBarImage("Background", root.transform, backgroundSprite, backgroundColor);
        Stretch(bg.GetComponent<RectTransform>());

        GameObject fill = CreateBarImage("Fill", bg.transform, fillSprite, fillColor);
        Stretch(fill.GetComponent<RectTransform>());

        _fillImage = fill.GetComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void SetBarVisible(bool visible)
    {
        if (_barRoot != null)
            _barRoot.gameObject.SetActive(visible);
    }

    private Vector3 GetAnchorPosition()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        return (sprite != null && sprite.sprite != null) ? sprite.bounds.center : transform.position;
    }

    private float GetVerticalOffset()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        return (sprite != null && sprite.sprite != null)
            ? sprite.bounds.extents.y + verticalOffset
            : verticalOffset;
    }

    private static GameObject CreateBarImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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