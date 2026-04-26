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

    [SerializeField] private SpriteRenderer shadowSpriteRenderer;

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
    private readonly int _hashAttackSpeed = Animator.StringToHash("AttackSpeed");

    [Header("Attack Animation")]
    [Tooltip("Duração do clip de ataque em segundos com speed=1. Ajuste para coincidir com o clip real no Animator.")]
    [SerializeField] private float _attackAnimClipLength = 0.333f;

    [Header("Death Animation")]
    [Tooltip("Segundos que o player deve existir após morrer (deve ser >= duração do clip de morte).")]
    [SerializeField] private float _deathDestroyDelay = 4f;

    // Controla se a animação de ataque já está rodando para não cancelá-la
    private float _lastAttackTriggerTime = float.NegativeInfinity;

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

        // Sincroniza a velocidade da animação de ataque com o fire rate atual.
        // AttackSpeed = (duração_clip * CurrentFireRate) → animação sempre dura exatamente 1/CurrentFireRate segundos.
        // IMPORTANTE: adicione o parâmetro float "AttackSpeed" no Animator e use-o como
        // Multiplier de Speed no estado de ataque.
        float attackSpeedMult = (playerShooting != null && playerShooting.BaseFireRate > 0f)
            ? _attackAnimClipLength * playerShooting.CurrentFireRate
            : 1f;
        _animator.SetFloat(_hashAttackSpeed, Mathf.Max(0.1f, attackSpeedMult));
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    private void HandleShoot()
    {
        // Só re-aciona a animação se o intervalo mínimo passou (evita cancelar a animação corrente).
        float attackInterval = (playerShooting != null && playerShooting.CurrentFireRate > 0f)
            ? 1f / playerShooting.CurrentFireRate
            : 0.2f;

        if (Time.time - _lastAttackTriggerTime >= attackInterval - 0.016f)
        {
            _animator.ResetTrigger(_hashOnShoot); // limpa trigger em fila antes de reativar
            _animator.SetTrigger(_hashOnShoot);
            _lastAttackTriggerTime = Time.time;
        }
    }

    private void HandleFlipSprite(bool facingRight)
    {
        ApplyFacingToRenderers(facingRight);
    }

    /// <summary>
    /// Aplica flip vindo da rede (mesma convenção de HandleFlipSprite / movimento).
    /// </summary>
    public void ApplyNetworkFacing(bool facingRight)
    {
        ApplyFacingToRenderers(facingRight);
    }

    private void ApplyFacingToRenderers(bool facingRight)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = facingRight;
        if (shadowSpriteRenderer != null)
            shadowSpriteRenderer.flipX = facingRight;
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
        // SetDestroyDelay é chamado AQUI porque HandleDeath() é invocado dentro de
        // OnDied?.Invoke(), que ocorre ANTES de Destroy(gameObject, _destroyDelay) em
        // HealthComponent.Die(). Assim o delay correto é garantido na ordem certa.
        if (healthComponent != null)
            healthComponent.SetDestroyDelay(_deathDestroyDelay);

        // Para o player no lugar e desativa controles para que a animação de morte
        // possa tocar completamente antes do objeto ser destruído.
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;

        if (_collider2D != null) _collider2D.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;

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