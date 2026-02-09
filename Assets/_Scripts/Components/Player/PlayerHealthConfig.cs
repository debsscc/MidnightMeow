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

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }
    private void OnEnable(){
        _healthComponent.OnHealthChanged.AddListener(_OnHealthChanged);
    }
    private void OnDisable() {
         _healthComponent.OnHealthChanged.RemoveListener(_OnHealthChanged);
    }
    private void Start()
    {
        // Injeta a vida definida no ScriptableObject
        _healthComponent.Initialize(stats.maxHealth);
    }
    
    private void _OnHealthChanged(float _currentHealth, float _maxHealth) {
        Debug.Log($"Health changed captured by player config: {_currentHealth}/{_maxHealth}");
        GameEvents.InvokePlayerHealthChanged(_currentHealth, _maxHealth);
    }
}