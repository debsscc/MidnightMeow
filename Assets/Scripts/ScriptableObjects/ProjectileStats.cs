using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileStats", menuName = "Scriptable Objects/ProjectileStats")]
public class ProjectileStats : ScriptableObject
{
    [Header("Movimento")]
    public float moveSpeed = 15f;
    public int maxBounces = 1;
    public bool infinityBounces = false;
    public bool collectable = true;

    [Header("Combat")]
    public float damage = 20f;

    [Header("Distância Máxima")]
    [Tooltip("Distância máxima percorrida antes de se destruir. 0 = sem limite.")]
    public float maxDistance = 0f;
}
