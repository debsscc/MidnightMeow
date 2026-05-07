///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Componente que encontra e armazena o alvo atual do inimigo com base na prioridade definida nas estat�sticas.
// ---------------------------------------------------------------- */
using UnityEngine;

public class EnemyTargetFinder : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private Transform _currentTarget;
    public Transform CurrentTarget => _currentTarget;

    private void Start()
    {
        FindTarget();
    }

    /// <summary>
    /// Encontra o alvo mais próximo com a tag correta (Player ou Structure).
    /// No multiplayer com vários jogadores, sempre retorna o mais próximo.
    /// Em single-player com um jogador, o comportamento é idêntico ao original.
    /// </summary>
    public void FindTarget()
    {
        string targetTag = (stats.targetPriority == TargetPriority.Player) ? "Player" : "Structure";
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);

        if (targets.Length == 0)
        {
            _currentTarget = null;
            return;
        }

        // Retorna o alvo mais próximo entre todos os disponíveis
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var t in targets)
        {
            float dist = Vector2.Distance(transform.position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t;
            }
        }

        _currentTarget = nearest != null ? nearest.transform : null;
    }

    private void Update()
    {
        // Reavalia o alvo mais próximo a cada frame para manter perseguição dinâmica
        // entre múltiplos jogadores no multiplayer. Custo baixo com poucos jogadores.
        if (stats.targetPriority == TargetPriority.Player)
            FindTarget();
    }
}