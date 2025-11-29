///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o ataque corpo-a-corpo do inimigo.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyAttack_Melee : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private EnemyMovement _movement;
    private float _cooldownTimer;
    private bool _isInRange;

    public event System.Action OnAttack;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
    }

    private void OnEnable()
    {
        _movement.OnDestinationReached += () => _isInRange = true;
        _movement.OnDestinationLost += () => _isInRange = false;
    }

    private void OnDisable()
    {
        _movement.OnDestinationReached -= () => _isInRange = true;
        _movement.OnDestinationLost -= () => _isInRange = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (_isInRange && _cooldownTimer <= 0)
        {
            Attack();
        }
    }

    private void Attack()
    {
        _cooldownTimer = stats.attackCooldown;
        OnAttack?.Invoke();

        Debug.Log("Inimigo atacou!");
        Transform targetTransform = GetComponent<EnemyTargetFinder>().CurrentTarget;

        if (targetTransform != null)
        {
            // Verifica se o alvo tem vida
            if (targetTransform.TryGetComponent<IDamageable>(out IDamageable target))
            {
                // Aplica o dano definido no EnemyStats
                target.TakeDamage(stats.attackDamage, this.gameObject);
            }
        }
    }
}