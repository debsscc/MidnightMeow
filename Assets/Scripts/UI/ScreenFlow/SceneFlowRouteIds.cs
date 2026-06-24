/// <summary>
/// IDs estáveis de rotas para código e para o catálogo <see cref="SceneFlowCatalog"/>.
/// Designers podem usar estes IDs em <see cref="ScreenFlowRequest"/> ou referenciar o asset da rota.
/// </summary>
public static class SceneFlowRouteIds
{
    public const string BootstrapToMenu = "bootstrap_menu";
    public const string MenuToLobby = "menu_lobby";
    public const string MenuToMenu = "menu_reload";
    /// <summary>Legado — redireciona para Loading1. Prefira <see cref="LobbyToLoading1"/>.</summary>
    public const string LobbyToGameplay = "lobby_gameplay";
    public const string ReturnToMenu = "return_menu";

    public const string LobbyToLoading1 = "lobby_loading1";
    public const string Loading1ToPreparation = "loading1_preparation";
    public const string LobbyToCharacters = "lobby_characters";
    public const string CharactersToPreparation = "characters_preparation";
    public const string ReturnToLobby = "return_lobby";
    public const string PreparationToCharacters = "preparation_characters";
    public const string PreparationToHub = "preparation_hub";
    public const string PreparationToLoading2 = "preparation_loading2";
    public const string Loading2ToLobby = "loading2_lobby";
    public const string Loading2ToGameplay = "loading2_gameplay";
    public const string GameplayToPreparation = "gameplay_preparation";
    public const string GameplayToVictory = "gameplay_victory";
    public const string GameplayToDefeat = "gameplay_defeat";
    public const string VictoryToPreparation = "victory_preparation";
    public const string DefeatToPreparation = "defeat_preparation";
}
