using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Habilidade R da Nix — Investida: zona retangular de dano à frente.
/// </summary>
[DisallowMultipleComponent]
public class NixChargeAbilityExecutor : MonoBehaviour, IAbilityExecutor
{
    [Header("Combat")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackOrigin;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    public CharacterAbilityType AbilityType => CharacterAbilityType.NixCharge;

    private PlayerAbilityHandler _abilityHandler;

    private void Awake()
    {
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        if (attackOrigin == null)
            attackOrigin = transform;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }
    }

    public float Execute(AbilityTierData tierData, AbilityExecutionContext context)
    {
        Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)context.User.transform.position;
        Vector2 direction = context.AimDirection.sqrMagnitude > 0.0001f ? context.AimDirection.normalized : Vector2.up;

        float depth = tierData.range;
        float halfWidth = tierData.areaWidth * 0.5f;
        float searchRadius = depth + halfWidth + 0.5f;

        var hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyLayers);
        var processed = new HashSet<int>();

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            int id = hit.GetInstanceID();
            if (!processed.Add(id)) continue;

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

        return 0.25f;
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
