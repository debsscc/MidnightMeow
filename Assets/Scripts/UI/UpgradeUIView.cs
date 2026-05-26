using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class UpgradeUIView : MonoBehaviour
{
    [Header("Data Configurations")]
    [SerializeField] private PlayerProgressionData progressionData;
    [SerializeField] private UpgradeDefinition upgradeDefinition;
    [SerializeField] private UpgradeController controller;
    
    [Tooltip("O tipo deste upgrade (ex: Health)")]
    [SerializeField] private PlayerProgressionData.UpgradeType upgradeType;
    
    [Tooltip("Qual nível este botão representa? (1, 2 ou 3)")]
    [Range(1, 3)]
    [SerializeField] private int targetLevel = 1;

    [Header("UI References")]
    [SerializeField] private Button purchaseButton;
    [Tooltip("O componente Image que terá seu Source Image alterado (Background do botão)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject costContainer; // Opcional: para esconder a moeda se já comprado

    [Header("Visual States")]
    [Tooltip("Imagem mostrada quando o upgrade está disponível ou bloqueado")]
    [SerializeField] private Sprite availableImage;
    [Tooltip("Imagem mostrada quando o upgrade já foi comprado")]
    [SerializeField] private Sprite purchasedImage;
    [Tooltip("GameObject ativo apenas quando o upgrade está bloqueado (nível anterior não comprado)")]
    [SerializeField] private GameObject lockedObject;

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

    private void UpdateUI()
    {
        if (upgradeDefinition == null || progressionData == null) return;

        int currentLevel = progressionData.GetLevel(upgradeType);
        int cost = upgradeDefinition.GetCostForLevel(targetLevel);

        if (costText != null) costText.text = cost.ToString();

        // Lógica de Estado do Botão
        if (currentLevel >= targetLevel)
        {
            // ESTADO: Já Comprado (Ativa Sprite de Comprado, Oculta Bloqueio)
            SetVisuals(purchasedImage, false, false, false);
        }
        else if (currentLevel == targetLevel - 1)
        {
            // ESTADO: Disponível para compra (Ativa Sprite Disponível, Oculta Bloqueio)
            bool canAfford = progressionData.CanAfford(cost);
            SetVisuals(availableImage, false, canAfford, true);
        }
        else
        {
            // ESTADO: Bloqueado (Ativa Sprite Disponível, Mostra Bloqueio)
            SetVisuals(availableImage, true, false, true);
        }
    }

    private void SetVisuals(Sprite stateSprite, bool isLocked, bool isInteractable, bool showCost)
    {
        // Altera o Source Image do botão
        if (iconImage != null && stateSprite != null)
            iconImage.sprite = stateSprite;

        // Ativa/Desativa o GameObject de sobreposição de cadeado/bloqueio
        if (lockedObject != null)
            lockedObject.SetActive(isLocked);

        if (purchaseButton != null)
            purchaseButton.interactable = isInteractable;

        if (costContainer != null)
            costContainer.SetActive(showCost);
        else if (costText != null)
            costText.gameObject.SetActive(showCost);
    }

    public void OnPurchaseButtonClicked()
    {
        if (controller == null) return;

        // O controller cuida da transação. Se for sucesso, o evento OnChanged atualizará a UI.
        bool success = controller.TryPurchaseUpgrade(upgradeType);
        if (!success)
        {
            Debug.Log($"UpgradeUIView: Falha ao comprar {upgradeType} nível {targetLevel}.");
        }
    }
}