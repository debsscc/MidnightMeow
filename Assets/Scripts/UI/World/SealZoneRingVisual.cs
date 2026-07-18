///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Anel circular world-space via shader AbilityZoneFill (anti-aliased).
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Anel circular world-space. Usa o shader <c>MidnightMeow/AbilityZoneFill</c>
/// (círculo matemático — sem textura procedural pixelada).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SealZoneRingVisual : MonoBehaviour
{
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int PulseStrengthId = Shader.PropertyToID("_PulseStrength");

    private SpriteRenderer _background;
    private SpriteRenderer _fill;
    private Material _backgroundMaterial;
    private Material _fillMaterial;

    private void Awake()
    {
        _background = GetComponent<SpriteRenderer>();
        EnsureRenderers();
    }

    private void OnDestroy()
    {
        DestroyMaterial(ref _backgroundMaterial);
        DestroyMaterial(ref _fillMaterial);
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
        EnsureRenderers();

        float size = Mathf.Max(0.5f, diameter);
        transform.localScale = new Vector3(size, size, 1f);

        Color fillColor = showInteriorFill
            ? background
            : new Color(background.r, background.g, background.b, 0f);

        ApplyZoneMaterial(
            _background,
            _backgroundMaterial,
            fillColor,
            outline,
            Mathf.Clamp(outlineThickness, 0.02f, 0.15f),
            sortingOrder);

        _background.enabled = true;

        _fill.sortingOrder = sortingOrder + 2;
        ApplyZoneMaterial(
            _fill,
            _fillMaterial,
            fill,
            new Color(outline.r, outline.g, outline.b, 0f),
            0.02f,
            sortingOrder + 2);

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

    private void EnsureRenderers()
    {
        if (_background == null)
            _background = GetComponent<SpriteRenderer>();

        Sprite unit = CooperativeZoneSpriteFactory.GetUnitQuadSprite();
        _background.sprite = unit;
        _background.sharedMaterial = null;

        // Remove hierarquia legada do gerador procedural (Outline sprite).
        Transform legacyOutline = transform.Find("Outline");
        if (legacyOutline != null)
            DestroyActive(legacyOutline.gameObject);

        if (_backgroundMaterial == null)
            _backgroundMaterial = CombatVisualMaterials.CreateAbilityZoneFillInstance();
        _background.material = _backgroundMaterial;

        if (_fill == null)
        {
            Transform existing = transform.Find("Fill");
            if (existing != null)
                _fill = existing.GetComponent<SpriteRenderer>();

            if (_fill == null)
            {
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(transform, false);
                _fill = fillGo.AddComponent<SpriteRenderer>();
            }
        }

        _fill.sprite = unit;
        if (_fillMaterial == null)
            _fillMaterial = CombatVisualMaterials.CreateAbilityZoneFillInstance();
        _fill.material = _fillMaterial;
        _fill.enabled = false;
    }

    private static void ApplyZoneMaterial(
        SpriteRenderer renderer,
        Material material,
        Color fill,
        Color outline,
        float outlineWidth,
        int sortingOrder)
    {
        if (renderer == null || material == null)
            return;

        material.SetColor(FillColorId, fill);
        material.SetColor(OutlineColorId, outline);
        material.SetFloat(OutlineWidthId, outlineWidth);
        material.SetFloat(ShapeId, 0f);
        material.SetFloat(AlphaId, 1f);
        material.SetFloat(PulseStrengthId, 0f);
        renderer.sortingOrder = sortingOrder;
    }

    private static void DestroyMaterial(ref Material material)
    {
        if (material == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(material);
        else
            Object.DestroyImmediate(material);

        material = null;
    }

    private static void DestroyActive(GameObject go)
    {
        if (go == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(go);
        else
            Object.DestroyImmediate(go);
    }
}
