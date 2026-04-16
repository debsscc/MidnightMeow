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
    private int sortingOrderOffset = 5000;
    [SerializeField] private int sortingPrecision = 100;

    private Animator _animator;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider2D;

    // Hashes dos parametros para performance (evita usar strings)
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashOnShoot = Animator.StringToHash("OnShoot");
    private readonly int _hashOnPull = Animator.StringToHash("OnPull");
    private readonly int _hashOnHit = Animator.StringToHash("OnHit");
    private readonly int _hashOnDie = Animator.StringToHash("OnDie");

    private bool _loggedOnce = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<Collider2D>();

        Debug.Log($"[PlayerAnimationHandler] Awake | SpriteRenderer={_spriteRenderer != null} | Collider2D={_collider2D != null} | SortingOffset={sortingOrderOffset} | Precision={sortingPrecision}");
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

    private void LateUpdate()
    {
        UpdateSortingOrder();
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

    private void UpdateSortingOrder()
    {
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("[PlayerAnimationHandler] SpriteRenderer é null! O sprite não será desenhado.");
            return;
        }

        float referenceY = _collider2D != null ? _collider2D.bounds.min.y : transform.position.y;
        int newOrder = sortingOrderOffset - Mathf.RoundToInt(referenceY * sortingPrecision);

        if (!_loggedOnce)
        {
            _loggedOnce = true;
            Debug.Log($"[PlayerAnimationHandler] Primeiro LateUpdate | posY={transform.position.y:F2} | colliderMinY={referenceY:F2} | sortingOrder={newOrder} | enabled={_spriteRenderer.enabled} | color={_spriteRenderer.color}");
        }

        _spriteRenderer.sortingOrder = newOrder;
    }
}