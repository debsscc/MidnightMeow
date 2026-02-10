// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-04 21:33
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;
    private bool _isDead = false;



    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnDied;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _isDead;
    public bool IsAlive => !_isDead;


    void Start()
    {
        // pooling, chama Initialize manualmente
        if (_currentHealth <= 0)
            Initialize(_maxHealth);
    }

    public void Initialize(float maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;
        _isDead = false;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(float amount, GameObject instigator)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        Debug.Log($"{gameObject.name} took {amount} damage from {instigator.name}. Current Health: {_currentHealth}/{_maxHealth}");
        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        OnDied?.Invoke();

        // Para prot�tipo, destr�i o objeto. 
        // Em produ��o: substituir por Pooling.
        Destroy(gameObject, 0.1f);
    }
}