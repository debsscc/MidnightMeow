using UnityEngine;

/// <summary>
/// Burst procedural de partículas no ponto de impacto melee (sem arte extra).
/// </summary>
public static class MeleeHitBurstVfx
{
    private const int BurstCount = 4;
    private const float Lifetime = 0.22f;
    private const float StartSpeed = 2.5f;
    private const float StartSize = 0.12f;

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
        main.startLifetime = Lifetime;
        main.startSpeed = StartSpeed;
        main.startSize = StartSize;
        main.startColor = new Color(1f, 0.95f, 0.75f, 1f);
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.35f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)BurstCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.35f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ps.Play();
        Object.Destroy(root, Lifetime + 0.15f);
    }
}
