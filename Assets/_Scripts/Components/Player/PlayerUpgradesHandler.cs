using UnityEngine;

[RequireComponent(typeof(PlayerShooting))]
public class PlayerUpgradesHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player stats ScriptableObject containing selected upgrades")]
    [SerializeField] private PlayerStats playerStats;
    [Tooltip("Optional: Health config component on the player (will be auto-assigned if empty)")]
    [SerializeField] private PlayerHealthConfig healthConfig;
    [Tooltip("Player shooting component (will be auto-assigned if empty)")]
    [SerializeField] private PlayerShooting shooting;

    [Header("Multipliers per level")]
    [Tooltip("Percentage increase of max health per upgrade level (0.2 = +20% per level)")]
    [SerializeField] private float healthPerLevelMultiplier = 0.2f;
    [Tooltip("Percentage increase of fire rate per upgrade level (0.25 = +25% shots/sec per level)")]
    [SerializeField] private float fireRatePerLevelMultiplier = 0.25f;
    [Tooltip("Percentage increase of damage per upgrade level (0.25 = +25% damage per level)")]
    [SerializeField] private float damagePerLevelMultiplier = 0.25f;

    private void Awake()
    {
        if (shooting == null)
            shooting = GetComponent<PlayerShooting>();
        if (healthConfig == null)
            healthConfig = GetComponent<PlayerHealthConfig>();
    }

    private void OnEnable()
    {
        ApplyUpgrades();
    }

    private void ApplyUpgrades()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerUpgradesHandler: playerStats SO not assigned. Skipping upgrades.");
            return;
        }

        // Health
        if (healthConfig != null)
        {
            float newMax = playerStats.maxHealth * (1f + playerStats.healthUpgradeLevel * healthPerLevelMultiplier);
            healthConfig.SetInitialHealthOverride(newMax);
        }
        else
        {
            Debug.Log("PlayerUpgradesHandler: healthConfig not assigned; health upgrade not applied.");
        }

        // Fire rate and damage
        if (shooting != null)
        {
            float baseRate = shooting.BaseFireRate;
            float newRate = baseRate * (1f + playerStats.fireRateUpgradeLevel * fireRatePerLevelMultiplier);
            shooting.SetFireRate(newRate);

            float damageMultiplier = 1f + playerStats.damageUpgradeLevel * damagePerLevelMultiplier;
            shooting.SetDamageMultiplier(damageMultiplier);
        }
        else
        {
            Debug.Log("PlayerUpgradesHandler: shooting component not found; fire rate and damage upgrades not applied.");
        }
    }

    // Public API to change upgrades at runtime. Levels are clamped between 0 and 3.
    public void SetDamageUpgradeLevel(int level)
    {
        if (playerStats == null) return;
        playerStats.damageUpgradeLevel = Mathf.Clamp(level, 0, 3);
        ApplyUpgrades();
    }

    public void SetFireRateUpgradeLevel(int level)
    {
        if (playerStats == null) return;
        playerStats.fireRateUpgradeLevel = Mathf.Clamp(level, 0, 3);
        ApplyUpgrades();
    }

    public void SetHealthUpgradeLevel(int level)
    {
        if (playerStats == null) return;
        playerStats.healthUpgradeLevel = Mathf.Clamp(level, 0, 3);
        ApplyUpgrades();
    }

    // Convenience methods
    public void IncreaseDamageUpgrade() => SetDamageUpgradeLevel(playerStats != null ? playerStats.damageUpgradeLevel + 1 : 0);
    public void IncreaseFireRateUpgrade() => SetFireRateUpgradeLevel(playerStats != null ? playerStats.fireRateUpgradeLevel + 1 : 0);
    public void IncreaseHealthUpgrade() => SetHealthUpgradeLevel(playerStats != null ? playerStats.healthUpgradeLevel + 1 : 0);

    // Force re-apply current upgrades to connected components
    public void RefreshUpgrades()
    {
        ApplyUpgrades();
    }
}
