///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: ScriptableObject que armazena as estat�sticas dos inimigos.
// ---------------------------------------------------------------- */
using UnityEngine;

public enum TargetPriority { Player, Structure }

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Stats/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Geral")]
    public TargetPriority targetPriority = TargetPriority.Player;
    [Tooltip("Raio em que o inimigo detecta alvos. Fora do raio: random walk.")]
    public float targetDetectionRange = 20f;
    [Tooltip("Intervalo entre reavaliações de alvo (segundos). Reduz CPU e tráfego indireto.")]
    public float targetScanInterval = 0.25f;
    public float scale = 1f;

    [Header("Combate — Stun")]
    [Tooltip("Tempo parado após receber dano.")]
    public float hitStunDuration = 0.35f;

    [Header("Patrulha (sem alvo)")]
    [Tooltip("Raio para escolher destino aleatório no NavMesh.")]
    public float randomWalkRadius = 6f;
    [Tooltip("Intervalo entre novos destinos de patrulha.")]
    public float randomWalkInterval = 2.5f;

    [Header("Health")]
    public float maxHealth = 50f;
    [Tooltip("Segundos após a morte até o prefab ser removido da rede (despawn).")]
    public float deathDespawnDelay = 0.4f;

    [Header("Movimento")]
    public float moveSpeed = 3.5f;

    [Header("Ataque Corpo-a-Corpo")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;

    [Header("Ataque à Distância")]
    public float rangedAttackRange = 10f;
    public float rangedAttackCooldown = 3f;
    public EnemyProjectileStats projectileStats;

    [Header("Drop Settings")]
    public float dropChance = 0.5f;
    public int minCienceDrop = 10;
    public int maxCienceDrop = 20;
    public GameObject cienciaPrefab;
}