using UnityEngine;

/// <summary>
/// Parâmetros do ataque à distância (Cora e futuros ranged).
/// </summary>
[CreateAssetMenu(fileName = "RangedCombatStats", menuName = "MidnightMeow/Stats/Ranged Combat Stats")]
public class RangedCombatStats : ScriptableObject
{
    [Header("Cadência e Dano")]
    [Tooltip("Disparos por segundo.")]
    public float fireRate = 3f;

    [Tooltip("Multiplicador de dano do projétil.")]
    public float damageMultiplier = 1f;

    [Header("Alcance")]
    [Tooltip("Raio máximo da mira / origem do disparo em relação ao personagem.")]
    public float attackRange = 4f;

    [Header("Configurações Avançadas")]
    [Tooltip("Duração do clipe de ataque usada para sincronizar AttackSpeed no Animator.")]
    public float attackAnimClipLength = 0.333f;
}
