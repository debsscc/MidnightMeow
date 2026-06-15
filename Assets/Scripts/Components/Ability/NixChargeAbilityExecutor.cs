using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Habilidade R da Nix — Investida: avança à frente e causa dano aos inimigos no trajeto.
/// </summary>
[DisallowMultipleComponent]
public class NixChargeAbilityExecutor : MonoBehaviour, IAbilityExecutor
{
    [Header("Combat")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float chargeSpeed = 14f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    public CharacterAbilityType AbilityType => CharacterAbilityType.NixCharge;

    public bool IsCharging { get; private set; }

    public Vector2 ActiveChargeDirection { get; private set; } = Vector2.up;

    private PlayerAbilityHandler _abilityHandler;
    private Rigidbody2D _rb;
    private NetworkObject _networkObject;
    private NetworkPlayerAbilityRelay _abilityRelay;

    private void Awake()
    {
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        _rb = GetComponent<Rigidbody2D>();
        _networkObject = GetComponent<NetworkObject>();
        _abilityRelay = GetComponent<NetworkPlayerAbilityRelay>();
        if (attackOrigin == null)
            attackOrigin = transform;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }
    }

    public void ApplyEnemyLayers(LayerMask layers)
    {
        if (layers.value != 0)
            enemyLayers = layers;
    }

    public float Execute(AbilityTierData tierData, AbilityExecutionContext context)
    {
        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return 0.25f;

        StartCoroutine(ChargeRoutine(tierData, context));
        float duration = tierData.range / Mathf.Max(1f, chargeSpeed);
        return Mathf.Max(0.25f, duration);
    }

    private IEnumerator ChargeRoutine(AbilityTierData tierData, AbilityExecutionContext context)
    {
        if (_rb == null)
            yield break;

        Vector2 direction = context.AimDirection.sqrMagnitude > 0.0001f
            ? context.AimDirection.normalized
            : Vector2.up;
        ActiveChargeDirection = direction;

        float distance = tierData.range;
        float halfWidth = tierData.areaWidth * 0.5f;
        float speed = Mathf.Max(1f, chargeSpeed);
        float traveled = 0f;
        Vector2 chargeOrigin = _rb.position;

        _abilityRelay?.ResetChargeSession();
        IsCharging = true;

        try
        {
            while (traveled < distance)
            {
                float step = speed * Time.fixedDeltaTime;
                if (traveled + step > distance)
                    step = distance - traveled;

                traveled += step;
                Vector2 nextPos = chargeOrigin + direction * traveled;
                _rb.linearVelocity = Vector2.zero;
                _rb.MovePosition(nextPos);

                ApplyChargeDamage(chargeOrigin, direction, traveled, halfWidth, tierData, context);

                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            IsCharging = false;
            ActiveChargeDirection = Vector2.up;
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }
    }

    private void ApplyChargeDamage(
        Vector2 origin,
        Vector2 direction,
        float depth,
        float halfWidth,
        AbilityTierData tierData,
        AbilityExecutionContext context)
    {
        if (_abilityRelay != null && _networkObject != null && _networkObject.IsSpawned)
        {
            _abilityRelay.ReportChargeDamageFrame(
                origin,
                direction,
                depth,
                halfWidth,
                tierData.damage,
                context.OwnerClientId);
            return;
        }

        float searchRadius = depth + halfWidth + 0.5f;
        var hits = Physics2D.OverlapCircleAll(_rb.position, searchRadius, enemyLayers);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var damageable = hit.GetComponentInParent<HealthComponent>();
            if (damageable == null || !damageable.IsAlive) continue;

            Vector2 targetPoint = hit.bounds.center;
            if (!RectHitUtility.IsInsideOrientedRect(origin, direction, depth, halfWidth, targetPoint))
                continue;

            EnemyCombatUtility.ApplyDamage(
                damageable.gameObject,
                tierData.damage,
                context.OwnerClientId,
                context.User);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        var definition = _abilityHandler != null ? _abilityHandler.AbilitySet?.ability2 : null;
        if (definition == null || definition.abilityType != CharacterAbilityType.NixCharge) return;

        Vector2 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector2 forward = Vector2.up;
        if (TryGetComponent<PlayerAim>(out var aim) && aim.TryGetAimDirection(out Vector2 dir, out _))
            forward = dir;

        var host = GetComponent<AbilityDebugVisualHost>();
        if (host != null)
            host.DrawPreviewGizmo(CharacterAbilityType.NixCharge, origin, forward, definition.tier1);
        else
            AbilityDebugGizmoUtility.DrawOrientedRect(origin, forward, definition.tier1.range,
                definition.tier1.areaWidth * 0.5f,
                new Color(1f, 0.45f, 0.1f, 0.25f), new Color(1f, 0.75f, 0.2f, 0.9f));
    }
}

