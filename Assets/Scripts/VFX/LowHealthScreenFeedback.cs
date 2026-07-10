/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Escuta vida do jogador local e aciona filtro visual de pouca vida (solo e MP).
---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;

public static class LowHealthScreenFeedback
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged -= HandleNetworkHealthChanged;
        NetworkPlayerHealth.OnNetworkHealthChanged += HandleNetworkHealthChanged;

        NetworkPlayerHealth.OnNetworkPlayerDowned -= HandleLocalPlayerDowned;
        NetworkPlayerHealth.OnNetworkPlayerDowned += HandleLocalPlayerDowned;

        NetworkPlayerHealth.OnNetworkPlayerRevived -= HandleLocalPlayerRevived;
        NetworkPlayerHealth.OnNetworkPlayerRevived += HandleLocalPlayerRevived;
    }

    private static void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (!IsLocalClient(clientId))
            return;

        if (max <= 0f)
        {
            GameplayVignetteController.SetHealthRatio(0f);
            return;
        }

        GameplayVignetteController.SetHealthRatio(current / max);
    }

    private static void HandleLocalPlayerDowned(ulong clientId)
    {
        if (!IsLocalClient(clientId))
            return;

        if (HasRevivableTeammateDown())
        {
            GameplayVignetteController.ClearDeathVisualHold();
            GameplayVignetteController.SetHealthRatio(1f);
            return;
        }

        GameplayVignetteController.SetHealthRatio(0f);
    }

    private static bool HasRevivableTeammateDown()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return false;

        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
        bool anyRevivable = false;
        bool anyFighting = false;

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned)
                continue;

            if (health.CanBeRevived)
                anyRevivable = true;

            if (health.CanFight)
                anyFighting = true;
        }

        return anyRevivable && anyFighting;
    }

    private static void HandleLocalPlayerRevived(ulong clientId)
    {
        if (!IsLocalClient(clientId))
            return;

        NetworkPlayerHealth local = FindLocalPlayerHealth();
        if (local == null)
        {
            GameplayVignetteController.SetHealthRatio(1f);
            return;
        }

        float max = local.MaxHealth;
        GameplayVignetteController.SetHealthRatio(max > 0f ? local.CurrentHealth / max : 1f);
    }

    private static bool IsLocalClient(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null && clientId == networkManager.LocalClientId;
    }

    private static NetworkPlayerHealth FindLocalPlayerHealth()
    {
        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health != null && health.IsSpawned && health.IsOwner)
                return health;
        }

        return null;
    }
}
