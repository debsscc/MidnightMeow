///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Componente que gerencia o movimento do inimigo em dire��o ao seu alvo usando NavMeshAgent.
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
    // Evento acionado quando a orientação do sprite muda.
    // O booleano indica se o inimigo está virado para a direita (true) ou para a esquerda (false).
    public event System.Action<bool> OnFlipSprite;

    private bool _isFacingRight;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _targetFinder = GetComponent<EnemyTargetFinder>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = stats.moveSpeed;

        // Inicializa estado de orientação com base na escala local X
        _isFacingRight = transform.localScale.x >= 0f;
        OnFlipSprite?.Invoke(_isFacingRight);
    }

    private void Update()
    {
        if (_targetFinder.CurrentTarget == null) return;

        _agent.SetDestination(_targetFinder.CurrentTarget.position);

        // Detecta direção (simples): se o alvo está à direita do inimigo
        bool shouldFaceRight = _targetFinder.CurrentTarget.position.x >= transform.position.x;
        if (shouldFaceRight != _isFacingRight)
        {
            _isFacingRight = shouldFaceRight;
            OnFlipSprite?.Invoke(_isFacingRight);
        }

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