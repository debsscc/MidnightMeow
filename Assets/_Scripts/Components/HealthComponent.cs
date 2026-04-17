// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-04 21:33
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;
    private bool _isDead = false;



    [Tooltip("Segundos até o GameObject ser destruído após a morte. Para o Player, use um valor maior que a animação de morte (ex: 5). Para inimigos, mantenha 0.1.")]
    [SerializeField] private float _destroyDelay = 0.1f;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    // Disparado quando o componente perde vida: (damageAmount, instigator GameObject)
    public UnityEvent OnTakeDamage;
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
        // Notifica listeners que tomou dano
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (gameObject.CompareTag("Player"))
            FollowCamera.Instance?.Shake();
        Debug.Log($"{gameObject.name} took {amount} damage from {instigator.name}. Current Health: {_currentHealth}/{_maxHealth}");

        //quando leva dano, faz o sprite piscar (SpriteBlink.cs)
        if (gameObject.TryGetComponent<SpriteBlink>(out var spriteBlink))
        {
            spriteBlink.Blink();
        }

        // Knockback opcional — só aplica se o componente existir no GameObject
        if (gameObject.TryGetComponent<KnockbackReceiver>(out var knockback))
        {
            knockback.ApplyKnockback(instigator);
        }

        if (_currentHealth <= 0f)
            Die();
        
    }

    /// <summary>Define por código o delay de destruição após a morte. Útil para o player, que precisa esperar a animação de morte.</summary>
    public void SetDestroyDelay(float delay) => _destroyDelay = Mathf.Max(0f, delay);

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDied?.Invoke();

        Destroy(gameObject, _destroyDelay);
    }
}