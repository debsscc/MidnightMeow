using UnityEngine;

/// <summary>
/// Anel circular world-space com sprites procedurais (sem shader Telegraph).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SealZoneRingVisual : MonoBehaviour
{
    private static Sprite _circleSprite;
    private static Sprite _outlineSprite;
    private static float _cachedOutlineThickness = -1f;

    private SpriteRenderer _background;
    private SpriteRenderer _outline;
    private SpriteRenderer _fill;

    private void Awake()
    {
        _background = GetComponent<SpriteRenderer>();
        EnsureChildRenderers();
    }

    public void Configure(Color background, Color fill, int sortingOrder, float diameter)
    {
        Configure(background, fill, Color.white, sortingOrder, diameter, 0.055f, false);
    }

    public void Configure(Color background, Color fill, Color outline, int sortingOrder, float diameter)
    {
        Configure(background, fill, outline, sortingOrder, diameter, 0.055f, false);
    }

    public void Configure(
        Color background,
        Color fill,
        Color outline,
        int sortingOrder,
        float diameter,
        float outlineThickness,
        bool showInteriorFill)
    {
        EnsureChildRenderers();
        EnsureOutlineSprite(Mathf.Clamp(outlineThickness, 0.02f, 0.15f));

        _background.color = background;
        _fill.color = fill;
        _outline.color = outline;

        _background.sortingOrder = sortingOrder;
        _outline.sortingOrder = sortingOrder + 1;
        _fill.sortingOrder = sortingOrder + 2;

        _background.enabled = showInteriorFill;
        _outline.enabled = true;

        float size = Mathf.Max(0.5f, diameter);
        transform.localScale = new Vector3(size, size, 1f);
        SetFill(0f);
    }

    public void SetFill(float normalized)
    {
        if (_fill == null)
            return;

        float clamped = Mathf.Clamp01(normalized);
        _fill.transform.localScale = new Vector3(clamped, clamped, 1f);
        _fill.enabled = clamped > 0.01f;
    }

    private void EnsureChildRenderers()
    {
        EnsureSprites();

        _background.sprite = _circleSprite;

        if (_outline == null)
        {
            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(transform, false);
            _outline = outlineGo.AddComponent<SpriteRenderer>();
        }

        _outline.sprite = _outlineSprite;

        if (_fill == null)
        {
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(transform, false);
            _fill = fillGo.AddComponent<SpriteRenderer>();
        }

        _fill.sprite = _circleSprite;
    }

    private static void EnsureSprites()
    {
        if (_circleSprite == null)
            _circleSprite = CreateFilledCircleSprite(64, 0.98f);
    }

    private static void EnsureOutlineSprite(float thickness)
    {
        if (_outlineSprite != null && Mathf.Approximately(_cachedOutlineThickness, thickness))
            return;

        const float outer = 0.98f;
        float inner = Mathf.Clamp(outer - thickness, 0.82f, outer - 0.02f);
        _outlineSprite = CreateRingSprite(64, inner, outer);
        _cachedOutlineThickness = thickness;
    }

    private static Sprite CreateFilledCircleSprite(int size, float radiusNormalized)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float radius = center * radiusNormalized;
        float radiusSq = radius * radius;
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSq = dx * dx + dy * dy;
                pixels[y * size + x] = distSq <= radiusSq ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateRingSprite(int size, float innerRadiusNormalized, float outerRadiusNormalized)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float innerSq = Mathf.Pow(center * innerRadiusNormalized, 2f);
        float outerSq = Mathf.Pow(center * outerRadiusNormalized, 2f);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSq = dx * dx + dy * dy;
                pixels[y * size + x] = distSq >= innerSq && distSq <= outerSq ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
