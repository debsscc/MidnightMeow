///* ----------------------------------------------------------------
// ATUALIZADO EM: 22-05-2026
// DESCRIÇÃO: Movimento via NavMesh: persegue alvo ou patrulha (random walk) sem alvo no raio.
// Respeita stun de dano.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyTargetFinder))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private NavMeshAgent _agent;
    private EnemyTargetFinder _targetFinder;
    private EnemyHitStun _hitStun;
    private HealthComponent _health;

    public event System.Action OnDestinationReached;
    public event System.Action OnDestinationLost;
    public event System.Action<bool> OnFlipSprite;

    private bool _isFacingRight;
    private const float FlipThreshold = 0.05f;

    private float _nextRandomWalkTime;
    private bool _hasRandomDestination;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _targetFinder = GetComponent<EnemyTargetFinder>();
        _hitStun = GetComponent<EnemyHitStun>();
        _health = GetComponent<HealthComponent>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        if (stats != null)
            _agent.speed = stats.moveSpeed;

        _isFacingRight = transform.localScale.x >= 0f;
    }

    private void Start()
    {
        OnFlipSprite?.Invoke(_isFacingRight);
    }

    private void Update()
    {
        if (stats == null) return;
        if (_health != null && !_health.IsAlive) return;

        if (_hitStun != null && _hitStun.IsStunned)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            return;
        }

        if (!_targetFinder.HasTarget)
        {
            _hasRandomDestination = false;
            PatrolRandomWalk();
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(_targetFinder.CurrentTarget.position);

        float deltaX = _targetFinder.CurrentTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > FlipThreshold)
        {
            bool shouldFaceRight = deltaX > 0f;
            if (shouldFaceRight != _isFacingRight)
            {
                _isFacingRight = shouldFaceRight;
                OnFlipSprite?.Invoke(_isFacingRight);
            }
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

    private void PatrolRandomWalk()
    {
        if (Time.time < _nextRandomWalkTime && _hasRandomDestination) return;

        _nextRandomWalkTime = Time.time + Mathf.Max(0.5f, stats.randomWalkInterval);
        _hasRandomDestination = true;

        Vector2 offset = Random.insideUnitCircle * stats.randomWalkRadius;
        Vector3 sampleOrigin = transform.position + (Vector3)offset;

        if (NavMesh.SamplePosition(sampleOrigin, out NavMeshHit hit, stats.randomWalkRadius, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }
    }

    public float GetCurrentSpeed()
    {
        if (_hitStun != null && _hitStun.IsStunned) return 0f;
        return _agent.isStopped ? 0f : _agent.velocity.magnitude;
    }
}
