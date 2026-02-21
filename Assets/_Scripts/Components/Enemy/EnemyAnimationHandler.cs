///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Componente que gerencia as anima��es do inimigo com base em suas a��es e estado.
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
    [SerializeField] private bool isMelee; // Para determinar se o inimigo � corpo a corpo ou ranged, caso ambos os componentes existam.

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
    }

    private void OnEnable()
    {
        if (isMelee && _attack != null)
            _attack.OnAttack += HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack += HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite += HandleFlipSprite;
        healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
        healthComponent.OnDied.AddListener(HandleDie);
    }

    private void OnDisable()
    {
        if (isMelee && _attack != null)
            _attack.OnAttack -= HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack -= HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite -= HandleFlipSprite;
        healthComponent.OnDied.RemoveListener(HandleDie);
        healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
    }

    private void Update()
    {
        _animator.SetFloat(_hashMoveSpeed, enemyMovement != null ? enemyMovement.GetCurrentSpeed() : 0f);
    }

    private void HandleFlipSprite(bool facingRight)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = facingRight;
    }

    private void HandleAttack()
    {
        _animator.SetTrigger(_hashOnAttack);
    }

    private void HandleTakeDamage()
    {
        _animator.SetTrigger(_hashOnTakeDamage);
    }

    private void HandleDie()
    {
        _animator.SetTrigger(_hashOnDie);
    }

    private void HandleHealthChanged(float current, float max)
    {
        // Se a vida diminuiu (l�gica simples), toca anima��o de dano.
        // Nota: Para ser perfeito, o HealthComponent idealmente teria um evento "OnDamaged",
        // mas podemos usar OnHealthChanged verificando se n�o estamos mortos.
        if (current > 0 && current < max)
        {
            _animator.SetTrigger(_hashOnTakeDamage);
        }
    }
}