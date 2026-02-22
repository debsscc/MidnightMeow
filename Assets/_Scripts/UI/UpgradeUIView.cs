using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class UpgradeUIView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UpgradeDefinition upgradeDefinition;
    [SerializeField] private PlayerProgressionData progressionData;
    [SerializeField] private UpgradeController controller;
    [SerializeField] private PlayerProgressionData.UpgradeType upgradeType;

    [Header("UI References")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;

    private void OnEnable()
    {
        if (progressionData != null)
            progressionData.OnChanged += UpdateUI;

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);

        UpdateUI();
    }

    private void OnDisable()
    {
        if (progressionData != null)
            progressionData.OnChanged -= UpdateUI;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
    }

    public void Setup(UpgradeDefinition def, PlayerProgressionData data, UpgradeController ctrl, PlayerProgressionData.UpgradeType type)
    {
        upgradeDefinition = def;
        progressionData = data;
        controller = ctrl;
        upgradeType = type;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (upgradeDefinition == null || progressionData == null)
        {
            if (titleText != null)
                titleText.text = "-";
            if (levelText != null)
                levelText.text = "";
            if (costText != null)
                costText.text = "";
            if (purchaseButton != null)
                purchaseButton.interactable = false;
            return;
        }

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(upgradeDefinition.displayName) ? upgradeDefinition.upgradeId : upgradeDefinition.displayName;

        int currentLevel = progressionData.GetLevel(upgradeType);
        int maxLevels = upgradeDefinition.MaxLevels;

        if (levelText != null)
            levelText.text = $"Nivel: {currentLevel}/{maxLevels}";

        if (currentLevel >= maxLevels)
        {
            if (costText != null)
                costText.text = "MÁXIMO";
            if (purchaseButton != null)
                purchaseButton.interactable = false;
            return;
        }

        int nextLevel = currentLevel + 1;
        int cost = upgradeDefinition.GetCostForLevel(nextLevel);

        if (costText != null)
            costText.text = cost.ToString();

        bool canAfford = progressionData.CanAfford(cost);
        if (purchaseButton != null)
            purchaseButton.interactable = canAfford;
    }

    public void OnPurchaseButtonClicked()
    {
        if (controller == null)
        {
            Debug.LogError("UpgradeUIView: controller not assigned.");
            return;
        }

        bool success = controller.TryPurchaseUpgrade(upgradeType);
        if (!success)
        {
            // UI feedback can be handled here (sound/animation), but do not change data directly
            Debug.Log("UpgradeUIView: purchase failed (insufficient funds or max level).");
        }
        // UpdateUI will be called via progressionData.OnChanged
    }
}
