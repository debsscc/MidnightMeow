using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Poça de dano contínuo da Cora (Investida R).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class CoraDamagePool : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float tickInterval = 0.5f;

    private AbilityTierData _tierData;
    private ulong _ownerClientId;
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

        const float referenceRange = 4f;
        const float basePrefabScale = 2.42f;
        float scaleMultiplier = Mathf.Max(0.5f, tierData.range / referenceRange);
        transform.localScale = Vector3.one * (basePrefabScale * scaleMultiplier);

        if (_trigger != null)
            _trigger.radius = tierData.range / (basePrefabScale * scaleMultiplier);

        if (_damageRoutine != null)
            StopCoroutine(_damageRoutine);
        _damageRoutine = StartCoroutine(DamageTickRoutine());

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
        if (tierData.effectDuration > 0f)
            _lifetimeRoutine = StartCoroutine(LifetimeRoutine(tierData.effectDuration));
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

        var hits = Physics2D.OverlapCircleAll(transform.position, _tierData.range, enemyLayers);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive) continue;

            EnemyCombatUtility.ApplyDamage(health.gameObject, damage, _ownerClientId, gameObject);
        }
    }

    private bool IsEnemy(Collider2D other)
    {
        return enemyLayers.value == 0 || ((1 << other.gameObject.layer) & enemyLayers.value) != 0;
    }
}
