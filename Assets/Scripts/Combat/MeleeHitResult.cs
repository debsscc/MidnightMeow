using UnityEngine;

/// <summary>Resultado local de um swing melee (feedback visual).</summary>
public readonly struct MeleeHitResult
{
    public int HitCount { get; }
    public Vector2[] HitPoints { get; }
    public GameObject[] Targets { get; }

    public MeleeHitResult(int hitCount, Vector2[] hitPoints, GameObject[] targets)
    {
        HitCount = hitCount;
        HitPoints = hitPoints ?? System.Array.Empty<Vector2>();
        Targets = targets ?? System.Array.Empty<GameObject>();
    }

    public static MeleeHitResult Miss => new MeleeHitResult(0, System.Array.Empty<Vector2>(), System.Array.Empty<GameObject>());
}
