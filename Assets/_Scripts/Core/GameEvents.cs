///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Define eventos globais do jogo que podem ser invocados e assinados por diferentes componentes.
// ---------------------------------------------------------------- */

using UnityEngine;
using System;
public static class GameEvents
{
    // Evento disparado quando o jogador coleta muni��o
    public static event Action OnAmmoCollected;
    public static event Action<float, float> OnPlayerHealthChanged;

    // M�todo para invocar o evento de muni��o coletada
    public static void InvokeAmmoCollected()
    {
        OnAmmoCollected?.Invoke();
    }

    public static void InvokePlayerHealthChanged(float currentHealth, float maxHealth)
    {
        Debug.Log($"Player health changed captured by game event: {currentHealth}/{maxHealth}");
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
