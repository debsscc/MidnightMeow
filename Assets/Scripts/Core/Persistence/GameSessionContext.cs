/// <summary>
/// Estado volátil da sessão atual (não persistido). Coordena origem de telas e rotas pendentes.
/// </summary>
public static class GameSessionContext
{
    public enum SessionStartMode
    {
        None,
        NewGame,
        Continue
    }

    public enum CharactersScreenMode
    {
        /// <summary>Menu/Lobby: apenas consulta de skills, sem níveis nem compras.</summary>
        UpgradesOnly,
        /// <summary>Preparação: seleção sincronizada + upgrades com magículas.</summary>
        SelectionAllowed
    }

    public enum CharactersScreenOrigin
    {
        None,
        Menu,
        Lobby,
        Preparation
    }

    public static SessionStartMode StartMode { get; set; } = SessionStartMode.None;
    public static ScreenFlowPhase CurrentPhase { get; set; } = ScreenFlowPhase.None;
    public static CharactersScreenMode CharactersMode { get; set; } = CharactersScreenMode.UpgradesOnly;
    public static CharactersScreenOrigin CharactersOrigin { get; set; } = CharactersScreenOrigin.None;
    public static string PendingRouteId { get; set; } = string.Empty;
    public static string ReturnRouteId { get; set; } = string.Empty;
    public static string ActiveGameplaySceneName { get; set; } = "Fase-1";
    public static int ActiveContractIndex { get; set; } = -1;
    public static int ActiveSaveSlot { get; set; }
    public static bool AutoHostOnLobbyEnter { get; set; }
    public static bool IsSinglePlayer { get; set; }

    public static void BeginNewGame(int slot = 0)
    {
        StartMode = SessionStartMode.NewGame;
        ActiveSaveSlot = slot;
        AutoHostOnLobbyEnter = false;
        IsSinglePlayer = false;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        CharactersOrigin = CharactersScreenOrigin.None;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
    }

    public static void BeginContinue(int slot = 0)
    {
        StartMode = SessionStartMode.Continue;
        ActiveSaveSlot = slot;
        AutoHostOnLobbyEnter = true;
        IsSinglePlayer = false;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        CharactersOrigin = CharactersScreenOrigin.None;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
    }

    public static void BeginSinglePlayer()
    {
        IsSinglePlayer = true;
        AutoHostOnLobbyEnter = false;
    }

    public static void BeginMultiplayer()
    {
        IsSinglePlayer = false;
    }

    public static void Reset()
    {
        StartMode = SessionStartMode.None;
        CurrentPhase = ScreenFlowPhase.None;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        CharactersOrigin = CharactersScreenOrigin.None;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
        ActiveGameplaySceneName = "Fase-1";
        ActiveContractIndex = -1;
        AutoHostOnLobbyEnter = false;
        IsSinglePlayer = false;
    }

    public static void ResetContractRound()
    {
        PendingRouteId = string.Empty;
    }
}
