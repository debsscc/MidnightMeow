///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// ATUALIZADO EM: 15-07-2026
// DESCRIÇÃO: ScriptableObject que armazena as estatísticas dos inimigos.
// ---------------------------------------------------------------- */
using UnityEngine;

/// <summary>Legado — preferir <see cref="AggroType"/>.</summary>
public enum TargetPriority { Player, Structure }

/// <summary>Comportamento de seleção de alvo (servidor).</summary>
public enum AggroType
{
    PlayersOnly = 0,
    StructuresOnly = 1,
    Dynamic = 2
}

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Stats/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Geral")]
    [Tooltip("Tipo de aggro. Dynamic usa as opções swap* abaixo.")]
    public AggroType aggroType = AggroType.PlayersOnly;

    [Tooltip("Legado: mapeado para AggroType em ResolveAggroType() se o asset antigo ainda depender disso.")]
    public TargetPriority targetPriority = TargetPriority.Player;

    [Header("Aggro Dynamic")]
    [Tooltip("Se true (Dynamic): trocar da estrutura para um jogador mais próximo no range.")]
    public bool swapToNearbyPlayer = true;

    [Tooltip("Se true (Dynamic): ao receber dano de um jogador, focar nele.")]
    public bool swapOnDamage = true;

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

    [Tooltip("Redução percentual (0–1) do dano de ataques Ranged.")]
    [Range(0f, 1f)]
    public float rangedDefense;
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

    /// <summary>
    /// Resolve o tipo de aggro efetivo. Assets legados com <see cref="targetPriority"/> = Structure
    /// e <see cref="aggroType"/> ainda no default (PlayersOnly) mapeiam para StructuresOnly.
    /// </summary>
    public AggroType ResolveAggroType()
    {
        // Campo novo: serializa como 0 em assets antigos. Só use o legado Structure
        // quando o autor nunca definiu AggroType explicitamente além do default.
        if (aggroType == AggroType.PlayersOnly && targetPriority == TargetPriority.Structure)
            return AggroType.StructuresOnly;

        return aggroType;
    }

    private void OnValidate()
    {
        if (targetDetectionRange < 0.5f)
            targetDetectionRange = 0.5f;
        if (targetScanInterval < 0.05f)
            targetScanInterval = 0.05f;
    }
}
