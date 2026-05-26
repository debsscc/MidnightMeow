using System;
using UnityEngine;

/// <summary>Eventos globais de telegraphs inimigos (VFX, áudio, UI).</summary>
public static class EnemyTelegraphEvents
{
    public static event Action<TelegraphEventData> OnTelegraphStarted;
    public static event Action<TelegraphEventData> OnTelegraphFillComplete;
    public static event Action<TelegraphResolvedEventData> OnTelegraphResolved;

    public static void InvokeStarted(TelegraphEventData data) => OnTelegraphStarted?.Invoke(data);
    public static void InvokeFillComplete(TelegraphEventData data) => OnTelegraphFillComplete?.Invoke(data);
    public static void InvokeResolved(TelegraphResolvedEventData data) => OnTelegraphResolved?.Invoke(data);
}

public readonly struct TelegraphEventData
{
    public readonly GameObject Instigator;
    public readonly Vector2 WorldPosition;
    public readonly float RotationDegrees;
    public readonly TelegraphShapeType Shape;
    public readonly Vector2 Size;
    public readonly EnemyTelegraphResolution Resolution;

    public TelegraphEventData(
        GameObject instigator,
        Vector2 worldPosition,
        float rotationDegrees,
        TelegraphShapeType shape,
        Vector2 size,
        EnemyTelegraphResolution resolution)
    {
        Instigator = instigator;
        WorldPosition = worldPosition;
        RotationDegrees = rotationDegrees;
        Shape = shape;
        Size = size;
        Resolution = resolution;
    }
}

public readonly struct TelegraphResolvedEventData
{
    public readonly TelegraphEventData Telegraph;
    public readonly int TargetsHit;
    public readonly bool SpawnedProjectile;

    public TelegraphResolvedEventData(TelegraphEventData telegraph, int targetsHit, bool spawnedProjectile)
    {
        Telegraph = telegraph;
        TargetsHit = targetsHit;
        SpawnedProjectile = spawnedProjectile;
    }
}
