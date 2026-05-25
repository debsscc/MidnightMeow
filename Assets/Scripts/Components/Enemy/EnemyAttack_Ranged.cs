///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o ataque à distância do inimigo.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(EnemyTargetFinder))]
public class EnemyAttack_Ranged : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private EnemyTargetFinder _targetFinder;
    private float _cooldownTimer;

    public event System.Action OnAttack;

    private void Awake()
    {
        _targetFinder = GetComponent<EnemyTargetFinder>();
    }

    private void Update()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (_targetFinder.CurrentTarget != null && IsTargetInRange() && _cooldownTimer <= 0)
        {
            Attack();
        }
    }

    private bool IsTargetInRange()
    {
        if (_targetFinder.CurrentTarget == null) return false;
        float distance = Vector2.Distance(transform.position, _targetFinder.CurrentTarget.position);
        return distance <= stats.rangedAttackRange;
    }

    private void Attack()
    {
        _cooldownTimer = stats.rangedAttackCooldown;
        OnAttack?.Invoke();

        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 direction = (_targetFinder.CurrentTarget.position - firePoint.position).normalized;
            Quaternion rotation = ProjectileAimUtility.RotationFromDirection(
                direction,
                ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);
            
            Instantiate(projectilePrefab, firePoint.position, rotation);
        }
    }
}
