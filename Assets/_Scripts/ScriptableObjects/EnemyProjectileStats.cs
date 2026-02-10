///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: ScriptableObject que armazena as estatísticas dos projéteis inimigos.
// ---------------------------------------------------------------- */

using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyProjectileStats", menuName = "Stats/Enemy Projectile Stats")]
public class EnemyProjectileStats : ScriptableObject
{
    [Header("Movimento")]
    public float moveSpeed = 10f;

    [Header("Combat")]
    public float damage = 15f;

    [Header("Lifetime")]
    public float lifetime = 5f;
}
