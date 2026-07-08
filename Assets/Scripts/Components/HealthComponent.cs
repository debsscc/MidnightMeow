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

    [Tooltip("Inimigos em rede devem desativar destruição local e usar NetworkObject.Despawn.")]
    [SerializeField] private bool _allowDestroyOnDeath = true;

    private float _invulnerableUntil;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    // Disparado quando o componente perde vida: (damageAmount, instigator GameObject)
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDied;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _isDead;
    public bool IsAlive => !_isDead;


    void Awake()
    {
        if (gameObject.CompareTag("Enemy") && GetComponent<EnemyHealthBarDisplay>() == null)
            gameObject.AddComponent<EnemyHealthBarDisplay>();
    }

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

    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    public void SetInvulnerableFor(float seconds)
    {
        if (seconds <= 0f) return;
        _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
    }

    public void TakeDamage(float amount, GameObject instigator)
    {
        TakeDamage(amount, instigator, DamageType.Generic);
    }

    public void TakeDamage(float amount, GameObject instigator, DamageType damageType)
    {
        if (_isDead || amount <= 0f || IsInvulnerable) return;

        if (TryGetComponent<CarriageDamageFilter>(out var carriageFilter)
            && !carriageFilter.CanAcceptDamage(instigator, damageType))
            return;

        if (gameObject.CompareTag("Player") && IsPlayerDashing(gameObject))
            return;

        amount = DamageDefenseUtility.ApplyDefense(amount, damageType, DamageDefenseUtility.ResolveEnemyStats(gameObject));
        if (amount <= 0f) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        // Notifica listeners que tomou dano
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (gameObject.CompareTag("Player") && ShouldShakeCameraForPlayerDamage())
            PlayerCameraFeedback.ShakeOnLocalPlayerDamage();

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

        if (ShouldEmitLocalDamageIndicator())
            GameEvents.InvokeDamageShown(amount, transform.position + Vector3.up * 0.5f);

        if (_currentHealth <= 0f)
            Die();
    }

    /// <summary>Define por código o delay de destruição após a morte. Útil para o player, que precisa esperar a animação de morte.</summary>
    public void SetDestroyDelay(float delay) => _destroyDelay = Mathf.Max(0f, delay);

    public void SetAllowDestroyOnDeath(bool allow) => _allowDestroyOnDeath = allow;

    private bool ShouldEmitLocalDamageIndicator()
    {
        if (TryGetComponent<NetworkEnemyController>(out var enemy) && enemy.IsSpawned)
            return false;
        if (TryGetComponent<NetworkPlayerHealth>(out var player) && player.IsSpawned)
            return false;
        return true;
    }

    /// <summary>
    /// Dano em rede replica shake no ClientRpc do owner; offline/legado usa este caminho.
    /// </summary>
    private bool ShouldShakeCameraForPlayerDamage()
    {
        if (TryGetComponent<NetworkPlayerHealth>(out var networkHealth) && networkHealth.IsSpawned)
            return false;

        return true;
    }

    /// <summary>
    /// Espelha estado de vida em clientes (inimigos em rede) sem aplicar dano/knockback.
    /// </summary>
    public void ApplyNetworkMirror(float current, float max, bool isDead)
    {
        _maxHealth = max;
        if (isDead)
            current = 0f;

        _currentHealth = Mathf.Clamp(current, 0f, max);
        _isDead = isDead;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private static bool IsPlayerDashing(GameObject player)
    {
        if (player == null)
            return false;

        if (player.TryGetComponent<PlayerDash>(out var dash) && dash.IsDashing)
            return true;

        if (player.TryGetComponent<NetworkPlayerAbilityRelay>(out var relay) && relay.NetworkIsDashing)
            return true;

        return false;
    }

    private void Die()
    {
        if (_isDead) return;

        _currentHealth = 0f;
        _isDead = true;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnDied?.Invoke();

        if (gameObject.CompareTag("Enemy") && !TryGetComponent<NetworkEnemyController>(out _))
            GameEvents.InvokeEnemyKilledByPlayer(0);

        if (_allowDestroyOnDeath)
            Destroy(gameObject, _destroyDelay);
    }
}