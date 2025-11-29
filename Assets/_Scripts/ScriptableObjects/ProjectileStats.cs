using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileStats", menuName = "Scriptable Objects/ProjectileStats")]
public class ProjectileStats : ScriptableObject
{
    [Header("Movimento")]
    public float moveSpeed = 15f;

    [Header("Combat")]
    public float damage = 20f;
}
