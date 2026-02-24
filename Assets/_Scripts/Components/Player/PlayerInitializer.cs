using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Base player stats asset")]
    [SerializeField] private PlayerStats baseStats;
    [SerializeField] private PlayerProgressionData progressionData;
    [SerializeField] private UpgradeDefinition[] upgradeDefinitions = new UpgradeDefinition[3];

    [Header("Player Components")]
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private PlayerShooting playerShooting;

    private void Awake()
    {
        if (healthComponent == null) healthComponent = GetComponent<HealthComponent>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();

        if (progressionData == null && ServiceLocator.HasService<PlayerProgressionData>())
        {
            progressionData = ServiceLocator.GetService<PlayerProgressionData>();
        }
    }

    private void OnEnable()
    {
        // Absorvendo as responsabilidades de eventos do antigo PlayerHealthConfig
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
            healthComponent.OnDied.AddListener(HandlePlayerDied);
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
            healthComponent.OnDied.RemoveListener(HandlePlayerDied);
        }
    }

    private void Start()
    {
        ApplyProgressionToPlayer();
    }

    private void ApplyProgressionToPlayer()
    {
        // Helper para buscar o bônus seguro
        float GetBonus(PlayerProgressionData.UpgradeType type)
        {
            int level = progressionData != null ? progressionData.GetLevel(type) : 0;
            int idx = (int)type;
            if (upgradeDefinitions != null && idx < upgradeDefinitions.Length && upgradeDefinitions[idx] != null)
                return upgradeDefinitions[idx].GetBonusForLevel(level);
            return 0f;
        }

        // 1. Injeção de Vida (Multiplicador Percentual)
        float healthBonus = GetBonus(PlayerProgressionData.UpgradeType.Health);
        float baseHealth = baseStats != null ? baseStats.maxHealth : (healthComponent != null ? healthComponent.MaxHealth : 100f);
        float finalMaxHealth = baseHealth * (1f + healthBonus);
        
        if (healthComponent != null)
            healthComponent.Initialize(finalMaxHealth);

        // 2. Injeção de Cadência de Tiro (Multiplicador Percentual)
        float fireRateBonus = GetBonus(PlayerProgressionData.UpgradeType.FireRate);
        if (playerShooting != null)
        {
            float finalRate = playerShooting.BaseFireRate * (1f + fireRateBonus);
            playerShooting.SetFireRate(finalRate);
        }

        // 3. Injeção de Dano (Multiplicador Percentual)
        float damageBonus = GetBonus(PlayerProgressionData.UpgradeType.Damage);
        if (playerShooting != null)
        {
            // O dano base 1 é multiplicado pelas configurações do projétil mais tarde.
            float damageMultiplier = 1f + damageBonus; 
            playerShooting.SetDamageMultiplier(damageMultiplier);
        }

        Debug.Log($"PlayerInitializer: Status -> Vida: {finalMaxHealth}, Cadência: {playerShooting?.CurrentFireRate}, DanoMult: {playerShooting?.DamageMultiplier}");
    }

    // --- Event Handlers (Substituem o PlayerHealthConfig) ---
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        GameEvents.InvokePlayerHealthChanged(currentHealth, maxHealth);
    }

    private void HandlePlayerDied()
    {
        GameEvents.InvokePlayerDefeated();
    }
}