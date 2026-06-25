using UnityEngine;

/// <summary>
/// Sprites unitários (1 unidade de mundo = 1 diâmetro) para zonas cooperativas world-space.
/// </summary>
public static class CooperativeZoneSpriteFactory
{
    private static Sprite _unitQuadSprite;

    /// <summary>
    /// Quad 1×1 unidade de mundo (ppu = largura da textura). Use com <c>localScale = Vector3.one * diameter</c>.
    /// </summary>
    public static Sprite GetUnitQuadSprite()
    {
        if (_unitQuadSprite != null)
            return _unitQuadSprite;

        Texture2D tex = Texture2D.whiteTexture;
        _unitQuadSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            tex.width);
        return _unitQuadSprite;
    }
}
