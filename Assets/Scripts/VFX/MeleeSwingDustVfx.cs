// ----------------------------------------------------------------------------
// MADE BY: DEBS CARVALHO
// DATE: 13/07/2026
// DESCRIPTION: Poeira curta nos pés no início do swing melee.
// ----------------------------------------------------------------------------

using UnityEngine;


public static class MeleeSwingDustVfx
{
    private const int DustCount = 5;
    private const float Lifetime = 0.28f;

    public static void Play(Vector2 feetWorldPosition)
    {
        GameObject root = new GameObject("MeleeSwingDust");
        root.transform.position = new Vector3(feetWorldPosition.x, feetWorldPosition.y, 0f);

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = Lifetime;
        main.startLifetime = Lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.55f, 0.48f, 0.4f, 0.55f);
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.15f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)DustCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 0.52f, 0.42f), 0f),
                new GradientColorKey(new Color(0.45f, 0.4f, 0.35f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.55f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        Renderer renderer = ps.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;

        ps.Play();
        Object.Destroy(root, Lifetime + 0.2f);
    }
}
