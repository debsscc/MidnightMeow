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
    [SerializeField] private PlayerMeleeCombat playerMeleeCombat;
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

    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashOnShoot = Animator.StringToHash("OnShoot");
    private readonly int _hashOnPull = Animator.StringToHash("OnPull");
    private readonly int _hashOnHit = Animator.StringToHash("OnHit");
    private readonly int _hashOnTakeDamage = Animator.StringToHash("OnDamage");
    private readonly int _hashOnDie = Animator.StringToHash("OnDie");
    private readonly int _hashAttackSpeed = Animator.StringToHash("AttackSpeed");
    private readonly int _hashOnAbility1 = Animator.StringToHash("OnAbility1");
    private readonly int _hashOnAbility2 = Animator.StringToHash("OnAbility2");
    private readonly int _hashOnDash = Animator.StringToHash("OnDash");

    [Header("Attack Animation")]
    [SerializeField] private float _attackAnimClipLength = 0.333f;

    [Header("Death Animation")]
    [SerializeField] private float _deathDestroyDelay = 4f;

    private float _lastAttackTriggerTime = float.NegativeInfinity;
    private bool _loggedOnce;
    private bool _useNetworkMoveSpeed;
    private float _networkMoveSpeed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<Collider2D>();

        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerMeleeCombat == null) playerMeleeCombat = GetComponent<PlayerMeleeCombat>();
        if (playerAbilityHandler == null) playerAbilityHandler = GetComponent<PlayerAbilityHandler>();
    }

    private void OnEnable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot += HandleShoot;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnAttackPerformed += HandleMeleeAttack;
        if (playerAbilityHandler != null)
            playerAbilityHandler.OnAbilityActivated += HandleAbility;
        if (playerMovement != null)
            playerMovement.OnFlipSprite += HandleFlipSprite;
        if (healthComponent != null)
        {
            healthComponent.OnDied.AddListener(HandleDeath);
            healthComponent.OnTakeDamage.AddListener(HandleHit);
        }
    }

    private void OnDisable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot -= HandleShoot;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnAttackPerformed -= HandleMeleeAttack;
        if (playerAbilityHandler != null)
            playerAbilityHandler.OnAbilityActivated -= HandleAbility;
        if (playerMovement != null)
            playerMovement.OnFlipSprite -= HandleFlipSprite;
        if (healthComponent != null)
        {
            healthComponent.OnDied.RemoveListener(HandleDeath);
            healthComponent.OnTakeDamage.RemoveListener(HandleHit);
        }
    }

    public void SetUseNetworkMoveSpeed(bool enabled) => _useNetworkMoveSpeed = enabled;

    public void ApplyNetworkMoveSpeed(float speed) => _networkMoveSpeed = speed;

    public void PlayRemoteAttackAnimation() => TriggerAttackAnimation();

    private void Update()
    {
        float moveSpeed = _useNetworkMoveSpeed ? _networkMoveSpeed : _rb.linearVelocity.magnitude;
        _animator.SetFloat(_hashMoveSpeed, moveSpeed);

        float attackSpeedMult = 1f;
        if (playerShooting != null && playerShooting.BaseFireRate > 0f)
            attackSpeedMult = _attackAnimClipLength * playerShooting.CurrentFireRate;
        else if (playerMeleeCombat != null && playerMeleeCombat.CombatStats != null)
            attackSpeedMult = _attackAnimClipLength / Mathf.Max(0.1f, playerMeleeCombat.CombatStats.attackCooldown);

        _animator.SetFloat(_hashAttackSpeed, Mathf.Max(0.1f, attackSpeedMult));
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void PlayAbilityAnimation(CharacterAbilityType abilityType)
    {
        switch (abilityType)
        {
            case CharacterAbilityType.NixPush:
            case CharacterAbilityType.CoraBarrier:
                TrySetTrigger(_hashOnAbility1);
                break;
            case CharacterAbilityType.NixCharge:
            case CharacterAbilityType.CoraPool:
                TrySetTrigger(_hashOnAbility2);
                break;
            case CharacterAbilityType.Dash:
                TrySetTrigger(_hashOnDash);
                break;
        }
    }

    private void TrySetTrigger(int hash)
    {
        if (!HasAnimatorTrigger(hash))
            return;

        _animator.SetTrigger(hash);
    }

    private bool HasAnimatorTrigger(int hash)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger && parameters[i].nameHash == hash)
                return true;
        }

        return false;
    }

    private void HandleShoot() => TriggerAttackAnimation();

    private void HandleMeleeAttack(Vector2 origin, Vector2 direction, MeleeCombatStats stats)
        => TriggerAttackAnimation();

    private void TriggerAttackAnimation()
    {
        float attackInterval = 0.2f;
        if (playerShooting != null && playerShooting.CurrentFireRate > 0f)
            attackInterval = 1f / playerShooting.CurrentFireRate;
        else if (playerMeleeCombat?.CombatStats != null)
            attackInterval = playerMeleeCombat.CombatStats.attackCooldown;

        if (Time.time - _lastAttackTriggerTime >= attackInterval - 0.016f)
        {
            _animator.ResetTrigger(_hashOnShoot);
            _animator.SetTrigger(_hashOnShoot);
            _lastAttackTriggerTime = Time.time;
        }
    }

    private void HandleFlipSprite(bool facingRight) => ApplyFacingToRenderers(facingRight);

    public void ApplyNetworkFacing(bool facingRight) => ApplyFacingToRenderers(facingRight);

    private void ApplyFacingToRenderers(bool facingRight)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = facingRight;
        if (shadowSpriteRenderer != null)
            shadowSpriteRenderer.flipX = facingRight;
    }

    private void HandleHit()
    {
        if (healthComponent != null && !healthComponent.IsDead)
            _animator.SetTrigger(_hashOnTakeDamage);
    }

    private void HandleAbility(CharacterAbilityType abilityType) => PlayAbilityAnimation(abilityType);

    public void HandleDeath()
    {
        if (healthComponent != null)
            healthComponent.SetDestroyDelay(_deathDestroyDelay);

        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;

        if (_collider2D != null) _collider2D.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;

        _animator.SetTrigger(_hashOnDie);
    }

    private void UpdateSortingOrder()
    {
        if (_spriteRenderer == null) return;

        float referenceY = _collider2D != null ? _collider2D.bounds.min.y : transform.position.y;
        int newOrder = sortingOrderOffset - Mathf.RoundToInt(referenceY * sortingPrecision);

        if (!_loggedOnce)
        {
            _loggedOnce = true;
            Debug.Log($"[PlayerAnimationHandler] sortingOrder={newOrder}");
        }

        _spriteRenderer.sortingOrder = newOrder;
    }
}
