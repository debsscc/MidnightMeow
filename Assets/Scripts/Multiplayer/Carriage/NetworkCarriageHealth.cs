using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Vida da carruagem replicada pelo servidor. Integra <see cref="HealthComponent"/> + barra de inimigo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent), typeof(CarriageDamageFilter))]
public class NetworkCarriageHealth : NetworkBehaviour
{
    private HealthComponent _health;

    private readonly NetworkVariable<float> _syncHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _syncMaxHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isBroken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsBroken => _isBroken.Value;
    public float RepairProgress => GetComponent<NetworkCarriageRepairManager>()?.RepairProgress ?? 0f;
    public bool IsRepairActive => GetComponent<NetworkCarriageRepairManager>()?.RepairActive ?? false;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _health.SetAllowDestroyOnDeath(false);

        if (GetComponent<CarriageDamageFilter>() == null)
            gameObject.AddComponent<CarriageDamageFilter>();
    }

    public void SetAllowDestroyOnDeath(bool allow) => _health.SetAllowDestroyOnDeath(allow);

    public override void OnNetworkSpawn()
    {
        _syncHealth.OnValueChanged += HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged += HandleSyncedMaxHealthChanged;
        _isBroken.OnValueChanged += HandleBrokenChanged;

        _health.OnHealthChanged.AddListener(HandleHealthChanged);
        _health.OnDied.AddListener(HandleDied);

        ApplySyncedHealthToComponent();
    }

    public override void OnNetworkDespawn()
    {
        _syncHealth.OnValueChanged -= HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged -= HandleSyncedMaxHealthChanged;
        _isBroken.OnValueChanged -= HandleBrokenChanged;

        if (_health != null)
        {
            _health.OnHealthChanged.RemoveListener(HandleHealthChanged);
            _health.OnDied.RemoveListener(HandleDied);
        }

        base.OnNetworkDespawn();
    }

    public void ServerInitialize(float maxHealth)
    {
        if (!IsServer)
            return;

        _health.Initialize(maxHealth);
        _isBroken.Value = false;
        PublishHealthToNetwork();

        CarriageController carriage = GetComponent<CarriageController>();
        carriage?.ServerNotifyRepaired();
    }

    public void ServerRestoreAfterRepair(float healthAmount)
    {
        if (!IsServer)
            return;

        _isBroken.Value = false;
        _health.Initialize(healthAmount);
        PublishHealthToNetwork();

        CarriageController carriage = GetComponent<CarriageController>();
        carriage?.ServerNotifyRepaired();
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (IsServer)
            PublishHealthToNetwork();
    }

    private void HandleDied()
    {
        if (!IsServer)
            return;

        _isBroken.Value = true;
        PublishHealthToNetwork();

        CarriageController carriage = GetComponent<CarriageController>();
        carriage?.ServerNotifyBroken();
    }

    private void HandleSyncedHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void HandleSyncedMaxHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void HandleBrokenChanged(bool previous, bool current) => ApplySyncedHealthToComponent();

    private void ApplySyncedHealthToComponent()
    {
        if (_health == null || _syncMaxHealth.Value <= 0f)
            return;

        bool isDead = _isBroken.Value;
        _health.ApplyNetworkMirror(_syncHealth.Value, _syncMaxHealth.Value, isDead);
    }

    private void PublishHealthToNetwork()
    {
        if (_health == null)
            return;

        _syncHealth.Value = _health.CurrentHealth;
        _syncMaxHealth.Value = _health.MaxHealth;
    }
}

/// <summary>
/// Restringe dano da carruagem a ataques/projéteis de inimigos (ignora jogadores).
/// Consultado por <see cref="HealthComponent.TakeDamage"/> via TryGetComponent — mesmo padrão de
/// <see cref="PlayerDamageImmunity"/> no jogador.
/// </summary>
[DisallowMultipleComponent]
public class CarriageDamageFilter : MonoBehaviour
{
    public bool CanAcceptDamage(GameObject instigator, DamageType damageType)
    {
        if (instigator == null)
            return false;

        if (IsEnemyInstigator(instigator))
            return true;

        return false;
    }

    private static bool IsEnemyInstigator(GameObject instigator)
    {
        if (instigator.CompareTag("Enemy"))
            return true;

        if (instigator.GetComponentInParent<NetworkEnemyController>() != null)
            return true;

        if (instigator.GetComponent<EnemyProjectile>() != null)
            return true;

        if (instigator.GetComponentInParent<EnemyProjectile>() != null)
            return true;

        return false;
    }
}
