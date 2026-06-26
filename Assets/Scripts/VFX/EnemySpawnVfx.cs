using UnityEngine;

/// <summary>
/// Baforada de poeira procedural no ponto de spawn do inimigo (sem prefab de arte).
/// Espelha o estilo de <see cref="DissolveSparkleVfx"/>, mas com leitura de "saiu do buraco".
/// </summary>
public static class EnemySpawnVfx
{
    public static ParticleSystem Play(
        Vector3 worldPosition,
        float radius,
        Color tint,
        float duration = 0.6f,
        int sortingLayerId = 0,
        int sortingOrder = 100)
    {
        if (duration <= 0f)
            return null;

        GameObject root = new GameObject("EnemySpawnPuff");
        root.transform.position = worldPosition;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.0f);
        main.startColor = tint;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24, (short)34) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.1f, radius);
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Os três eixos precisam estar no mesmo modo (RandomBetweenTwoConstants).
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(tint, 0f),
                new GradientColorKey(tint * 0.7f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.8f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
        ProceduralParticleAsset.Apply(renderer);

        ps.Play();
        Object.Destroy(root, duration + 0.75f);
        return ps;
    }
}
