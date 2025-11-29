///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Configura a vida do jogador com base em um ScriptableObject.
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

    private void Start()
    {
        // Injeta a vida definida no ScriptableObject
        _healthComponent.Initialize(stats.maxHealth);
    }
}