///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente de UI que exibe a barra de adrenalina do jogador.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class AdrenalineBarUi : MonoBehaviour
{
    [SerializeField] private Slider adrenalineSlider;
    [SerializeField] private PlayerStats stats;

    [Header("Unity Events")]
    public UnityEvent OnAdrenalineLow;

    private bool _hasInvokedLowEvent = false;

    private void OnEnable()
    {
        GameEvents.OnPlayerAdrenalineChanged += UpdateAdrenalineBar;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerAdrenalineChanged -= UpdateAdrenalineBar;
    }

    private void Start()
    {
        if (adrenalineSlider != null)
        {
            adrenalineSlider.maxValue = 1f;
            adrenalineSlider.value = 1f;
        }
    }

    private void UpdateAdrenalineBar(float currentAdrenaline, float maxAdrenaline)
    {
        if (adrenalineSlider != null && maxAdrenaline > 0)
        {
            float normalizedValue = currentAdrenaline / maxAdrenaline;
            adrenalineSlider.value = normalizedValue;

            if (stats != null)
            {
                float lowThresholdNormalized = stats.adrenalineLowThreshold / maxAdrenaline;

                if (normalizedValue <= lowThresholdNormalized && !_hasInvokedLowEvent)
                {
                    _hasInvokedLowEvent = true;
                    OnAdrenalineLow?.Invoke();
                }
                else if (normalizedValue > lowThresholdNormalized)
                {
                    _hasInvokedLowEvent = false;
                }
            }
        }
    }
}