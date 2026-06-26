using UnityEngine;

/// <summary>
/// Partículas procedurais de brilho durante dissolve de sprite (sem prefab de arte).
/// </summary>
public static class DissolveSparkleVfx
{
    public static ParticleSystem Attach(Transform followTarget, Bounds emitBounds, float duration, Color tint)
    {
        if (followTarget == null || duration <= 0f)
            return null;

        GameObject root = new GameObject("DissolveSparkles");
        root.transform.SetParent(followTarget, worldPositionStays: false);
        root.transform.position = emitBounds.center;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = tint;
        main.maxParticles = 96;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.08f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.scale = new Vector3(
            Mathf.Max(0.15f, emitBounds.size.x),
            Mathf.Max(0.15f, emitBounds.size.y),
            0.05f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(tint, 0.45f),
                new GradientColorKey(tint * 0.6f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 100;
        ProceduralParticleAsset.Apply(renderer);

        ps.Play();
        Object.Destroy(root, duration + 0.75f);
        return ps;
    }
}
