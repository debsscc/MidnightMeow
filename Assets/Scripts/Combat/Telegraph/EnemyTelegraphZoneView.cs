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

    private SpriteRenderer _renderer;
    private Material _materialInstance;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void ApplyStyle(EnemyTelegraphVisualStyle style, TelegraphShapeType shape, TelegraphFillMode fillMode)
    {
        EnsureMaterial();
        if (style == null) return;

        _materialInstance.SetColor(BackgroundColorId, style.backgroundColor);
        _materialInstance.SetColor(FillColorId, style.fillColor);
        _materialInstance.SetColor(OutlineColorId, style.outlineColor);
        _materialInstance.SetFloat(OutlineWidthId, style.outlineWidth);
        _materialInstance.SetFloat(ShapeId, shape == TelegraphShapeType.Circle ? 0f : 1f);
        _materialInstance.SetFloat(FillModeId, (float)fillMode);
        _renderer.sortingOrder = style.sortingOrder;
    }

    public void SetFill(float normalized)
    {
        if (_materialInstance == null) return;
        _materialInstance.SetFloat(FillAmountId, Mathf.Clamp01(normalized));
    }

    public void SetWorldPose(Vector2 position, float rotationDegrees, TelegraphShapeType shape, Vector2 size)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        float diameter = shape == TelegraphShapeType.Circle ? size.x * 2f : 1f;
        if (shape == TelegraphShapeType.Circle)
            transform.localScale = new Vector3(diameter, diameter, 1f);
        else
            transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void EnsureMaterial()
    {
        if (_materialInstance != null) return;

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
}
