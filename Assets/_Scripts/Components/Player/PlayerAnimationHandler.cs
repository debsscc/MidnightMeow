///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia as animações do jogador com base em suas ações e estado.
// ---------------------------------------------------------------- */
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class PlayerAnimationHandler : MonoBehaviour
{
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private PlayerAbilityHandler playerAbilityHandler;
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private PlayerMovement playerMovement;

    private Animator _animator;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    // Hashes dos parametros para performance (evita usar strings)
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashOnShoot = Animator.StringToHash("OnShoot");
    private readonly int _hashOnPull = Animator.StringToHash("OnPull");
    private readonly int _hashOnHit = Animator.StringToHash("OnHit");
    private readonly int _hashOnDie = Animator.StringToHash("OnDie");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        playerShooting.OnShoot += HandleShoot;
        playerAbilityHandler.OnAbilityActivated += HandleAbility;
        playerMovement.OnFlipSprite += HandleFlipSprite;
        healthComponent.OnDied.AddListener(HandleDeath);
        
    }

    private void OnDisable()
    {
        // Limpa as assinaturas
        playerShooting.OnShoot -= HandleShoot;
        playerAbilityHandler.OnAbilityActivated -= HandleAbility;
        playerMovement.OnFlipSprite -= HandleFlipSprite;
        healthComponent.OnDied.RemoveListener(HandleDeath);
    }

    private void Update()
    {
        _animator.SetFloat(_hashMoveSpeed, _rb.linearVelocity.magnitude);
    }

    private void HandleShoot()
    {
        _animator.SetTrigger(_hashOnShoot);
    }

    private void HandleFlipSprite(bool facingRight)
    {
        _spriteRenderer.flipX = facingRight;
    }

    private void HandleAbility(Ability ability)
    {
        if (ability is Ability_ProjectilePull)
        {
            _animator.SetTrigger(_hashOnPull);
        }
        else if (ability is Ability_ProjectileReflect)
        {
            _animator.SetTrigger(_hashOnHit);
        }
    }

    public void HandleDeath() 
    {
        _animator.SetTrigger(_hashOnDie);
    }
}