/// <summary>
/// Paralisa o inimigo por um tempo configurável em EnemyStats após tomar dano.
/// </summary>

using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyHitStun : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private float _stunEndTime;

    public bool IsStunned => Time.time < _stunEndTime;

    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnTakeDamage.AddListener(HandleTakeDamage);
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnTakeDamage.RemoveListener(HandleTakeDamage);
    }

    private void HandleTakeDamage()
    {
        ApplyStun();
    }

    public void ApplyStun()
    {
        if (stats == null || stats.hitStunDuration <= 0f) return;
        _stunEndTime = Time.time + stats.hitStunDuration;
    }
}
