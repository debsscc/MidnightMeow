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
    private EnemyMovement _movement;
    private EnemyAttack_Melee _attack;
    [SerializeField] private HealthComponent healthComponent;

    // Hashes
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashOnAttack = Animator.StringToHash("OnAttack");
    private readonly int _hashOnTakeDamage = Animator.StringToHash("OnTakeDamage");
    private readonly int _hashOnDie = Animator.StringToHash("OnDie");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<EnemyMovement>();
        //_attack = GetComponent<EnemyAttack_Melee>() || GetComponent<EnemyAttack_Ranged>();
        // _health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        _attack.OnAttack += HandleAttack;
        healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
        healthComponent.OnDied.AddListener(HandleDie);
    }

    private void OnDisable()
    {
        _attack.OnAttack -= HandleAttack;
        healthComponent.OnDied.RemoveListener(HandleDie);
        healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
    }

    private void Update()
    {
        _animator.SetFloat(_hashMoveSpeed, _movement.GetCurrentSpeed());
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