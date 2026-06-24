/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Escuta vida do jogador local e aciona filtro visual de pouca vida (solo e MP).
---------------------------------------------------------------- */

using UnityEngine;

public static class LowHealthScreenFeedback
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;

        GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
        GameEvents.OnPlayerDefeated += HandlePlayerDefeated;

        GameEvents.OnAllPlayersDefeated -= HandlePlayerDefeated;
        GameEvents.OnAllPlayersDefeated += HandlePlayerDefeated;
    }

    private static void HandlePlayerHealthChanged(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            GameplayVignetteController.SetHealthRatio(0f);
            return;
        }

        GameplayVignetteController.SetHealthRatio(currentHealth / maxHealth);
    }

    private static void HandlePlayerDefeated()
    {
        GameplayVignetteController.SetHealthRatio(0f);
    }
}
