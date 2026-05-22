using UnityEngine;

[CreateAssetMenu(fileName = "MeleeCombatStats", menuName = "Scriptable Objects/Melee Combat Stats")]
public class MeleeCombatStats : ScriptableObject
{
    [Header("Ataque")]
    public float damage = 2f;
    public float attackCooldown = 0.45f;
    public float attackRange = 1.8f;
    public float windupDelay = 0.08f;

    [Header("Trapézio (base perto do jogador → base longe)")]
    [Tooltip("Metade da largura na origem do ataque (perto do jogador).")]
    public float nearHalfWidth = 0.35f;

    [Tooltip("Metade da largura no fim do alcance (longe do jogador).")]
    public float farHalfWidth = 1.1f;

    [Header("Knockback (força do atacante)")]
    public float knockbackForce = 18f;
    public float knockbackDuration = 0.25f;
    public float knockbackDistance = 0.65f;

    [Header("Debug")]
    public bool drawDebugGizmos = true;
}
