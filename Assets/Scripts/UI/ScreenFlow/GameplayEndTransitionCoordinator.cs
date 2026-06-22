using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sincroniza vitória/derrota em multiplayer: todos os peers iniciam fade antes do host trocar de cena.
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

        // Host/servidor dispara a troca de cena via MultiplayerGameManager.ReturnToPreparationRoutine.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            return;

        if (newState == GameState.Victory)
            ScreenFlowStateMachine.ShowVictoryScreen();
        else
            ScreenFlowStateMachine.ShowDefeatScreen();
    }

    private static bool IsNetworkGameplaySession()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsListening)
            return false;

        return MultiplayerGameManager.Instance != null;
    }
}
