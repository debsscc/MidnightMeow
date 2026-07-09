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
    public static event Action<float, float> OnPlayerAdrenalineChanged;
    public static event Action<int> OnCienciaCollected;
    public static event Action OnAdrenalineLow;
    public static event Action<int, int, int, int> OnWaveStatusChanged;
    public static event Action<int, int, int> OnPhaseObjectiveStatusChanged;
    // Evento global disparado quando o jogo entra/ Sai do estado de pause
    public static event Action<bool> OnPauseChanged;
    public static bool IsPaused { get; private set; }
    // Evento global disparado quando todas as waves são completadas
    public static event Action OnNightEnded;
    // Evento global disparado quando o jogador morre
    public static event Action OnPlayerDefeated;
    public static event Action<float, Vector3> OnDamageShown;

    // M�todo para invocar o evento de muni��o coletada
    public static void InvokeAmmoCollected()
    {
        OnAmmoCollected?.Invoke();
    }

    public static void InvokePlayerHealthChanged(float currentHealth, float maxHealth)
    {
//        Debug.Log($"Player health changed captured by game event: {currentHealth}/{maxHealth}");
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public static void InvokeNightEnded()
    {
        Debug.Log("GameEvents: Night ended");
        OnNightEnded?.Invoke();
    }

    public static void InvokePlayerDefeated()
    {
        Debug.Log("GameEvents: Player defeated");
        OnPlayerDefeated?.Invoke();
    }

    public static void InvokeDamageShown(float amount, Vector3 worldPosition)
    {
        if (amount <= 0f) return;
        OnDamageShown?.Invoke(amount, worldPosition);
    }

    public static void InvokeCienciaCollected(int amount)
    {
        OnCienciaCollected?.Invoke(amount);
    }


    public static void InvokePlayerAdrenalineChanged(float currentAdrenaline, float maxAdrenaline)
    {
        //Debug.Log($"Player adrenaline changed captured by game event: {currentAdrenaline}/{maxAdrenaline}");
        OnPlayerAdrenalineChanged?.Invoke(currentAdrenaline, maxAdrenaline);
    }

    public static void InvokeAdrenalineLow()
    {
        OnAdrenalineLow?.Invoke();
    }

    public static void InvokeWaveStatusChanged(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        OnWaveStatusChanged?.Invoke(currentWave, totalWaves, enemiesRemaining, totalKilled);
    }

    public static void InvokePhaseObjectiveStatusChanged(int holesSealed, int totalHoles, int enemiesAlive)
    {
        OnPhaseObjectiveStatusChanged?.Invoke(holesSealed, totalHoles, enemiesAlive);
    }

    public static void InvokePauseChanged(bool paused)
    {
        IsPaused = paused;

        if (paused)
            GameplayPauseController.ApplyImmediateFreeze();
        else
            GameplayPauseController.ReleaseSpawners();

        OnPauseChanged?.Invoke(paused);
    }

    /// <summary>
    /// Congela gameplay (input, spawners) sem disparar <see cref="OnPauseChanged"/> nem abrir UI de pause.
    /// Usado em teardown de vitória/derrota.
    /// </summary>
    public static void InvokeGameplayFreeze()
    {
        IsPaused = true;
        GameplayPauseController.ApplyImmediateFreeze();
    }

    // --- Eventos de Multiplayer ---

    // Disparado quando qualquer jogador entra na partida (clientId, isLocalPlayer)
    public static event System.Action<ulong, bool> OnPlayerJoined;
    // Disparado quando qualquer jogador sai da partida (clientId)
    public static event System.Action<ulong> OnPlayerLeft;
    // Disparado quando todos os jogadores estão mortos no multiplayer
    public static event System.Action OnAllPlayersDefeated;
    // Disparado quando um inimigo morre (ClientId do instigador)
    public static event System.Action<ulong> OnEnemyKilledByPlayer;
    public static event Action<float> OnCarriagePathProgressChanged;
    public static event Action OnCarriageArrived;

    public static void InvokePlayerJoined(ulong clientId, bool isLocalPlayer)
    {
        OnPlayerJoined?.Invoke(clientId, isLocalPlayer);
    }

    public static void InvokePlayerLeft(ulong clientId)
    {
        OnPlayerLeft?.Invoke(clientId);
    }

    public static void InvokeAllPlayersDefeated()
    {
        OnAllPlayersDefeated?.Invoke();
    }

    public static void InvokeEnemyKilledByPlayer(ulong killerClientId)
    {
        OnEnemyKilledByPlayer?.Invoke(killerClientId);
    }

    public static void InvokeCarriagePathProgressChanged(float normalizedProgress)
    {
        OnCarriagePathProgressChanged?.Invoke(Mathf.Clamp01(normalizedProgress));
    }

    public static void InvokeCarriageArrived()
    {
        OnCarriageArrived?.Invoke();
    }
}
