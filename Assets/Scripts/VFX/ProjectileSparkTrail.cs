using UnityEngine;

/// <summary>
/// Lightweight pink spark trail for player projectiles (Cora fireball).
/// Built at runtime so the Combat Projectile prefab stays simple to tune.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectileSparkTrail : MonoBehaviour
{
    [SerializeField] private Color _coreColor = new Color(1f, 0.35f, 0.85f, 1f);
    [SerializeField] private Color _edgeColor = new Color(1f, 0.7f, 0.95f, 0.55f);
    [SerializeField] private float _emissionRate = 28f;
    [SerializeField] private float _startSize = 0.12f;
    [SerializeField] private float _lifetime = 0.28f;
    [SerializeField] private float _speed = 0.55f;

    private ParticleSystem _ps;
    private bool _stopped;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        if (_ps == null)
            _ps = gameObject.AddComponent<ParticleSystem>();

        Configure(_ps);
        _ps.Play(true);
    }

    public void StopTrail()
    {
        if (_stopped || _ps == null) return;
        _stopped = true;

        var emission = _ps.emission;
        emission.rateOverTime = 0f;
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Configure(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = _lifetime;
        main.startSpeed = _speed;
        main.startSize = _startSize;
        main.startColor = _coreColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.15f;
        main.maxParticles = 64;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = _emissionRate;
        emission.rateOverDistance = 6f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(_coreColor, 0f),
                new GradientColorKey(_edgeColor, 0.45f),
                new GradientColorKey(new Color(0.6f, 0.15f, 0.7f, 0.2f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.55f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 6;
        if (renderer.sharedMaterial == null)
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }
}
