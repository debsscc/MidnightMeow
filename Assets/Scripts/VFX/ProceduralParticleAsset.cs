using UnityEngine;

/// <summary>
/// Material + textura compartilhados para partículas procedurais (bolinha suave, sem prefab de arte).
/// Evita o quadrado magenta de "shader missing" quando um ParticleSystem é criado por código.
/// </summary>
public static class ProceduralParticleAsset
{
    private static Material _material;
    private static Texture2D _dotTexture;

    public static Material SoftDotMaterial
    {
        get
        {
            if (_material != null)
                return _material;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            _material = new Material(shader) { mainTexture = GetSoftDotTexture() };
            return _material;
        }
    }

    public static void Apply(ParticleSystemRenderer renderer)
    {
        if (renderer != null)
            renderer.sharedMaterial = SoftDotMaterial;
    }

    private static Texture2D GetSoftDotTexture()
    {
        if (_dotTexture != null)
            return _dotTexture;

        const int size = 64;
        _dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float maxDist = center;
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha *= alpha; // borda mais suave
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _dotTexture.SetPixels(pixels);
        _dotTexture.Apply();
        return _dotTexture;
    }
}
