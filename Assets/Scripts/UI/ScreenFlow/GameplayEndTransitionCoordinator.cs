using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sincroniza vitória/derrota em multiplayer via <see cref="MultiplayerGameManager.BeginEndGameScreenTransitionClientRpc"/>.
/// </summary>
public static class GameplayEndTransitionCoordinator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Subscribe()
    {
        MultiplayerGameManager.OnGameStateChanged -= HandleGameStateChanged;
        MultiplayerGameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private static void HandleGameStateChanged(GameState newState)
    {
        if (newState != GameState.Victory && newState != GameState.Defeat)
            return;

        if (!IsNetworkGameplaySession())
            return;

        // Transição unificada no ClientRpc do MultiplayerGameManager (host + clientes).
    }

    private static bool IsNetworkGameplaySession()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsListening)
            return false;

        return MultiplayerGameManager.Instance != null;
    }
}
