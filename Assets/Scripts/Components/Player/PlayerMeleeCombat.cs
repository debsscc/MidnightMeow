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

    public event Action<Vector2, Vector2, MeleeCombatStats> OnAttackPerformed;

    public MeleeCombatStats CombatStats => combatStats;
    public Vector2 AttackOriginPosition => attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;

    private PlayerInputHandler _input;
    private PlayerAim _aim;
    private float _lastAttackTime = -999f;
    private bool _isAttacking;
    private Coroutine _attackRoutine;

    public bool IsAttacking => _isAttacking && _attackRoutine != null;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _aim = GetComponent<PlayerAim>();

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
        if (!pressed || combatStats == null || IsAttacking) return;

        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return;

        if (Time.time < _lastAttackTime + combatStats.attackCooldown) return;

        _attackRoutine = StartCoroutine(MeleeAttackRoutine());
    }

    private IEnumerator MeleeAttackRoutine()
    {
        _isAttacking = true;
        _lastAttackTime = Time.time;

        if (combatStats.windupDelay > 0f)
            yield return new WaitForSeconds(combatStats.windupDelay);

        if (!_aim.TryGetAimDirection(out Vector2 direction, out _))
            direction = Vector2.up;

        PerformMeleeHit(direction);
        CancelAttackState();
    }

    private void CancelAttackState()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _isAttacking = false;
    }

    private void PerformMeleeHit(Vector2 direction)
    {
        if (combatStats == null) return;

        Vector2 origin = AttackOriginPosition;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

        OnAttackPerformed?.Invoke(origin, direction, combatStats);

        float searchRadius = combatStats.attackRange + combatStats.farHalfWidth + 0.5f;
        var hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyLayers);
        var processed = new HashSet<int>();

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            int id = hit.GetInstanceID();
            if (!processed.Add(id)) continue;

            var damageable = hit.GetComponentInParent<HealthComponent>();
            if (damageable == null) continue;

            Vector2 targetPoint = hit.bounds.center;
            if (!MeleeHitUtility.IsInsideTrapezoid(
                    origin,
                    direction,
                    combatStats.attackRange,
                    combatStats.nearHalfWidth,
                    combatStats.farHalfWidth,
                    targetPoint))
                continue;

            var targetRoot = damageable.gameObject;
            ApplyHitToTarget(targetRoot, direction, targetPoint);

            if (drawDebugHits)
            {
                GameplayDiagnosticHub.EmitMelee(new MeleeHitDiagnostic(
                    name,
                    targetRoot.name,
                    combatStats.damage,
                    true,
                    "trapezoid hit"));
            }
        }

        GameplayDiagnosticHub.EmitMelee(new MeleeHitDiagnostic(
            name,
            "-",
            0f,
            false,
            $"swing origin={origin} dir={direction}"));
    }

    private void ApplyHitToTarget(GameObject target, Vector2 attackDirection, Vector2 hitPoint)
    {
        float damage = combatStats.damage;
        Vector2 knockDir = ((Vector2)target.transform.position - AttackOriginPosition).normalized;
        if (knockDir.sqrMagnitude < 0.0001f)
            knockDir = attackDirection;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsSpawned)
        {
            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            networkEnemy.TakeDamageRpc(damage, localId);
            networkEnemy.ApplyKnockbackRpc(knockDir, combatStats.knockbackDistance, combatStats.knockbackDuration);
        }
        else if (target.TryGetComponent<HealthComponent>(out var health))
        {
            health.TakeDamage(damage, gameObject);
            if (target.TryGetComponent<KnockbackReceiver>(out var knockback))
            {
                knockback.ApplyKnockback(knockDir, combatStats.knockbackForce, combatStats.knockbackDuration);
            }
        }

    }
}
