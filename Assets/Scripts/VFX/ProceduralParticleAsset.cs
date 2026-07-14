using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Material + texturas procedurais para partículas soft (círculo / chama), sem arte extra.
/// Evita o “quadrado opaco” clássico no URP quando o material não fica Transparent.
/// </summary>
public static class ProceduralParticleAsset
{
    private static Material _softDotMaterial;
    private static Material _softFlameMaterial;
    private static Material _additiveFlameMaterial;
    private static Texture2D _dotTexture;
    private static Texture2D _flameTexture;
    private static Texture2D _candleFlameTexture;
    private static Sprite _candleFlameSprite;
    private static Sprite _candleGlowSprite;
    private static Texture2D _glowTexture;

    public static Material SoftDotMaterial => _softDotMaterial ??= BuildMaterial(GetSoftDotTexture(), additive: false);

    public static Material SoftFlameMaterial => _softFlameMaterial ??= BuildMaterial(GetSoftFlameTexture(), additive: false);

    public static Material AdditiveFlameMaterial => _additiveFlameMaterial ??= BuildMaterial(GetSoftFlameTexture(), additive: true);

    /// <summary>Sprite teardrop de vela (núcleo claro → amarelo → laranja).</summary>
    public static Sprite CandleFlameSprite => _candleFlameSprite ??= BuildCandleFlameSprite();

    /// <summary>Halo soft atrás da chama.</summary>
    public static Sprite CandleGlowSprite => _candleGlowSprite ??= BuildCandleGlowSprite();


    public static void Apply(ParticleSystemRenderer renderer)
    {
        ApplySoftDot(renderer);
    }

    public static void ApplySoftDot(ParticleSystemRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.sharedMaterial = SoftDotMaterial;
        renderer.material = SoftDotMaterial; // instancia segura em runtime
    }

    public static void ApplySoftFlame(ParticleSystemRenderer renderer, bool additive = true)
    {
        if (renderer == null)
            return;

        Material mat = additive ? AdditiveFlameMaterial : SoftFlameMaterial;
        renderer.sharedMaterial = mat;
        renderer.material = mat;
    }

    private static Material BuildMaterial(Texture2D texture, bool additive)
    {
        Shader shader = ResolveParticleShader();
        Material material = new Material(shader)
        {
            name = additive ? "ProceduralAdditiveFlame" : "ProceduralSoftParticle",
            mainTexture = texture,
            hideFlags = HideFlags.HideAndDontSave
        };

        AssignTexture(material, texture);
        ConfigureTransparent(material, additive);
        return material;
    }

    private static Shader ResolveParticleShader()
    {
        // Sprites/Default em 2D/URP respeita alpha da textura soft sem setup extra.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            return shader;

        shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null)
            return shader;

        return Shader.Find("Unlit/Transparent");
    }

    private static void AssignTexture(Material material, Texture2D texture)
    {
        material.mainTexture = texture;

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", Color.black);
    }

    private static void ConfigureTransparent(Material material, bool additive)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f); // Transparent
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", additive ? 1f : 0f); // 1 = Additive, 0 = Alpha
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0f);

        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt(
            "_DstBlend",
            additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (additive)
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        else
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        material.renderQueue = (int)RenderQueue.Transparent;
        material.enableInstancing = false;
    }

    private static Texture2D GetSoftDotTexture()
    {
        if (_dotTexture != null)
            return _dotTexture;

        const int size = 128;
        _dotTexture = CreateSoftRadialTexture(size, size, horizontalFalloff: 1f, verticalFalloff: 1f, power: 2.2f);
        _dotTexture.name = "ProceduralSoftDot";
        return _dotTexture;
    }

    private static Texture2D GetSoftFlameTexture()
    {
        if (_flameTexture != null)
            return _flameTexture;

        // Elipse vertical — parece mais “chama” e menos bolinha/quadrado.
        const int width = 96;
        const int height = 160;
        _flameTexture = CreateSoftRadialTexture(width, height, horizontalFalloff: 1.35f, verticalFalloff: 0.85f, power: 1.7f);
        _flameTexture.name = "ProceduralSoftFlame";
        return _flameTexture;
    }

    private static Texture2D CreateSoftRadialTexture(
        int width,
        int height,
        float horizontalFalloff,
        float verticalFalloff,
        float power)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.55f; // pivô um pouco baixo = base da chama
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x - cx) / (cx * horizontalFalloff);
                float ny = (y - cy) / (cy * verticalFalloff);
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, power);

                // Afina o topo da chama.
                float tip = Mathf.InverseLerp(0f, height - 1, y);
                alpha *= Mathf.Lerp(1f, 0.35f, tip * tip);

                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Sprite BuildCandleFlameSprite()
    {
        _candleFlameTexture = CreateCandleFlameTexture(96, 160);
        // Pivot na base da chama (pavio).
        return Sprite.Create(
            _candleFlameTexture,
            new Rect(0f, 0f, _candleFlameTexture.width, _candleFlameTexture.height),
            new Vector2(0.5f, 0.08f),
            100f);
    }

    private static Sprite BuildCandleGlowSprite()
    {
        _glowTexture = CreateSoftRadialTexture(128, 128, 1f, 1f, 1.8f);
        _glowTexture.name = "ProceduralCandleGlow";
        return Sprite.Create(
            _glowTexture,
            new Rect(0f, 0f, _glowTexture.width, _glowTexture.height),
            new Vector2(0.5f, 0.35f),
            100f);
    }

    /// <summary>
    /// Gota clássica de vela: base larga, ponta fina, núcleo branco/amarelo, borda laranja.
    /// </summary>
    private static Texture2D CreateCandleFlameTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
            name = "ProceduralCandleFlame"
        };

        float cx = (width - 1) * 0.5f;
        var pixels = new Color[width * height];

        Color core = new Color(1f, 0.98f, 0.85f, 1f);
        Color mid = new Color(1f, 0.78f, 0.18f, 1f);
        Color edge = new Color(1f, 0.35f, 0.02f, 1f);

        for (int y = 0; y < height; y++)
        {
            // 0 = base, 1 = ponta
            float t = y / (float)(height - 1);

            // Largura da gota: larga embaixo, afina no topo (teardrop).
            float halfWidth = Mathf.Lerp(0.42f, 0.04f, Mathf.Pow(t, 0.72f)) * cx;
            halfWidth = Mathf.Max(1.2f, halfWidth);

            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float nx = dx / halfWidth;

                // Fora da gota.
                if (nx > 1.05f)
                {
                    pixels[y * width + x] = Color.clear;
                    continue;
                }

                // Soft edge
                float edgeFade = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Max(0f, nx), 1.55f));
                // Suaviza base e ponta
                float verticalFade = 1f;
                if (t < 0.06f)
                    verticalFade = Mathf.SmoothStep(0f, 1f, t / 0.06f);
                else if (t > 0.88f)
                    verticalFade = Mathf.SmoothStep(1f, 0f, (t - 0.88f) / 0.12f);

                float alpha = edgeFade * verticalFade;
                if (alpha <= 0.01f)
                {
                    pixels[y * width + x] = Color.clear;
                    continue;
                }

                // Núcleo mais claro no centro-baixo; borda mais laranja.
                float radial = Mathf.Clamp01(nx);
                float along = Mathf.Clamp01(t);
                Color tone = Color.Lerp(core, mid, Mathf.Clamp01(radial * 0.85f + along * 0.25f));
                tone = Color.Lerp(tone, edge, Mathf.Clamp01(radial * radial + along * 0.35f));

                // Tip mais laranja
                tone = Color.Lerp(tone, edge, Mathf.Clamp01((along - 0.55f) / 0.45f) * 0.65f);

                tone.a = Mathf.Clamp01(alpha);
                pixels[y * width + x] = tone;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }
}
