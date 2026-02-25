///* ----------------------------------------------------------------
// ATUALIZADO EM: 25-02-2026
// DESCRIÇÃO: Injeta dados globais nos componentes mecânicos (Vida, Tiro, Dash).
// ---------------------------------------------------------------- */

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
    [SerializeField] private PlayerDash playerDash; // Nova dependência

    private void Awake()
    {
        if (healthComponent == null) healthComponent = GetComponent<HealthComponent>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();

        if (progressionData == null && ServiceLocator.HasService<PlayerProgressionData>())
        {
            progressionData = ServiceLocator.GetService<PlayerProgressionData>();
        }
    }

    private void OnEnable()
    {
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
        float GetBonus(PlayerProgressionData.UpgradeType type)
        {
            int level = progressionData != null ? progressionData.GetLevel(type) : 0;
            int idx = (int)type;
            if (upgradeDefinitions != null && idx < upgradeDefinitions.Length && upgradeDefinitions[idx] != null)
                return upgradeDefinitions[idx].GetBonusForLevel(level);
            return 0f;
        }

        // 1. Injeção de Vida
        float healthBonus = GetBonus(PlayerProgressionData.UpgradeType.Health);
        float baseHealth = baseStats != null ? baseStats.maxHealth : (healthComponent != null ? healthComponent.MaxHealth : 100f);
        float finalMaxHealth = baseHealth * (1f + healthBonus);
        
        if (healthComponent != null)
            healthComponent.Initialize(finalMaxHealth);

        // 2. Injeção de Cadência de Tiro
        float fireRateBonus = GetBonus(PlayerProgressionData.UpgradeType.FireRate);
        if (playerShooting != null)
        {
            float finalRate = playerShooting.BaseFireRate * (1f + fireRateBonus);
            playerShooting.SetFireRate(finalRate);
        }

        // 3. Injeção de Dano
        float damageBonus = GetBonus(PlayerProgressionData.UpgradeType.Damage);
        if (playerShooting != null)
        {
            float damageMultiplier = 1f + damageBonus; 
            playerShooting.SetDamageMultiplier(damageMultiplier);
        }

        // 4. Inicialização do Dash (Prepara terreno para futuros upgrades)
        if (playerDash != null)
        {
            playerDash.InitializeBaseStats();
            
            // Exemplo de como um futuro upgrade seria injetado:
            // float dashSpeedBonus = GetBonus(PlayerProgressionData.UpgradeType.DashSpeed);
            // playerDash.SetDashUpgrades(dashSpeedBonus, 0f);
        }

        Debug.Log($"PlayerInitializer: Status Injetados. Vida: {finalMaxHealth}, DanoMult: {playerShooting?.DamageMultiplier}");
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        GameEvents.InvokePlayerHealthChanged(currentHealth, maxHealth);
    }

    private void HandlePlayerDied()
    {
        GameEvents.InvokePlayerDefeated();
    }
}