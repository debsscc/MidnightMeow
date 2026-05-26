using UnityEngine;

[DisallowMultipleComponent]
public class UpgradeController : MonoBehaviour
{
    [Tooltip("Reference to the player progression data (single source of truth)")]
    [SerializeField] private PlayerProgressionData progressionData;

    [Tooltip("Definitions for each upgrade. Order must match PlayerProgressionData.UpgradeType enum.")]
    [SerializeField] private UpgradeDefinition[] upgradeDefinitions = new UpgradeDefinition[3];

    public PlayerProgressionData ProgressionData => progressionData;

    private void Awake()
    {
        if (progressionData == null)
            Debug.LogWarning("UpgradeController: progressionData is not assigned.");

        if (upgradeDefinitions == null || upgradeDefinitions.Length == 0)
            Debug.LogWarning("UpgradeController: upgradeDefinitions not assigned or empty.");
    }

    public bool TryPurchaseUpgrade(PlayerProgressionData.UpgradeType type)
    {
        if (progressionData == null)
        {
            Debug.LogError("UpgradeController: progressionData missing. Cannot process purchase.");
            return false;
        }

        int index = (int)type;
        if (upgradeDefinitions == null || index < 0 || index >= upgradeDefinitions.Length)
        {
            Debug.LogError($"UpgradeController: No UpgradeDefinition for type {type} (index {index}).");
            return false;
        }

        var def = upgradeDefinitions[index];

        int currentLevel = progressionData.GetLevel(type);
        int maxLevels = def.MaxLevels;

        if (currentLevel >= maxLevels)
        {
            return false; // already at max
        }

        int nextLevel = currentLevel + 1; // 1-based for UpgradeDefinition
        int cost = def.GetCostForLevel(nextLevel);

        if (!progressionData.SpendScience(cost))
        {
            return false; // cannot afford
        }

        progressionData.IncreaseLevel(type);
        return true;
    }

    // Utility getter for UI or other systems
    public UpgradeDefinition GetDefinition(PlayerProgressionData.UpgradeType type)
    {
        int idx = (int)type;
        if (upgradeDefinitions == null || idx < 0 || idx >= upgradeDefinitions.Length) return null;
        return upgradeDefinitions[idx];
    }
}
