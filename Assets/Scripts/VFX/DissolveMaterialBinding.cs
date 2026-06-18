using UnityEngine;

/// <summary>
/// Mapeia propriedades de materiais de dissolve suportados (DissolveSprite e VOiD1 2D URP).
/// </summary>
internal readonly struct DissolveMaterialBinding
{
    public enum Kind
    {
        Unsupported,
        DissolveSprite,
        Void1Sprite2D
    }

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");
    private static readonly int SparkleIntensityId = Shader.PropertyToID("_SparkleIntensity");

    private static readonly int Void1FadeId = Shader.PropertyToID("Vector1_51DDBE76");
    private static readonly int Void1EdgeColorId = Shader.PropertyToID("Color_AE581CF8");

    public Kind Driver { get; }
    public int AmountPropertyId { get; }
    public float AmountAtStart { get; }
    public float AmountAtEnd { get; }
    public int EdgeColorPropertyId { get; }

    private DissolveMaterialBinding(
        Kind driver,
        int amountPropertyId,
        float amountAtStart,
        float amountAtEnd,
        int edgeColorPropertyId)
    {
        Driver = driver;
        AmountPropertyId = amountPropertyId;
        AmountAtStart = amountAtStart;
        AmountAtEnd = amountAtEnd;
        EdgeColorPropertyId = edgeColorPropertyId;
    }

    public static DissolveMaterialBinding FromMaterial(Material template)
    {
        if (template == null)
            return new DissolveMaterialBinding(Kind.Unsupported, 0, 0f, 0f, -1);

        if (template.HasProperty(Void1FadeId))
        {
            return new DissolveMaterialBinding(
                Kind.Void1Sprite2D,
                Void1FadeId,
                0f,
                50f,
                template.HasProperty(Void1EdgeColorId) ? Void1EdgeColorId : -1);
        }

        if (template.HasProperty(DissolveAmountId))
        {
            return new DissolveMaterialBinding(
                Kind.DissolveSprite,
                DissolveAmountId,
                0f,
                1f,
                template.HasProperty(EdgeColorId) ? EdgeColorId : -1);
        }

        return new DissolveMaterialBinding(Kind.Unsupported, 0, 0f, 0f, -1);
    }

    public void ApplyInitial(Material instance, Color edgeColor, float edgeIntensity, float sparkleIntensity)
    {
        if (instance == null || Driver == Kind.Unsupported)
            return;

        SetAmount(instance, 0f);
        ApplyEdgeColor(instance, edgeColor, edgeIntensity);

        if (Driver == Kind.DissolveSprite)
        {
            if (instance.HasProperty(EdgeIntensityId))
                instance.SetFloat(EdgeIntensityId, edgeIntensity);

            if (instance.HasProperty(SparkleIntensityId))
                instance.SetFloat(SparkleIntensityId, sparkleIntensity);
        }
    }

    public void SetAmount(Material instance, float normalizedProgress)
    {
        if (instance == null || Driver == Kind.Unsupported)
            return;

        float t = Mathf.Clamp01(normalizedProgress);
        float value = Mathf.Lerp(AmountAtStart, AmountAtEnd, t);
        instance.SetFloat(AmountPropertyId, value);
    }

    private void ApplyEdgeColor(Material instance, Color edgeColor, float edgeIntensity)
    {
        if (EdgeColorPropertyId < 0)
            return;

        if (Driver == Kind.Void1Sprite2D)
        {
            Color hdr = edgeColor * Mathf.Max(1f, edgeIntensity);
            instance.SetColor(EdgeColorPropertyId, hdr);
            return;
        }

        instance.SetColor(EdgeColorPropertyId, edgeColor);
    }
}
