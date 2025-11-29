///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia o ciclo do dia, permitindo iniciar e terminar o dia.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startNightButton; // Arraste o botão aqui

    public event System.Action OnDayEnded;

    private void Start()
    {
        // Configura o botão
        startNightButton.onClick.AddListener(EndDay);
    }

    public void StartDay()
    {
        Debug.Log("--- INÍCIO DO DIA ---");
        startNightButton.gameObject.SetActive(true);
        startNightButton.interactable = true;
    }

    private void EndDay()
    {
        startNightButton.interactable = false; // Evita clique duplo
        startNightButton.gameObject.SetActive(false);
        OnDayEnded?.Invoke();
    }
}