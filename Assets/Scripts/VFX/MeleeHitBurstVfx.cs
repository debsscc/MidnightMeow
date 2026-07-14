using UnityEngine;

/// <summary>
/// Burst procedural de faíscas de impacto melee (aço / branco-ciano).
/// </summary>
public static class MeleeHitBurstVfx
{
    private const int BurstCount = 7;
    private const float Lifetime = 0.2f;
    private const float StartSpeed = 3.2f;
    private const float StartSize = 0.1f;

    public static void Play(Vector2 worldPosition)
    {
        GameObject root = new GameObject("MeleeHitBurst");
        root.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = Lifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, Lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(StartSpeed * 0.55f, StartSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(StartSize * 0.5f, StartSize);
        main.startColor = new Color(0.85f, 0.95f, 1f, 1f);
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.55f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)BurstCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.55f, 0.85f, 1f), 0.45f),
                new GradientColorKey(new Color(1f, 0.9f, 0.55f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        Renderer renderer = ps.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sortingOrder = 55;

        ps.Play();
        Object.Destroy(root, Lifetime + 0.15f);
    }
}
