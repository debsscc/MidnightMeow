///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia as animações do inimigo com base em suas ações e estado.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(Animator), typeof(EnemyMovement))]
public class EnemyAnimationHandler : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private EnemyMovement enemyMovement;
    private SpriteRenderer _spriteRenderer;
    private EnemyAttack_Melee _attack;
    private EnemyAttack_Ranged _attackRanged;
    private HealthComponent healthComponent;
    [SerializeField] private bool isMelee; // Para determinar se o inimigo é corpo a corpo ou ranged, caso ambos os componentes existam.

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private float _lastMoveSpeed = 0f;
    private const float SpeedEpsilon = 0.01f;

    // Hashes
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashOnAttack = Animator.StringToHash("OnAttack");
    private readonly int _hashOnTakeDamage = Animator.StringToHash("OnTakeDamage");
    private readonly int _hashOnDie = Animator.StringToHash("OnDie");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (isMelee)
            _attack = GetComponent<EnemyAttack_Melee>();
        else
            _attackRanged = GetComponent<EnemyAttack_Ranged>();
        healthComponent = GetComponent<HealthComponent>();

        if (debugLogs)
        {
            Debug.Log($"EnemyAnimationHandler.Awake - {gameObject.name}: animator={_animator!=null}, enemyMovement={enemyMovement!=null}, spriteRenderer={_spriteRenderer!=null}, healthComponent={healthComponent!=null}, isMelee={isMelee}");
        }
    }

    private void OnEnable()
    {
        if (isMelee && _attack != null)
            _attack.OnAttack += HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack += HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite += HandleFlipSprite;

        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
            healthComponent.OnDied.AddListener(HandleDie);
        }
    }

    private void OnDisable()
    {
        if (isMelee && _attack != null)
            _attack.OnAttack -= HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack -= HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite -= HandleFlipSprite;

        if (healthComponent != null)
        {
            healthComponent.OnDied.RemoveListener(HandleDie);
            healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }
    }

    private void Update()
    {
        if (_animator == null) return;

        float speed = enemyMovement != null ? enemyMovement.GetCurrentSpeed() : 0f;
        _animator.SetFloat(_hashMoveSpeed, speed);

        if (debugLogs && Mathf.Abs(speed - _lastMoveSpeed) > SpeedEpsilon)
        {
            Debug.Log($"EnemyAnimationHandler.Update - {gameObject.name}: MoveSpeed changed {_lastMoveSpeed} -> {speed}");
            _lastMoveSpeed = speed;
        }
    }

    private void HandleFlipSprite(bool facingRight)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = facingRight;
    }

    private void HandleAttack()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleAttack - {gameObject.name}");
        _animator.SetTrigger(_hashOnAttack);
    }

    private void HandleTakeDamage()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleTakeDamage - {gameObject.name}");
        _animator.SetTrigger(_hashOnTakeDamage);
    }

    private void HandleDie()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleDie - {gameObject.name}");
        _animator.SetTrigger(_hashOnDie);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleHealthChanged - {gameObject.name}: current={current}, max={max}");

        if (_animator == null) return;

        if (current > 0 && current < max)
        {
            if (debugLogs) Debug.Log($"EnemyAnimationHandler.Triggering TakeDamage for {gameObject.name}");
            _animator.SetTrigger(_hashOnTakeDamage);
        }
    }
}
