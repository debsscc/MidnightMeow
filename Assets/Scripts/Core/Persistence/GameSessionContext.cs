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
        UpgradesOnly,
        SelectionAllowed
    }

    public static SessionStartMode StartMode { get; set; } = SessionStartMode.None;
    public static CharactersScreenMode CharactersMode { get; set; } = CharactersScreenMode.UpgradesOnly;
    public static string PendingRouteId { get; set; } = string.Empty;
    public static string ReturnRouteId { get; set; } = string.Empty;
    public static int ActiveSaveSlot { get; set; }
    public static bool AutoHostOnLobbyEnter { get; set; }

    public static void BeginNewGame(int slot = 0)
    {
        StartMode = SessionStartMode.NewGame;
        ActiveSaveSlot = slot;
        AutoHostOnLobbyEnter = false;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
    }

    public static void BeginContinue(int slot = 0)
    {
        StartMode = SessionStartMode.Continue;
        ActiveSaveSlot = slot;
        AutoHostOnLobbyEnter = true;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
    }

    public static void Reset()
    {
        StartMode = SessionStartMode.None;
        CharactersMode = CharactersScreenMode.UpgradesOnly;
        PendingRouteId = string.Empty;
        ReturnRouteId = string.Empty;
        AutoHostOnLobbyEnter = false;
    }
}
