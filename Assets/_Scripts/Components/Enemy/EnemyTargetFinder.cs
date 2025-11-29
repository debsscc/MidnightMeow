///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que encontra e armazena o alvo atual do inimigo com base na prioridade definida nas estatísticas.
// ---------------------------------------------------------------- */
using UnityEngine;

public class EnemyTargetFinder : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private Transform _currentTarget;
    public Transform CurrentTarget => _currentTarget; // Getter público

    private void Start()
    {
        string targetTag = (stats.targetPriority == TargetPriority.Player) ? "Player" : "Structure";

        GameObject target = GameObject.FindGameObjectWithTag(targetTag);
        if (target != null)
        {
            _currentTarget = target.transform;
        }
        else
        {
            // Lidar com o caso onde o alvo não é encontrado
        }
    }
}