// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Componente de UI que exibe o status de um único jogador no HUD multiplayer.
// Mostra nome, barra de saúde e barra de adrenalina/frenesi. Muda de aparência ao jogador morrer (cor escura) e ao reviver. 
// É criado/destruído dinamicamente pelo MultiplayerHUD conforme jogadores entram e saem da partida.
// ---------------------------------------------------------------- 

using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class PlayerHUDCard : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider adrenalineSlider;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image playerColorIndicator;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image adrenalineFill;
    [SerializeField] private GameObject deadOverlay;
    [SerializeField] private TMP_Text statusText;

    [Header("Cores de Adrenalina")]
    [SerializeField] private Color normalAdrenalineColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color frenzyAdrenalineColor = new Color(1f, 0.4f, 0f);

    private ulong _clientId;

    public void Initialize(ulong clientId, string playerName, Color cardColor, Color playerColor)
    {
        _clientId = clientId;

        if (playerNameText != null)
            playerNameText.text = playerName;

        if (cardBackground != null)
            cardBackground.color = cardColor;

        if (playerColorIndicator != null)
            playerColorIndicator.color = playerColor;

        if (deadOverlay != null)
            deadOverlay.SetActive(false);

        if (statusText != null)
            statusText.text = "";

        // Inicializa sliders
        UpdateHealth(1f, 1f);
        UpdateAdrenaline(1f, 1f, false);
    }
    public void UpdateHealth(float current, float max)
    {
        if (healthSlider == null) return;
        healthSlider.maxValue = max > 0 ? max : 1f;
        healthSlider.value = current;
    }

    public void UpdateAdrenaline(float current, float max, bool isFrenzy)
    {
        if (adrenalineSlider == null) return;
        adrenalineSlider.maxValue = max > 0 ? max : 1f;
        adrenalineSlider.value = current;

        if (adrenalineFill != null)
            adrenalineFill.color = isFrenzy ? frenzyAdrenalineColor : normalAdrenalineColor;
    }

    public void SetDeadState(Color deadColor)
    {
        if (cardBackground != null)
            cardBackground.color = deadColor;

        if (deadOverlay != null)
            deadOverlay.SetActive(true);

        if (statusText != null)
            statusText.text = IsPortuguese() ? "INCONSCIENTE" : "DOWNED";
    }

    public void SetAliveState(Color aliveColor)
    {
        if (cardBackground != null)
            cardBackground.color = aliveColor;

        if (deadOverlay != null)
            deadOverlay.SetActive(false);

        if (statusText != null)
            statusText.text = "";
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }
}
