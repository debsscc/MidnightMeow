///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: ScriptableObject que armazena as estatísticas dos inimigos.
// ---------------------------------------------------------------- */
using UnityEngine;

public enum TargetPriority { Player, Structure }

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Stats/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Geral")]
    public TargetPriority targetPriority = TargetPriority.Player;
    public float targetDetectionRange = 20f;

    [Header("Health")]
    public float maxHealth = 50f;

    [Header("Movimento")]
    public float moveSpeed = 3.5f;

    [Header("Ataque Corpo-a-Corpo")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10; 
}