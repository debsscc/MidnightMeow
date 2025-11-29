///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Define eventos globais do jogo que podem ser invocados e assinados por diferentes componentes.
// ---------------------------------------------------------------- */

using UnityEngine;
using System;
public static class GameEvents
{
    // Evento disparado quando o jogador coleta munição
    public static event Action OnAmmoCollected;

    // Método para invocar o evento de munição coletada
    public static void InvokeAmmoCollected()
    {
        OnAmmoCollected?.Invoke();
    }
}
