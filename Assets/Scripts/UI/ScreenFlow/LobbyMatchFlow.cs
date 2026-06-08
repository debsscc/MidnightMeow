/// <summary>
/// Transição do lobby para a tela de preparação (via Loading1).
/// </summary>
public static class LobbyMatchFlow
{
    public static bool TryBeginMatchFromLobby() => ScreenFlowStateMachine.BeginPreparationFromLobby();
}
