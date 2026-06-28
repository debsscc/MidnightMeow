using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Garante transição para vitória mesmo se <see cref="MultiplayerGameManager"/> ainda não tiver feito spawn de rede.
/// </summary>
public static class MultiplayerVictoryCoordinator
{
    public static void TryBeginVictoryFromPhaseObjective()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net != null && !net.IsServer)
            return;

        MultiplayerGameManager manager = MultiplayerGameManager.Instance;
        if (manager != null)
        {
            manager.RequestVictoryFromPhaseObjective();
            return;
        }

        Debug.LogWarning("[MultiplayerVictoryCoordinator] MultiplayerGameManager ausente — fallback local.");
        PhaseObjectiveManager.Instance?.BeginVictoryScreenFallback();
    }
}
