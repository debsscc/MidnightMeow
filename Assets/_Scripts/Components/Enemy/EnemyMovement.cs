///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o movimento do inimigo em direção ao seu alvo usando NavMeshAgent.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.AI; 

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyTargetFinder))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private NavMeshAgent _agent;
    private EnemyTargetFinder _targetFinder;

    public event System.Action OnDestinationReached;
    public event System.Action OnDestinationLost;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _targetFinder = GetComponent<EnemyTargetFinder>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = stats.moveSpeed;
    }

    private void Update()
    {
        if (_targetFinder.CurrentTarget == null) return;

        _agent.SetDestination(_targetFinder.CurrentTarget.position);

        float distance = Vector2.Distance(transform.position, _targetFinder.CurrentTarget.position);

        if (distance <= stats.attackRange)
        {
            _agent.isStopped = true;
            OnDestinationReached?.Invoke();
        }
        else
        {
            _agent.isStopped = false;
            OnDestinationLost?.Invoke();
        }
    }
    public float GetCurrentSpeed()
    {
        return _agent.isStopped ? 0f : _agent.velocity.magnitude;
    }
}