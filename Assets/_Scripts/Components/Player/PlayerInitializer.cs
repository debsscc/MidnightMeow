using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Base player stats asset (contains base values)")]
    [SerializeField] private PlayerStats baseStats;

    [Tooltip("Global progression data (ciência + upgrade levels)")]
    [SerializeField] private PlayerProgressionData progressionData;

    [Tooltip("Upgrade definitions in the same order as PlayerProgressionData.UpgradeType")]
    [SerializeField] private UpgradeDefinition[] upgradeDefinitions = new UpgradeDefinition[3];

    [Header("Player Components")]
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private PlayerShooting playerShooting;

    private void Awake()
    {
        // Auto-assign if not provided
        if (healthComponent == null)
            healthComponent = GetComponent<HealthComponent>();
        if (playerShooting == null)
            playerShooting = GetComponent<PlayerShooting>();

        // Try to resolve progressionData from ServiceLocator if unset
        if (progressionData == null && ServiceLocator.HasService<PlayerProgressionData>())
        {
            progressionData = ServiceLocator.GetService<PlayerProgressionData>();
        }
    }

    private void Start()
    {
        ApplyProgressionToPlayer();
        Debug.Log("PlayerInitializer: Applied progression to player at Start.\n" +
                  $"Health: {healthComponent?.MaxHealth}, FireRate: {playerShooting?.CurrentFireRate}, DamageMultiplier: {playerShooting?.DamageMultiplier}");
    }

    public void ApplyProgressionToPlayer()
    {
        // Health
        float baseHealth = baseStats != null ? baseStats.maxHealth : (healthComponent != null ? healthComponent.MaxHealth : 100f);
        int healthLevel = progressionData != null ? progressionData.GetLevel(PlayerProgressionData.UpgradeType.Health) : 0;
        float healthBonus = 0f;
        if (upgradeDefinitions != null && upgradeDefinitions.Length > (int)PlayerProgressionData.UpgradeType.Health && upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.Health] != null)
            healthBonus = upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.Health].GetBonusForLevel(healthLevel);

        float finalMaxHealth = baseHealth * (1f + healthBonus);
        if (healthComponent != null)
            healthComponent.Initialize(finalMaxHealth);

        // Fire rate
        int fireRateLevel = progressionData != null ? progressionData.GetLevel(PlayerProgressionData.UpgradeType.FireRate) : 0;
        float fireRateBonus = 0f;
        if (upgradeDefinitions != null && upgradeDefinitions.Length > (int)PlayerProgressionData.UpgradeType.FireRate && upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.FireRate] != null)
            fireRateBonus = upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.FireRate].GetBonusForLevel(fireRateLevel);

        if (playerShooting != null)
        {
            float baseRate = playerShooting.BaseFireRate;
            float finalRate = baseRate * (1f + fireRateBonus);
            playerShooting.SetFireRate(finalRate);
        }

        // Damage
        int damageLevel = progressionData != null ? progressionData.GetLevel(PlayerProgressionData.UpgradeType.Damage) : 0;
        float damageBonus = 0f;
        if (upgradeDefinitions != null && upgradeDefinitions.Length > (int)PlayerProgressionData.UpgradeType.Damage && upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.Damage] != null)
            damageBonus = upgradeDefinitions[(int)PlayerProgressionData.UpgradeType.Damage].GetBonusForLevel(damageLevel);

        if (playerShooting != null)
        {
            float damageMultiplier = 1f + damageBonus;
            playerShooting.SetDamageMultiplier(damageMultiplier);
        }
    }
}
