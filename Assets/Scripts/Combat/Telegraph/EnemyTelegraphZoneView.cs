using UnityEngine;

/// <summary>Aplica o shader de preenchimento e escala o sprite conforme forma/tamanho.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyTelegraphZoneView : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int BackgroundColorId = Shader.PropertyToID("_BackgroundColor");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int FillModeId = Shader.PropertyToID("_FillMode");
    private static readonly int FillOriginSideId = Shader.PropertyToID("_FillOriginSide");
    private static readonly int ConeInnerRatioId = Shader.PropertyToID("_ConeInnerRatio");
    private static readonly int ConeOuterRatioId = Shader.PropertyToID("_ConeOuterRatio");

    private static readonly Color DefaultBackgroundColor = new Color(1f, 0.92f, 0.22f, 0.55f);
    private static readonly Color DefaultFillColor = new Color(0.9f, 0.12f, 0.08f, 0.85f);
    private static readonly Color DefaultOutlineColor = new Color(0.95f, 0.15f, 0.1f, 1f);
    private const float DefaultOutlineWidth = 0.06f;
    private const int DefaultSortingOrder = 50;

    private SpriteRenderer _renderer;
    private Material _materialInstance;
    private TelegraphShapeType _activeShape = TelegraphShapeType.Circle;
    private float _coneInnerRadius;
    private float _coneOuterRadius;
    private float _coneLength;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void ApplyStyle(EnemyTelegraphVisualStyle style, TelegraphShapeType shape, TelegraphFillMode fillMode)
    {
        EnsureMaterial();

        _activeShape = shape;
        float shapeValue = shape switch
        {
            TelegraphShapeType.Circle => 0f,
            TelegraphShapeType.Rectangle => 1f,
            TelegraphShapeType.ConeFrustum => 2f,
            _ => 1f
        };
        _materialInstance.SetFloat(ShapeId, shapeValue);
        _materialInstance.SetFloat(FillModeId, (float)fillMode);

        if (style != null)
        {
            _materialInstance.SetColor(BackgroundColorId, style.backgroundColor);
            _materialInstance.SetColor(FillColorId, style.fillColor);
            _materialInstance.SetColor(OutlineColorId, style.outlineColor);
            _materialInstance.SetFloat(OutlineWidthId, style.outlineWidth);
            _renderer.sortingOrder = style.sortingOrder;
            return;
        }

        _materialInstance.SetColor(BackgroundColorId, DefaultBackgroundColor);
        _materialInstance.SetColor(FillColorId, DefaultFillColor);
        _materialInstance.SetColor(OutlineColorId, DefaultOutlineColor);
        _materialInstance.SetFloat(OutlineWidthId, DefaultOutlineWidth);
        _renderer.sortingOrder = DefaultSortingOrder;
    }

    public void SetFill(float normalized)
    {
        if (_materialInstance == null) return;
        _materialInstance.SetFloat(FillAmountId, Mathf.Clamp01(normalized));
    }

    public void SetWorldPose(Vector2 position, float rotationDegrees, TelegraphShapeType shape, Vector2 size)
    {
        SetWorldPose(position, rotationDegrees, shape, size, null);
    }

    /// <summary>Pose com parâmetros opcionais de tronco de cone.</summary>
    public void SetWorldPose(
        Vector2 position,
        float rotationDegrees,
        TelegraphShapeType shape,
        Vector2 size,
        TelegraphStrikeDefinition strike)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        transform.localScale = Vector3.one;

        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_renderer != null && _renderer.sprite == null)
            _renderer.sprite = CooperativeZoneSpriteFactory.GetUnitQuadSprite();

        if (shape == TelegraphShapeType.Circle)
        {
            float diameter = Mathf.Max(0.1f, size.x * 2f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
            return;
        }

        if (shape == TelegraphShapeType.ConeFrustum && strike != null)
        {
            TelegraphConeFrustumUtility.ResolveRadii(strike, out _coneInnerRadius, out _coneOuterRadius, out _coneLength);
            Vector2 aabb = TelegraphConeFrustumUtility.GetAabbSize(_coneInnerRadius, _coneOuterRadius, _coneLength);
            transform.localScale = new Vector3(aabb.x, aabb.y, 1f);
            ApplyConeShaderRatios(_coneInnerRadius, _coneOuterRadius);
            return;
        }

        transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 1f);
    }

    public void ConfigureFillOrigin(Vector2 worldOrigin)
    {
        EnsureMaterial();
        if (_materialInstance == null)
            return;

        Vector3 local = transform.InverseTransformPoint(worldOrigin);
        float side = local.y >= 0f ? 1f : 0f;
        _materialInstance.SetFloat(FillOriginSideId, side);
    }

    private void ApplyConeShaderRatios(float innerRadius, float outerRadius)
    {
        EnsureMaterial();
        if (_materialInstance == null)
            return;

        float maxR = Mathf.Max(innerRadius, outerRadius, 0.01f);
        float innerHalfUv = 0.5f * (innerRadius / maxR);
        float outerHalfUv = 0.5f * (outerRadius / maxR);
        _materialInstance.SetFloat(ConeInnerRatioId, Mathf.Clamp(innerHalfUv, 0.01f, 0.5f));
        _materialInstance.SetFloat(ConeOuterRatioId, Mathf.Clamp(outerHalfUv, 0.01f, 0.5f));
    }

    private void EnsureMaterial()
    {
        if (_materialInstance != null) return;

        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        Shader shader = ResolveTelegraphShader();
        _materialInstance = new Material(shader);
        _renderer.material = _materialInstance;
        _renderer.color = Color.white;
    }

    private static Shader ResolveTelegraphShader()
    {
        Material template = Resources.Load<Material>("TelegraphZoneMaterial");
        if (template != null && template.shader != null)
            return template.shader;

        Shader shader = Shader.Find("MidnightMeow/TelegraphFill");
        if (shader != null)
            return shader;

        return Shader.Find("Sprites/Default");
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_activeShape != TelegraphShapeType.ConeFrustum)
            return;

        TelegraphConeFrustumUtility.DrawGizmos(
            transform.position,
            transform.eulerAngles.z,
            _coneInnerRadius,
            _coneOuterRadius,
            _coneLength,
            new Color(1f, 0.4f, 0.1f, 0.9f));
    }
#endif
}
