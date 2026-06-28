///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o drop de ciência quando o inimigo morre.
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyDropHandler : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public float DeathDespawnDelay => stats != null ? stats.deathDespawnDelay : 0.4f;

    /// <summary>
    /// Delegate opcional de spawn para contexto de rede.
    /// Assinatura: (prefab, position) => GameObject instanciado.
    /// Utilizado pelo NetworkWaveManager para spawnar drops como NetworkObjects.
    /// </summary>
    public System.Func<GameObject, Vector3, int, GameObject> SpawnDelegate;

    private HealthComponent _healthComponent;
    private bool _dropProcessed;

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

    private void HandleDrop() => TrySpawnDrop();

    /// <summary>Idempotente — usado pelo NetworkEnemyController no servidor.</summary>
    public void TrySpawnDrop()
    {
        if (_dropProcessed || stats == null || stats.cienciaPrefab == null) return;

        NetworkManager net = NetworkManager.Singleton;
        if (net != null && net.IsListening && !net.IsServer)
            return;

        if (net != null && net.IsServer && GetComponent<NetworkObject>() is { IsSpawned: true } && SpawnDelegate == null)
            return;

        float randomValue = Random.Range(0f, 1f);
        if (randomValue > stats.dropChance) return;

        _dropProcessed = true;
        int dropAmount = Random.Range(stats.minCienceDrop, stats.maxCienceDrop + 1);

        GameObject cienciaInstance = SpawnDelegate != null
            ? SpawnDelegate(stats.cienciaPrefab, transform.position, dropAmount)
            : Instantiate(stats.cienciaPrefab, transform.position, Quaternion.identity);

        if (SpawnDelegate == null && cienciaInstance != null &&
            cienciaInstance.TryGetComponent<Ciencia>(out Ciencia ciencia))
            ciencia.SetValue(dropAmount);
    }
}
