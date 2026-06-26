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
    [SerializeField] private AnimatorProfileBinder animationBinder;
    private int sortingOrderOffset = 5000;
    [SerializeField] private int sortingPrecision = 100;
    [Tooltip("Ajuste fino do Y de referência de profundidade (alinhe os pés do inimigo ao mesmo critério do player).")]
    [SerializeField] private float sortingReferenceYOffset = 0f;
    private SpriteRenderer _spriteRenderer;
    private EnemyAttack_Melee _attack;
    private EnemyAttack_Ranged _attackRanged;
    private EnemyTelegraphedAttacker _telegraphedAttacker;
    private HealthComponent healthComponent;
    private Collider2D _collider2D;
    [SerializeField] private bool isMelee; // Para determinar se o inimigo é corpo a corpo ou ranged, caso ambos os componentes existam.

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float _lastMoveSpeed = 0f;
    private const float SpeedEpsilon = 0.01f;
    private NetworkEnemyController _networkEnemyController;

    // Hashes
    private int _hashMoveSpeed;
    private int _hashOnAttack;
    private int _hashOnTakeDamage;
    private int _hashOnDie;
    private int _hashIsAttacking;

    private float _attackAnimEndTime = -1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = ResolveBodyCollider();
        _telegraphedAttacker = GetComponent<EnemyTelegraphedAttacker>();
        if (isMelee)
            _attack = GetComponent<EnemyAttack_Melee>();
        else
            _attackRanged = GetComponent<EnemyAttack_Ranged>();
        healthComponent = GetComponent<HealthComponent>();
        _networkEnemyController = GetComponent<NetworkEnemyController>();
        if (animationBinder == null)
            animationBinder = GetComponent<AnimatorProfileBinder>();

        ResolveAnimationHashes();

        if (debugLogs)
        {
            Debug.Log($"EnemyAnimationHandler.Awake - {gameObject.name}: animator={_animator!=null}, enemyMovement={enemyMovement!=null}, spriteRenderer={_spriteRenderer!=null}, healthComponent={healthComponent!=null}, isMelee={isMelee}");
        }
    }

    private void ResolveAnimationHashes()
    {
        if (animationBinder != null)
        {
            _hashMoveSpeed = animationBinder.GetMoveSpeedHash();
            _hashOnAttack = animationBinder.GetOnShootHash();
            _hashOnTakeDamage = animationBinder.GetOnTakeDamageHash();
            _hashOnDie = animationBinder.GetOnDieHash();
            _hashIsAttacking = animationBinder.GetIsAttackingHash();

            if (animationBinder.Profile != null)
            {
                sortingOrderOffset = animationBinder.Profile.sortingOrderOffset;
                sortingPrecision = animationBinder.Profile.sortingPrecision;
            }

            return;
        }

        _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
        _hashOnAttack = Animator.StringToHash("OnAttack");
        _hashOnTakeDamage = Animator.StringToHash("OnTakeDamage");
        _hashOnDie = Animator.StringToHash("OnDie");
        _hashIsAttacking = Animator.StringToHash("IsAttacking");
    }

    public bool IsAttackingForAnimator => ResolveIsAttacking();

    private void OnEnable()
    {
        if (_telegraphedAttacker != null && _telegraphedAttacker.HasActivePattern)
            _telegraphedAttacker.OnAttackWindup += HandleAttack;
        else if (isMelee && _attack != null)
            _attack.OnAttack += HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack += HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite += HandleFlipSprite;

        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.AddListener(HandleTakeDamageEvent);
            healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
        }
    }

    private void OnDisable()
    {
        if (_telegraphedAttacker != null)
            _telegraphedAttacker.OnAttackWindup -= HandleAttack;
        if (isMelee && _attack != null)
            _attack.OnAttack -= HandleAttack;
        else if (!isMelee && _attackRanged != null)
            _attackRanged.OnAttack -= HandleAttack;
        if (enemyMovement != null)
            enemyMovement.OnFlipSprite -= HandleFlipSprite;

        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.RemoveListener(HandleTakeDamageEvent);
            healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }
    }

    private void Update()
    {
        if (_animator == null) return;
        if (healthComponent != null && !healthComponent.IsAlive) return;
        if (_networkEnemyController != null && _networkEnemyController.DrivesAnimatorOnClient)
            return;

        float speed = enemyMovement != null ? enemyMovement.GetCurrentSpeed() : 0f;
        _animator.SetFloat(_hashMoveSpeed, speed);

        if (_hashIsAttacking != 0 && HasAnimatorBool(_hashIsAttacking))
            _animator.SetBool(_hashIsAttacking, ResolveIsAttacking());

        if (debugLogs && Mathf.Abs(speed - _lastMoveSpeed) > SpeedEpsilon)
        {
            Debug.Log($"EnemyAnimationHandler.Update - {gameObject.name}: MoveSpeed changed {_lastMoveSpeed} -> {speed}");
            _lastMoveSpeed = speed;
        }
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    private void HandleFlipSprite(bool facingRight)
    {
        if (_spriteRenderer != null)
        {
            // Sprites dos ratos foram desenhados olhando para a direita; flipX espelha para a esquerda.
            _spriteRenderer.flipX = !facingRight;
        }
    }

    private void HandleAttack()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleAttack - {gameObject.name}");
        _animator.SetTrigger(_hashOnAttack);

        float clipLength = animationBinder != null ? animationBinder.AttackAnimClipLength : 0.333f;
        _attackAnimEndTime = Time.time + clipLength;
    }

    private bool ResolveIsAttacking()
    {
        if (_telegraphedAttacker != null && _telegraphedAttacker.IsExecuting)
            return true;

        if (enemyMovement != null && enemyMovement.IsAttackPaused)
            return true;

        return Time.time < _attackAnimEndTime;
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

    private void HandleTakeDamage()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.HandleTakeDamage - {gameObject.name}");
        _animator.SetTrigger(_hashOnTakeDamage);
    }

    public void PlayDeathAnimation()
    {
        if (_animator == null) return;
        if (debugLogs) Debug.Log($"EnemyAnimationHandler.PlayDeathAnimation - {gameObject.name}");
        _animator.SetTrigger(_hashOnDie);
    }

    public void PlayTakeDamageAnimation()
    {
        if (_animator == null) return;
        _animator.SetTrigger(_hashOnTakeDamage);
    }

    private void HandleTakeDamageEvent()
    {
        if (_animator == null)
            return;

        PlayTakeDamageAnimation();
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (debugLogs)
            Debug.Log($"EnemyAnimationHandler.HandleHealthChanged - {gameObject.name}: current={current}, max={max}");
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

    private void UpdateSortingOrder()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        float referenceY = _collider2D != null && _collider2D.enabled
            ? _collider2D.bounds.min.y
            : _spriteRenderer.bounds.min.y;
        referenceY += sortingReferenceYOffset;
        _spriteRenderer.sortingOrder = sortingOrderOffset - Mathf.RoundToInt(referenceY * sortingPrecision);
    }
}
