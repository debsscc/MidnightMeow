using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Poça de dano contínuo da Cora (Investida R).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class CoraDamagePool : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Multiplicador visual da poça (1 = tamanho do raio da habilidade).")]
    [SerializeField] private float visualScaleMultiplier = 0.8f;

    private AbilityTierData _tierData;
    private ulong _ownerClientId;
    private float _worldRadius;
    private CircleCollider2D _trigger;
    private readonly HashSet<int> _enemiesInside = new HashSet<int>();
    private Coroutine _damageRoutine;
    private Coroutine _lifetimeRoutine;

    private void Awake()
    {
        _trigger = GetComponent<CircleCollider2D>();
        _trigger.isTrigger = true;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }
    }

    public void Initialize(AbilityTierData tierData, ulong ownerClientId)
    {
        _tierData = tierData;
        _ownerClientId = ownerClientId;
        _worldRadius = Mathf.Max(0.25f, tierData.range);

        SyncPoolVisualAndCollider(_worldRadius);

        bool isNetworkedInstance = TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned;
        bool shouldRunAuthoritativeLogic = !isNetworkedInstance || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
        if (!shouldRunAuthoritativeLogic)
            return;

        if (_damageRoutine != null)
            StopCoroutine(_damageRoutine);
        _damageRoutine = StartCoroutine(DamageTickRoutine());

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
        if (tierData.effectDuration > 0f)
            _lifetimeRoutine = StartCoroutine(LifetimeRoutine(tierData.effectDuration));
    }

    private void SyncPoolVisualAndCollider(float worldRadius)
    {
        transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float spriteDiameter = 1f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 bounds = spriteRenderer.sprite.bounds.size;
            spriteDiameter = Mathf.Max(bounds.x, bounds.y);
        }

        float targetDiameter = worldRadius * 2f * Mathf.Max(0.05f, visualScaleMultiplier);
        float uniformScale = targetDiameter / Mathf.Max(0.01f, spriteDiameter);
        transform.localScale = Vector3.one * uniformScale;

        if (_trigger != null)
            _trigger.radius = worldRadius / uniformScale;
    }

    private IEnumerator DamageTickRoutine()
    {
        float interval = Mathf.Max(0.1f, tickInterval);
        while (true)
        {
            ApplyDamageToOccupants();
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                netObj.Despawn(true);
            yield break;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsEnemy(other)) return;
        _enemiesInside.Add(other.GetInstanceID());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        _enemiesInside.Remove(other.GetInstanceID());
    }

    private void ApplyDamageToOccupants()
    {
        float damage = _tierData.damagePerSecond > 0f
            ? _tierData.damagePerSecond * tickInterval
            : _tierData.damage;

        if (damage <= 0f) return;

        float radius = _worldRadius > 0f ? _worldRadius : _tierData.range;
        var hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive) continue;

            EnemyCombatUtility.ApplyDamage(health.gameObject, damage, _ownerClientId, gameObject, DamageType.Ranged);
        }
    }

    private bool IsEnemy(Collider2D other)
    {
        return enemyLayers.value == 0 || ((1 << other.gameObject.layer) & enemyLayers.value) != 0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        CircleCollider2D collider = _trigger != null ? _trigger : GetComponent<CircleCollider2D>();
        if (collider == null)
            return;

        Bounds bounds = collider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
        Vector3 center = bounds.center;

        Gizmos.color = new Color(0.75f, 0.2f, 0.95f, 0.25f);
        Gizmos.DrawSphere(center, radius * 0.15f);

        const int segments = 32;
        Vector3 previous = center + Vector3.right * radius;
        Gizmos.color = new Color(0.9f, 0.5f, 1f, 0.95f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
#endif
}
