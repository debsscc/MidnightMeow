///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Configura a vida do jogador com base em um ScriptableObject.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class PlayerHealthConfig : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    private HealthComponent _healthComponent;
    private float? _initialHealthOverride = null;

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }
    private void OnEnable(){
        _healthComponent.OnHealthChanged.AddListener(_OnHealthChanged);
           _healthComponent.OnDied.AddListener(_OnDied);
    }
    private void OnDisable() {
         _healthComponent.OnHealthChanged.RemoveListener(_OnHealthChanged);
            _healthComponent.OnDied.RemoveListener(_OnDied);
    }
    private void Start()
    {
        // Injeta a vida definida no ScriptableObject (pode ser sobrescrita por upgrades)
        float initial = _initialHealthOverride.HasValue ? _initialHealthOverride.Value : stats.maxHealth;
        _healthComponent.Initialize(initial);
    }
    
    private void _OnHealthChanged(float _currentHealth, float _maxHealth) {
        Debug.Log($"Health changed captured by player config: {_currentHealth}/{_maxHealth}");
        GameEvents.InvokePlayerHealthChanged(_currentHealth, _maxHealth);
    }

    private void _OnDied()
    {
        Debug.Log("PlayerHealthConfig: player died, invoking GameEvents");
        GameEvents.InvokePlayerDefeated();
    }

    // Called by external systems (eg. PlayerUpgradesHandler) to override initial max health
    public void SetInitialHealthOverride(float maxHealth)
    {
        _initialHealthOverride = maxHealth;
    }
}