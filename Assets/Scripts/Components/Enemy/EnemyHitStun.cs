/// <summary>
/// Paralisa o inimigo por um tempo configurável (hit-stun curto em dano ou stun de combate).
/// O timer roda localmente; em MP o servidor é a autoridade e NetworkEnemyController sincroniza o estado.
/// </summary>

using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyHitStun : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private float _stunEndTime;

    public bool IsStunned => Time.time < _stunEndTime;

    public float StunTimeRemaining => Mathf.Max(0f, _stunEndTime - Time.time);

    public event Action<float> OnStunApplied;
    public event Action OnStunEnded;

    private HealthComponent _health;
    private bool _wasStunned;

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

    private void Update()
    {
        bool stunned = IsStunned;
        if (_wasStunned && !stunned)
            OnStunEnded?.Invoke();
        _wasStunned = stunned;
    }

    private void HandleTakeDamage()
    {
        ApplyStun();
    }

    public void ApplyStun()
    {
        ApplyStun(stats != null ? stats.hitStunDuration : 0f);
    }

    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);
        _wasStunned = true;
        OnStunApplied?.Invoke(duration);
    }
}
