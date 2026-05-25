/// <summary>
/// IDs estáveis de rotas para código e para o catálogo <see cref="SceneFlowCatalog"/>.
/// Designers podem usar estes IDs em <see cref="ScreenFlowRequest"/> ou referenciar o asset da rota.
/// </summary>
public static class SceneFlowRouteIds
{
    public const string BootstrapToMenu = "bootstrap_menu";
    public const string MenuToLobby = "menu_lobby";
    public const string MenuToMenu = "menu_reload";
    public const string LobbyToGameplay = "lobby_gameplay";
    public const string ReturnToMenu = "return_menu";
}
