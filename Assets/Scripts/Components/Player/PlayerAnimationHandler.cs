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
    [Tooltip("Ajuste fino do Y de referência de profundidade (alinhe os pés do player ao mesmo critério dos inimigos).")]
    [SerializeField] private float sortingReferenceYOffset = 0f;

    [SerializeField] private SpriteRenderer shadowSpriteRenderer;

    [Header("Attack Animation")]
    [SerializeField] private float _attackAnimClipLength = 0.333f;

    [Header("Death Animation")]
    [SerializeField] private float _deathDestroyDelay = 4f;
    [SerializeField] private int deadSortingOrderBoost = 50;

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
    private int _shootingStateHash;
    private int _meleeAttackStateHash;

    private NetworkPlayerAbilityRelay _abilityRelay;

    private float _defaultAnimatorSpeed = 1f;

    private float _lastAttackTriggerTime = float.NegativeInfinity;
    private bool _loggedOnce;
    private bool _useNetworkMoveSpeed;
    private float _networkMoveSpeed;
    private Transform _dustFacingTransform;
    private Vector3 _dustBaseLocalScale;
    private NetworkPlayerHealth _networkHealth;
    private bool _deathPresentationActive;
    private float _rangedFireReleaseNormalizedTime = 0.45f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = ResolveBodyCollider();

        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerMeleeCombat == null) playerMeleeCombat = GetComponent<PlayerMeleeCombat>();
        if (playerAbilityHandler == null) playerAbilityHandler = GetComponent<PlayerAbilityHandler>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (healthComponent == null) healthComponent = GetComponent<HealthComponent>();
        if (animationBinder == null) animationBinder = GetComponent<AnimatorProfileBinder>();
        _abilityRelay = GetComponent<NetworkPlayerAbilityRelay>();
        _networkHealth = GetComponent<NetworkPlayerHealth>();

        if (_animator != null)
            _defaultAnimatorSpeed = _animator.speed;

        if (playerMovement != null && playerMovement.dustParticle != null)
        {
            _dustFacingTransform = playerMovement.dustParticle.transform;
            _dustBaseLocalScale = _dustFacingTransform.localScale;
        }

        ResolveAnimationHashes();
        ResolveRangedFireReleaseFromClip();
    }

    private void ResolveRangedFireReleaseFromClip()
    {
        AnimationClip attackClip = animationBinder != null && animationBinder.Profile != null
            ? animationBinder.Profile.attackClip
            : null;

        if (attackClip != null && attackClip.length > 0f)
            _attackAnimClipLength = attackClip.length;

        if (AnimationClipFireReleaseUtility.TryGetReleaseNormalizedTime(attackClip, out float normalizedTime))
            _rangedFireReleaseNormalizedTime = normalizedTime;
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

                string attackStateName = animationBinder.Profile.attackAnimatorStateName;
                _shootingStateHash = string.IsNullOrEmpty(attackStateName)
                    ? Animator.StringToHash("Shooting")
                    : Animator.StringToHash(attackStateName);

                string meleeStateName = animationBinder.Profile.meleeAttackAnimatorStateName;
                _meleeAttackStateHash = string.IsNullOrEmpty(meleeStateName)
                    ? 0
                    : Animator.StringToHash(meleeStateName);
            }

            return;
        }

        _shootingStateHash = Animator.StringToHash("Shooting");
        _meleeAttackStateHash = Animator.StringToHash("Hitting");

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
            playerShooting.OnProjectileInstantiated += HandleProjectileFired;
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
            playerShooting.OnProjectileInstantiated -= HandleProjectileFired;
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

    public void PlayRemoteAttackAnimation()
    {
        if (playerShooting != null)
            TriggerRangedAttackAnimation();
        else
            TriggerAttackAnimation();
    }

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
        MeleeCombatStats stats = playerMeleeCombat != null ? playerMeleeCombat.CombatStats : null;
        return MeleeStrikeTimingUtility.ComputeStrikeDelay(
            stats,
            clipLength,
            GetMeleeAttackSpeedMultiplier());
    }

    public float GetMeleeRecoveryDelay()
    {
        float clipLength = _attackAnimClipLength > 0f ? _attackAnimClipLength : 0.333f;
        MeleeCombatStats stats = playerMeleeCombat != null ? playerMeleeCombat.CombatStats : null;
        return MeleeStrikeTimingUtility.ComputeRecoveryDelay(
            stats,
            clipLength,
            GetMeleeAttackSpeedMultiplier());
    }

    /// <summary>True enquanto o clip de ataque principal (Shooting / Hitting) está ativo.</summary>
    public bool IsPrimaryAttackAnimationPlaying()
    {
        return IsInRangedAttackAnimation() || IsInMeleeAttackAnimation();
    }

    public bool IsInRangedAttackAnimation() => TryGetShootingNormalizedTime(out _);

    public bool IsInMeleeAttackAnimation()
    {
        if (_animator == null || _meleeAttackStateHash == 0)
            return false;

        return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _meleeAttackStateHash;
    }

    /// <summary>True quando o estado de ataque ranged atingiu o frame PerformFire do clip.</summary>
    public bool IsRangedFireReleaseReady()
    {
        if (_animator == null || _shootingStateHash == 0)
            return false;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != _shootingStateHash)
            return false;

        return state.normalizedTime >= _rangedFireReleaseNormalizedTime;
    }

    /// <summary>Timeout de segurança: duração real do clip com AttackSpeed aplicado.</summary>
    public float GetRangedAttackMaxWaitSeconds()
    {
        float clipLength = _attackAnimClipLength > 0f ? _attackAnimClipLength : 0.517f;
        return clipLength / GetRangedClipSpeedMultiplier();
    }

    /// <summary>Tempo em segundos (wall clock) até o frame PerformFire do clip.</summary>
    public float GetRangedFireReleaseWallSeconds()
    {
        float clipLength = _attackAnimClipLength > 0f ? _attackAnimClipLength : 0.517f;
        return clipLength * _rangedFireReleaseNormalizedTime / GetRangedClipSpeedMultiplier();
    }

    /// <summary>
    /// Animation Event no clip de ataque. Mantido para compatibilidade com os clips;
    /// o disparo é autoritativo em <see cref="PlayerShooting.ExecuteShot"/> e a animação
    /// é acionada em <see cref="HandleProjectileFired"/>.
    /// </summary>
    public void PerformFire()
    {
    }

    private float GetRangedClipSpeedMultiplier()
    {
        if (playerShooting != null && playerShooting.CurrentFireRate > 0f)
            return Mathf.Max(0.1f, _attackAnimClipLength * playerShooting.CurrentFireRate);

        return 1f;
    }

    private float GetMeleeClipSpeedMultiplier() => GetMeleeAttackSpeedMultiplier();

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

        ApplyPrimaryAttackClipSpeed();
    }

    /// <summary>Acelera o playback do clip de ataque via Animator.speed (não o float AttackSpeed).</summary>
    private void ApplyPrimaryAttackClipSpeed()
    {
        if (_animator == null)
            return;

        if (IsInRangedAttackAnimation())
        {
            _animator.speed = _defaultAnimatorSpeed * GetRangedClipSpeedMultiplier();
            return;
        }

        if (IsInMeleeAttackAnimation())
        {
            _animator.speed = _defaultAnimatorSpeed * GetMeleeClipSpeedMultiplier();
            return;
        }

        _animator.speed = _defaultAnimatorSpeed;

        // DashAttack / legado: estados que ainda usam o parâmetro AttackSpeed no controller.
        if (_hashAttackSpeed != 0 && playerMeleeCombat != null && playerMeleeCombat.CombatStats != null)
            _animator.SetFloat(_hashAttackSpeed, GetMeleeAttackSpeedMultiplier());
        else if (_hashAttackSpeed != 0)
            _animator.SetFloat(_hashAttackSpeed, 1f);
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

    private bool HasAnimatorState(int stateHash)
    {
        if (_animator == null)
            return false;

        return _animator.HasState(0, stateHash);
    }

    private void HandleProjectileFired(GameObject _, Vector3 __, Quaternion ___, Vector2 ____)
        => TriggerRangedAttackAnimation();

    private void HandleMeleeAttackStarted() => TriggerMeleeAttackAnimation();

    public void TriggerRangedAttackAnimation()
    {
        if (_animator == null || _animator.layerCount == 0)
            return;

        _lastAttackTriggerTime = Time.time;

        if (TryGetShootingNormalizedTime(out float normalizedTime))
        {
            // Windup ainda rolando — deixa o clip acelerar via Animator.speed (não corta).
            if (normalizedTime < _rangedFireReleaseNormalizedTime)
                return;

            if (HasAnimatorState(_shootingStateHash))
            {
                _animator.Play(_shootingStateHash, 0, 0f);
                return;
            }
        }

        if (HasAnimatorState(_shootingStateHash))
            _animator.Play(_shootingStateHash, 0, 0f);
        else
            TrySetTrigger(_hashOnShoot);
    }

    private bool TryGetShootingNormalizedTime(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (_animator == null || _shootingStateHash == 0)
            return false;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != _shootingStateHash)
            return false;

        normalizedTime = state.normalizedTime;
        return true;
    }

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

        ApplyDustFacing(facingRight);
    }

    private void ApplyDustFacing(bool facingRight)
    {
        if (_dustFacingTransform == null)
            return;

        float sign = facingRight ? 1f : -1f;
        _dustFacingTransform.localScale = new Vector3(
            Mathf.Abs(_dustBaseLocalScale.x) * sign,
            _dustBaseLocalScale.y,
            _dustBaseLocalScale.z);
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

        _deathPresentationActive = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
        if (TryGetComponent<PlayerFacingController>(out var facingController))
            facingController.enabled = false;
        if (TryGetComponent<PlayerAim>(out var aim))
            aim.enabled = false;

        _animator.SetTrigger(_hashOnDie);

        if (!TryGetComponent<PlayerDeathPresentation>(out _))
            FinalizeDeathPhysics();
    }

    /// <summary>Desliga colisor após a animação de morte; mantém sorting via sprite bounds até lá.</summary>
    public void FinalizeDeathPhysics()
    {
        if (_collider2D != null)
            _collider2D.enabled = false;
    }

    /// <summary>Restaura física e animator após reviver de inconsciência.</summary>
    public void RestoreFromDowned()
    {
        _deathPresentationActive = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = true;
        }

        if (_collider2D != null)
            _collider2D.enabled = true;

        if (_animator != null)
        {
            _animator.speed = _defaultAnimatorSpeed;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.SetFloat(_hashMoveSpeed, 0f);
        }
    }

    private bool IsInDeathPresentation()
    {
        if (_deathPresentationActive)
            return true;

        if (_networkHealth != null && _networkHealth.IsSpawned && _networkHealth.IsUnconscious)
            return true;

        return healthComponent != null && healthComponent.IsDead;
    }

    /// <summary>Prefere o collider sólido (não-trigger) como referência; ignora hitboxes em trigger.</summary>
    private Collider2D ResolveBodyCollider()
    {
        var colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
                return colliders[i];
        }

        return GetComponent<Collider2D>();
    }

    private float ResolveSortingReferenceY()
    {
        if (IsInDeathPresentation() && _spriteRenderer != null)
            return _spriteRenderer.bounds.min.y;

        if (_collider2D != null && _collider2D.enabled)
            return _collider2D.bounds.min.y;

        if (_spriteRenderer != null)
            return _spriteRenderer.bounds.min.y;

        return transform.position.y;
    }

    private void UpdateSortingOrder()
    {
        if (_spriteRenderer == null)
            return;

        int newOrder = sortingOrderOffset - Mathf.RoundToInt((ResolveSortingReferenceY() + sortingReferenceYOffset) * sortingPrecision);
        if (IsInDeathPresentation())
            newOrder += deadSortingOrderBoost;

        if (!_loggedOnce)
        {
            _loggedOnce = true;
            Debug.Log($"[PlayerAnimationHandler] sortingOrder={newOrder}");
        }

        _spriteRenderer.sortingOrder = newOrder;
    }
}
