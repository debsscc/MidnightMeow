///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o drop de ciência quando o inimigo morre.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyDropHandler : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    /// <summary>
    /// Delegate opcional de spawn para contexto de rede.
    /// Assinatura: (prefab, position) => GameObject instanciado.
    /// Utilizado pelo NetworkWaveManager para spawnar drops como NetworkObjects.
    /// </summary>
    public System.Func<GameObject, Vector3, GameObject> SpawnDelegate;

    private HealthComponent _healthComponent;

    private void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        _healthComponent.OnDied.AddListener(HandleDrop);
    }

    private void OnDisable()
    {
        _healthComponent.OnDied.RemoveListener(HandleDrop);
    }

    private void HandleDrop()
    {
        if (stats.cienciaPrefab == null) return;

        float randomValue = Random.Range(0f, 1f);
        if (randomValue <= stats.dropChance)
        {
            int dropAmount = Random.Range(stats.minCienceDrop, stats.maxCienceDrop + 1);

            // Usa delegate de rede se disponível; caso contrário usa Instantiate padrão
            GameObject cienciaInstance = SpawnDelegate != null
                ? SpawnDelegate(stats.cienciaPrefab, transform.position)
                : Instantiate(stats.cienciaPrefab, transform.position, Quaternion.identity);

            if (cienciaInstance != null && cienciaInstance.TryGetComponent<Ciencia>(out Ciencia ciencia))
            {
                ciencia.SetValue(dropAmount);
            }
        }
    }
}
