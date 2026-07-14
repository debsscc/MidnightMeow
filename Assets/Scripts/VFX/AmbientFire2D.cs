using UnityEngine;

/// <summary>
/// Chama de vela 2D (gota + glow + faíscas leves). Prefab: AmbientFire2D.
/// Visual alvo: teardrop com núcleo claro e borda laranja — não chuva de partículas.
/// </summary>
[DisallowMultipleComponent]
public sealed class AmbientFire2D : MonoBehaviour
{
    public enum FireSizePreset
    {
        Candle,
        Torch,
        Bonfire
    }

    private static readonly string[] LayerNames =
    {
        "Glow", "FlameOuter", "FlameCore", "Sparks"
    };

    [Header("Preset / escala")]
    [SerializeField] private FireSizePreset sizePreset = FireSizePreset.Candle;
    [SerializeField] [Range(0.25f, 3f)] private float sizeMultiplier = 1f;
    [SerializeField] [Range(0.2f, 2f)] private float intensity = 1f;
    [SerializeField] private bool playOnEnable = true;

    [Header("Cores")]
    [SerializeField] private Color glowColor = new Color(1f, 0.45f, 0.08f, 0.35f);
    [SerializeField] private Color outerTint = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color coreTint = Color.white;
    [SerializeField] private Color sparkColor = new Color(1f, 0.9f, 0.45f, 1f);

    [Header("Render / anim")]
    [SerializeField] private int sortingOrder = 40;
    [SerializeField] private bool flicker = true;
    [SerializeField] [Range(0.5f, 10f)] private float flickerSpeed = 5.5f;
    [SerializeField] private bool playSparks = true;

    private SpriteRenderer _glow;
    private SpriteRenderer _outer;
    private SpriteRenderer _core;
    private ParticleSystem _sparks;
    private Vector3 _outerBaseScale;
    private Vector3 _coreBaseScale;
    private Vector3 _glowBaseScale;
    private float _phase;
    private bool _playing;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        RebuildAndPlay();
    }

    private void Start()
    {
        if (playOnEnable)
            RebuildAndPlay();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        Stop();
    }

    private void Update()
    {
        if (!_playing || !flicker || !Application.isPlaying)
            return;

        _phase += Time.deltaTime * flickerSpeed;
        AnimateFlame(_phase);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sizeMultiplier = Mathf.Clamp(sizeMultiplier, 0.25f, 3f);
        intensity = Mathf.Clamp(intensity, 0.2f, 2f);
        sortingOrder = Mathf.Max(0, sortingOrder);
    }
#endif

    public void Play() => RebuildAndPlay();

    public void Stop()
    {
        _playing = false;
        if (_sparks != null)
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void RebuildAndPlay()
    {
        EnsureVisuals();
        ApplyScaleAndColors();
        _playing = true;
        _phase = Random.Range(0f, 10f);

        if (_sparks != null)
        {
            if (playSparks)
            {
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _sparks.Play(true);
            }
            else
            {
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void EnsureVisuals()
    {
        DestroyLegacyChildren();

        _glow = CreateSpriteLayer("Glow", ProceduralParticleAsset.CandleGlowSprite, sortingOrder - 1);
        _outer = CreateSpriteLayer("FlameOuter", ProceduralParticleAsset.CandleFlameSprite, sortingOrder);
        _core = CreateSpriteLayer("FlameCore", ProceduralParticleAsset.CandleFlameSprite, sortingOrder + 1);
        _sparks = CreateSparksLayer("Sparks", sortingOrder + 2);
    }

    private void DestroyLegacyChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
                continue;

            bool known = false;
            for (int n = 0; n < LayerNames.Length; n++)
            {
                if (child.name == LayerNames[n] ||
                    child.name == "Outer" || child.name == "Mid" || child.name == "Core")
                {
                    known = true;
                    break;
                }
            }

            if (!known)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        _glow = null;
        _outer = null;
        _core = null;
        _sparks = null;
    }

    private SpriteRenderer CreateSpriteLayer(string name, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = gameObject.layer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        // Material default do sprite (não forçar textura de partícula).
        return sr;
    }

    private ParticleSystem CreateSparksLayer(string name, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        go.layer = gameObject.layer;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = order;
        ProceduralParticleAsset.ApplySoftDot(renderer);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        main.startColor = sparkColor;
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        var emission = ps.emission;
        emission.rateOverTime = 3.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.04f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(sparkColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = g;

        return ps;
    }

    private void ApplyScaleAndColors()
    {
        float s = ResolveBaseScale() * sizeMultiplier;
        float i = intensity;

        _glowBaseScale = new Vector3(s * 1.35f, s * 1.1f, 1f);
        _outerBaseScale = new Vector3(s * 1.05f, s * 1.15f, 1f);
        _coreBaseScale = new Vector3(s * 0.58f, s * 0.72f, 1f);

        if (_glow != null)
        {
            _glow.transform.localPosition = new Vector3(0f, 0.08f * s, 0f);
            _glow.transform.localScale = _glowBaseScale;
            Color g = glowColor;
            g.a = Mathf.Clamp01(glowColor.a * i);
            _glow.color = g;
        }

        if (_outer != null)
        {
            _outer.transform.localPosition = Vector3.zero;
            _outer.transform.localScale = _outerBaseScale;
            Color c = outerTint;
            c.a = Mathf.Clamp01(0.95f * i);
            _outer.color = c;
        }

        if (_core != null)
        {
            _core.transform.localPosition = new Vector3(0f, 0.02f * s, 0f);
            _core.transform.localScale = _coreBaseScale;
            Color c = coreTint;
            c.a = Mathf.Clamp01(i);
            _core.color = c;
        }

        if (_sparks != null)
        {
            var emission = _sparks.emission;
            emission.rateOverTime = playSparks ? 2.5f * i : 0f;
            var main = _sparks.main;
            main.startColor = sparkColor;
        }
    }

    private void AnimateFlame(float phase)
    {
        // Balanço + squash/stretch tipo vela.
        float sway = Mathf.Sin(phase * 1.3f) * 4.5f + Mathf.Sin(phase * 2.7f + 1.1f) * 2.2f;
        float stretch = 1f + Mathf.Sin(phase * 2.1f) * 0.07f + Mathf.Sin(phase * 5.3f) * 0.03f;
        float width = 1f + Mathf.Sin(phase * 1.7f + 0.4f) * 0.06f;
        float pulse = 0.92f + 0.08f * Mathf.Sin(phase * 3.4f);

        if (_outer != null)
        {
            _outer.transform.localRotation = Quaternion.Euler(0f, 0f, sway);
            _outer.transform.localScale = new Vector3(
                _outerBaseScale.x * width,
                _outerBaseScale.y * stretch,
                1f);
            Color c = _outer.color;
            c.a = Mathf.Clamp01(outerTint.a * intensity * pulse);
            _outer.color = c;
        }

        if (_core != null)
        {
            _core.transform.localRotation = Quaternion.Euler(0f, 0f, sway * 0.65f);
            _core.transform.localScale = new Vector3(
                _coreBaseScale.x * (2f - width),
                _coreBaseScale.y * (stretch + 0.04f),
                1f);
        }

        if (_glow != null)
        {
            float glowPulse = 0.85f + 0.15f * Mathf.Sin(phase * 2.4f + 0.7f);
            _glow.transform.localScale = _glowBaseScale * glowPulse;
            Color g = glowColor;
            g.a = Mathf.Clamp01(glowColor.a * intensity * glowPulse);
            _glow.color = g;
        }
    }

    private float ResolveBaseScale()
    {
        switch (sizePreset)
        {
            case FireSizePreset.Torch:
                return 1.35f;
            case FireSizePreset.Bonfire:
                return 2.2f;
            default:
                return 0.55f; // Candle — cabe no pavio das velas da Fase-3
        }
    }
}
