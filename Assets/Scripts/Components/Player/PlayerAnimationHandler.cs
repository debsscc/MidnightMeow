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
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private AnimatorProfileBinder animationBinder;
    [SerializeField] private int sortingOrderOffset = 5000;
    [SerializeField] private int sortingPrecision = 100;

    [SerializeField] private SpriteRenderer shadowSpriteRenderer;

    [Header("Attack Animation")]
    [SerializeField] private float _attackAnimClipLength = 0.333f;

    [Header("Death Animation")]
    [SerializeField] private float _deathDestroyDelay = 4f;

    private Animator _animator;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider2D;

    private int _hashMoveSpeed;
    private int _hashOnShoot;
    private int _hashOnTakeDamage;
    private int _hashOnDie;
    private int _hashAttackSpeed;
    private int _hashOnAbility1;
    private int _hashOnAbility2;
    private int _hashOnDash;
    private int _hashOnDashAttack;
    private int _hashIsDashing;

    private NetworkPlayerAbilityRelay _abilityRelay;

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
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (animationBinder == null) animationBinder = GetComponent<AnimatorProfileBinder>();
        _abilityRelay = GetComponent<NetworkPlayerAbilityRelay>();

        ResolveAnimationHashes();
    }

    private void ResolveAnimationHashes()
    {
        if (animationBinder != null)
        {
            _hashMoveSpeed = animationBinder.GetMoveSpeedHash();
            _hashOnShoot = animationBinder.GetOnShootHash();
            _hashOnTakeDamage = animationBinder.GetOnTakeDamageHash();
            _hashOnDie = animationBinder.GetOnDieHash();
            _hashAttackSpeed = animationBinder.GetAttackSpeedHash();
            _hashOnAbility1 = animationBinder.GetOnAbility1Hash();
            _hashOnAbility2 = animationBinder.GetOnAbility2Hash();
            _hashOnDash = animationBinder.GetOnDashHash();
            _hashOnDashAttack = animationBinder.GetOnDashAttackHash();
            _hashIsDashing = animationBinder.GetIsDashingHash();
            _attackAnimClipLength = animationBinder.AttackAnimClipLength;
            _deathDestroyDelay = animationBinder.DeathDestroyDelay;

            if (animationBinder.Profile != null)
            {
                sortingOrderOffset = animationBinder.Profile.sortingOrderOffset;
                sortingPrecision = animationBinder.Profile.sortingPrecision;
            }

            return;
        }

        _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
        _hashOnShoot = Animator.StringToHash("OnShoot");
        _hashOnTakeDamage = Animator.StringToHash("OnDamage");
        _hashOnDie = Animator.StringToHash("OnDie");
        _hashAttackSpeed = Animator.StringToHash("AttackSpeed");
        _hashOnAbility1 = Animator.StringToHash("OnAbility1");
        _hashOnAbility2 = Animator.StringToHash("OnAbility2");
        _hashOnDash = Animator.StringToHash("OnDash");
        _hashOnDashAttack = Animator.StringToHash("OnDashAttack");
        _hashIsDashing = Animator.StringToHash("IsDashing");
    }

    private void OnEnable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot += HandleShoot;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnMeleeAttackStarted += HandleMeleeAttackStarted;
        if (playerAbilityHandler != null)
            playerAbilityHandler.OnAbilityActivated += HandleAbility;
        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.AddListener(HandleHit);
        }
    }

    private void OnDisable()
    {
        if (playerShooting != null)
            playerShooting.OnShoot -= HandleShoot;
        if (playerMeleeCombat != null)
            playerMeleeCombat.OnMeleeAttackStarted -= HandleMeleeAttackStarted;
        if (playerAbilityHandler != null)
            playerAbilityHandler.OnAbilityActivated -= HandleAbility;
        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.RemoveListener(HandleHit);
        }
    }

    public void SetUseNetworkMoveSpeed(bool enabled) => _useNetworkMoveSpeed = enabled;

    public void ApplyNetworkMoveSpeed(float speed) => _networkMoveSpeed = speed;

    public void PlayRemoteAttackAnimation() => TriggerAttackAnimation();

    public void PlayRemoteDashAttackAnimation() => TriggerDashAttackAnimation();

    public void TriggerMeleeAttackAnimation()
    {
        if (playerDash != null && playerDash.IsDashing)
            TriggerDashAttackAnimation();
        else
            TriggerAttackAnimation();
    }

    public float GetMeleeStrikeDelay()
    {
        float clipLength = _attackAnimClipLength > 0f ? _attackAnimClipLength : 0.333f;
        return clipLength / Mathf.Max(0.1f, GetMeleeAttackSpeedMultiplier());
    }

    private float GetMeleeAttackSpeedMultiplier()
    {
        if (playerMeleeCombat == null || playerMeleeCombat.CombatStats == null)
            return 1f;

        float baseSpeed = _attackAnimClipLength / Mathf.Max(0.1f, playerMeleeCombat.CombatStats.attackCooldown);
        return baseSpeed * Mathf.Max(0.1f, playerMeleeCombat.CombatStats.attackAnimationSpeedMultiplier);
    }

    private void Update()
    {
        float moveSpeed = _useNetworkMoveSpeed ? _networkMoveSpeed : _rb.linearVelocity.magnitude;
        _animator.SetFloat(_hashMoveSpeed, moveSpeed);

        if (_hashIsDashing != 0 && HasAnimatorBool(_hashIsDashing))
        {
            bool isDashing = ResolveIsDashingForAnimator();
            _animator.SetBool(_hashIsDashing, isDashing);
        }

        float attackSpeedMult = 1f;
        if (playerShooting != null && playerShooting.BaseFireRate > 0f)
            attackSpeedMult = _attackAnimClipLength * playerShooting.CurrentFireRate;
        else if (playerMeleeCombat != null && playerMeleeCombat.CombatStats != null)
            attackSpeedMult = GetMeleeAttackSpeedMultiplier();

        _animator.SetFloat(_hashAttackSpeed, Mathf.Max(0.1f, attackSpeedMult));
    }

    private void LateUpdate() => UpdateSortingOrder();

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

    private bool TrySetTrigger(int hash)
    {
        if (!HasAnimatorTrigger(hash))
            return false;

        _animator.SetTrigger(hash);
        return true;
    }

    private bool HasAnimatorBool(int hash)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Bool && parameters[i].nameHash == hash)
                return true;
        }

        return false;
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

    private void HandleMeleeAttackStarted() => TriggerMeleeAttackAnimation();

    private void TriggerAttackAnimation()
    {
        if (!CanFireAttackTrigger())
            return;

        TrySetTrigger(_hashOnShoot);
        _lastAttackTriggerTime = Time.time;
    }

    private void TriggerDashAttackAnimation()
    {
        if (!CanFireAttackTrigger())
            return;

        if (!TrySetTrigger(_hashOnDashAttack))
            TrySetTrigger(_hashOnShoot);

        _lastAttackTriggerTime = Time.time;
    }

    private bool ResolveIsDashingForAnimator()
    {
        if (playerDash != null && playerDash.IsDashing)
            return true;

        if (_abilityRelay != null && _abilityRelay.IsSpawned && !_abilityRelay.IsOwner)
            return _abilityRelay.NetworkIsDashing;

        return false;
    }

    private bool CanFireAttackTrigger()
    {
        float attackInterval = 0.2f;
        if (playerShooting != null && playerShooting.CurrentFireRate > 0f)
            attackInterval = 1f / playerShooting.CurrentFireRate;
        else if (playerMeleeCombat?.CombatStats != null)
            attackInterval = playerMeleeCombat.CombatStats.attackCooldown;

        return Time.time - _lastAttackTriggerTime >= attackInterval - 0.016f;
    }

    public void ApplyNetworkFacing(bool facingRight) => ApplyFacingToRenderers(facingRight);

    private void ApplyFacingToRenderers(bool facingRight)
    {
        // Sprites dos personagens foram desenhados olhando para a direita; flipX espelha para a esquerda.
        bool flipX = !facingRight;
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = flipX;
        if (shadowSpriteRenderer != null)
            shadowSpriteRenderer.flipX = flipX;
    }

    private void HandleHit()
    {
        if (healthComponent != null && !healthComponent.IsDead)
            _animator.SetTrigger(_hashOnTakeDamage);
    }

    private void HandleAbility(CharacterAbilityType abilityType) => PlayAbilityAnimation(abilityType);

    public void HandleDeath()
    {
        if (healthComponent != null && !TryGetComponent<PlayerDeathPresentation>(out _))
            healthComponent.SetDestroyDelay(_deathDestroyDelay);

        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;

        if (_collider2D != null) _collider2D.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
        if (TryGetComponent<PlayerFacingController>(out var facingController))
            facingController.enabled = false;
        if (TryGetComponent<PlayerAim>(out var aim))
            aim.enabled = false;

        _animator.SetTrigger(_hashOnDie);
    }

    private void UpdateSortingOrder()
    {
        if (_spriteRenderer == null)
            return;

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
