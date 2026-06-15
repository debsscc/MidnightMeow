using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Habilidade Q da Nix — Empurrão com escudo: knockback + slow em área circular.
/// </summary>
[DisallowMultipleComponent]
public class NixPushAbilityExecutor : MonoBehaviour, IAbilityExecutor
{
    [Header("Combat")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackOrigin;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    public CharacterAbilityType AbilityType => CharacterAbilityType.NixPush;

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

    public void ApplyEnemyLayers(LayerMask layers)
    {
        if (layers.value != 0)
            enemyLayers = layers;
    }

    public float Execute(AbilityTierData tierData, AbilityExecutionContext context)
    {
        Vector2 nixPosition = context.User != null
            ? (Vector2)context.User.transform.position
            : attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 direction = context.AimDirection.sqrMagnitude > 0.0001f ? context.AimDirection.normalized : Vector2.up;
        const float forwardOffset = 1.15f;
        Vector2 origin = nixPosition + direction * forwardOffset;
        float radius = tierData.range * 1.35f;

        var hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayers);
        var processed = new HashSet<int>();

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            int id = hit.GetInstanceID();
            if (!processed.Add(id)) continue;

            var damageable = hit.GetComponentInParent<HealthComponent>();
            if (damageable == null || !damageable.IsAlive) continue;

            var targetRoot = damageable.gameObject;
            Vector2 knockDir = ((Vector2)targetRoot.transform.position - nixPosition).normalized;
            if (knockDir.sqrMagnitude < 0.0001f)
                knockDir = -direction;

            EnemyCombatUtility.ApplyDamage(targetRoot, tierData.damage, context.OwnerClientId, context.User);
            EnemyCombatUtility.ApplyKnockback(targetRoot, knockDir, tierData.knockbackDistance, tierData.knockbackDuration);
            EnemyCombatUtility.ApplySlow(targetRoot, tierData.slowMultiplier, tierData.slowDuration);
        }

        return tierData.knockbackDuration;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        var definition = _abilityHandler != null ? _abilityHandler.AbilitySet?.ability1 : null;
        if (definition == null || definition.abilityType != CharacterAbilityType.NixPush) return;

        Vector2 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector2 forward = Vector2.up;
        if (TryGetComponent<PlayerAim>(out var aim) && aim.TryGetAimDirection(out Vector2 dir, out _))
            forward = dir;

        var host = GetComponent<AbilityDebugVisualHost>();
        if (host != null)
            host.DrawPreviewGizmo(CharacterAbilityType.NixPush, origin, forward, definition.tier1);
        else
            AbilityDebugGizmoUtility.DrawCircle(origin, definition.tier1.range,
                new Color(0.2f, 0.55f, 1f, 0.25f), new Color(0.4f, 0.8f, 1f, 0.9f));
    }
}
