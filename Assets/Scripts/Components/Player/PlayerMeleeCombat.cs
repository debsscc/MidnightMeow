using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ataque corpo a corpo em trapézio na direção do mouse. Knockback autoritativo no servidor.
/// </summary>
[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerAim))]
public class PlayerMeleeCombat : MonoBehaviour
{
    [SerializeField] private MeleeCombatStats combatStats;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private bool drawDebugHits = true;

    public event Action OnMeleeAttackStarted;
    public event Action<Vector2, Vector2, MeleeCombatStats> OnAttackPerformed;
    public event Action<MeleeHitResult> OnMeleeHitsConfirmed;

    public MeleeCombatStats CombatStats => _runtimeCombatStats != null ? _runtimeCombatStats : combatStats;

    private MeleeCombatStats _runtimeCombatStats;

    public void ApplyRuntimeStats(MeleeCombatStats runtimeStats) => _runtimeCombatStats = runtimeStats;
    public Vector2 AttackOriginPosition => GetBodyPosition();

    private PlayerInputHandler _input;
    private PlayerAim _aim;
    private PlayerAbilityHandler _abilityHandler;
    private PlayerPassiveHandler _passiveHandler;
    private PlayerAnimationHandler _animationHandler;
    private Rigidbody2D _rb;
    private float _lastAttackTime = -999f;
    private bool _isAttacking;
    private Coroutine _attackRoutine;
    private bool _strikeTriggered;

    public bool IsAttacking => _isAttacking && _attackRoutine != null;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _aim = GetComponent<PlayerAim>();
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        _passiveHandler = GetComponent<PlayerPassiveHandler>();
        _animationHandler = GetComponent<PlayerAnimationHandler>();
        _rb = GetComponent<Rigidbody2D>();

        if (attackOrigin == null)
            attackOrigin = transform;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }

        if (combatStats != null && combatStats.drawDebugGizmos &&
            GetComponent<MeleeAttackDebugVisual>() == null)
            gameObject.AddComponent<MeleeAttackDebugVisual>();
    }

    private void OnEnable()
    {
        _input.OnFireInput += HandleFireInput;
    }

    private void OnDisable()
    {
        _input.OnFireInput -= HandleFireInput;
        CancelAttackState();
    }

    private void HandleFireInput(bool pressed)
    {
        if (!pressed || CombatStats == null || IsAttacking) return;

        if (_abilityHandler != null && !_abilityHandler.TryRequestPrimaryAttack())
            return;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return;

        if (Time.time < _lastAttackTime + CombatStats.attackCooldown) return;

        _attackRoutine = StartCoroutine(MeleeAttackRoutine());
    }

    /// <summary>Chamado por Animation Event no frame de impacto (fallback: deadline por SO).</summary>
    public void PerformStrike()
    {
        if (!_isAttacking)
            return;

        _strikeTriggered = true;
    }

    private IEnumerator MeleeAttackRoutine()
    {
        _isAttacking = true;
        _strikeTriggered = false;
        _lastAttackTime = Time.time;

        OnMeleeAttackStarted?.Invoke();

        float strikeDeadline = ResolveMeleeStrikeDelay();
        float elapsed = 0f;
        while (!_strikeTriggered && elapsed < strikeDeadline)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_aim.TryGetAimDirection(out Vector2 direction, out _))
            direction = Vector2.up;

        MeleeHitResult result = PerformMeleeHit(direction);
        OnMeleeHitsConfirmed?.Invoke(result);

        float recoveryDelay = ResolveMeleeRecoveryDelay();
        if (recoveryDelay > 0f)
            yield return new WaitForSeconds(recoveryDelay);

        CancelAttackState();
    }

    private float ResolveMeleeStrikeDelay()
    {
        if (_animationHandler != null)
            return _animationHandler.GetMeleeStrikeDelay();

        if (CombatStats == null)
            return 0.25f;

        return MeleeStrikeTimingUtility.ComputeStrikeDelay(CombatStats, 0.333f, 1f);
    }

    private float ResolveMeleeRecoveryDelay()
    {
        if (_animationHandler != null)
            return _animationHandler.GetMeleeRecoveryDelay();

        if (CombatStats == null)
            return 0f;

        return MeleeStrikeTimingUtility.ComputeRecoveryDelay(CombatStats, 0.333f, 1f);
    }

    private void CancelAttackState()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _isAttacking = false;
        _strikeTriggered = false;
    }

    private MeleeHitResult PerformMeleeHit(Vector2 direction)
    {
        if (CombatStats == null)
            return MeleeHitResult.Miss;

        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        Vector2 origin = GetSwingOrigin(direction);
        float areaMultiplier = _passiveHandler != null ? _passiveHandler.CleaveAreaMultiplier : 1f;
        float attackRange = CombatStats.attackRange * areaMultiplier;
        float nearHalfWidth = CombatStats.nearHalfWidth * areaMultiplier;
        float farHalfWidth = CombatStats.farHalfWidth * areaMultiplier;

        OnAttackPerformed?.Invoke(origin, direction, CombatStats);

        float searchRadius = attackRange + farHalfWidth + 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyLayers);
        HashSet<int> processed = new HashSet<int>();
        int maxTargets = _passiveHandler != null ? _passiveHandler.CleaveMaxTargets : 1;
        int hitCount = 0;

        List<Vector2> hitPoints = new List<Vector2>(maxTargets);
        List<GameObject> targets = new List<GameObject>(maxTargets);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            int id = hit.GetInstanceID();
            if (!processed.Add(id)) continue;

            HealthComponent damageable = hit.GetComponentInParent<HealthComponent>();
            if (damageable == null) continue;

            Vector2 targetPoint = hit.bounds.center;
            if (!MeleeHitUtility.IsInsideTrapezoid(
                    origin,
                    direction,
                    attackRange,
                    nearHalfWidth,
                    farHalfWidth,
                    targetPoint))
                continue;

            GameObject targetRoot = damageable.gameObject;
            ApplyHitToTarget(targetRoot, direction, origin, targetPoint);
            hitPoints.Add(targetPoint);
            targets.Add(targetRoot);
            hitCount++;
            if (hitCount >= maxTargets) break;

            if (drawDebugHits)
            {
                GameplayDiagnosticHub.EmitMelee(new MeleeHitDiagnostic(
                    name,
                    targetRoot.name,
                    CombatStats.damage,
                    true,
                    "trapezoid hit"));
            }
        }

        GameplayDiagnosticHub.EmitMelee(new MeleeHitDiagnostic(
            name,
            "-",
            0f,
            false,
            $"swing origin={origin} dir={direction} hits={hitCount}"));

        return new MeleeHitResult(hitCount, hitPoints.ToArray(), targets.ToArray());
    }

    private Vector2 GetBodyPosition()
    {
        if (_rb != null)
            return _rb.position;

        return transform.position;
    }

    private Vector2 GetSwingOrigin(Vector2 direction)
    {
        Vector2 origin = GetBodyPosition();
        float offset = CombatStats != null ? CombatStats.attackOriginForwardOffset : 0f;
        if (offset > 0f)
            origin += direction * offset;
        return origin;
    }

    private void ApplyHitToTarget(GameObject target, Vector2 attackDirection, Vector2 swingOrigin, Vector2 hitPoint)
    {
        float damage = CombatStats.damage;
        Vector2 knockDir = ((Vector2)target.transform.position - swingOrigin).normalized;
        if (knockDir.sqrMagnitude < 0.0001f)
            knockDir = attackDirection;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
        {
            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            networkEnemy.TakeDamageRpc(damage, localId, DamageType.Melee);
            networkEnemy.ApplyKnockbackRpc(knockDir, CombatStats.knockbackDistance, CombatStats.knockbackDuration);
        }
        else if (target.TryGetComponent<HealthComponent>(out var health))
        {
            health.TakeDamage(damage, gameObject, DamageType.Melee);
            if (target.TryGetComponent<KnockbackReceiver>(out var knockback))
            {
                knockback.ApplyKnockback(knockDir, CombatStats.knockbackForce, CombatStats.knockbackDuration);
            }
        }
    }
}
