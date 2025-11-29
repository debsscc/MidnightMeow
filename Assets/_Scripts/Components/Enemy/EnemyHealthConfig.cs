///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Configura a vida do inimigo com base em um ScriptableObject.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyHealthConfig : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    private HealthComponent _healthComponent;

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _healthComponent.Initialize(stats.maxHealth);
    }
}