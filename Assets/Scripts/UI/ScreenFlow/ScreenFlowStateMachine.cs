using Unity.Netcode;

using UnityEngine;

using UnityEngine.SceneManagement;



/// <summary>

/// Orquestra transições do fluxo de telas e reseta estado volátil entre fases.

/// </summary>

public static class ScreenFlowStateMachine

{

    public static ScreenFlowPhase CurrentPhase { get; private set; } = ScreenFlowPhase.None;



    public static void EnterPhase(ScreenFlowPhase phase)

    {

        CurrentPhase = phase;

        GameSessionContext.CurrentPhase = phase;

    }



    public static bool TryTransition(string routeId)

    {

        if (GameFlowOrchestrator.Instance != null && GameFlowOrchestrator.Instance.TryRequestRoute(routeId))

            return true;



        if (ScreenFlowController.Instance != null && ScreenFlowController.Instance.RequestRoute(routeId))

            return true;



        return false;

    }



    /// <summary>Lobby (solo ou MP sincronizado) → Loading1 → Preparation.</summary>

    public static bool BeginPreparationFromLobby()

    {

        EnterPhase(ScreenFlowPhase.ContractSelect);

        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading1ToPreparation;

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.SelectionAllowed;



        if (TryTransition(SceneFlowRouteIds.LobbyToLoading1))

            return true;



        return LoadSceneFallback("Loading1");

    }



    /// <summary>Lobby → Characters (somente consulta de skills).</summary>

    public static bool OpenCharactersFromLobby()

    {

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.UpgradesOnly;

        GameSessionContext.CharactersOrigin = GameSessionContext.CharactersScreenOrigin.Lobby;

        GameSessionContext.ReturnRouteId = SceneFlowRouteIds.ReturnToLobby;



        if (TryTransition(SceneFlowRouteIds.LobbyToCharacters))

            return true;



        return LoadSceneFallback("Characters");

    }



    /// <summary>Preparation → Characters (seleção + upgrades).</summary>

    public static bool OpenCharactersFromPreparation()

    {

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.SelectionAllowed;

        GameSessionContext.CharactersOrigin = GameSessionContext.CharactersScreenOrigin.Preparation;

        GameSessionContext.ReturnRouteId = SceneFlowRouteIds.CharactersToPreparation;



        if (TryTransition(SceneFlowRouteIds.PreparationToCharacters))

            return true;



        return LoadSceneFallback("Characters");

    }



    /// <summary>Preparation → Loading2 → Gameplay.</summary>

    public static bool BeginGameplayLoading()

    {

        EnterPhase(ScreenFlowPhase.LoadingToGameplay);

        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToGameplay;



        if (TryTransition(SceneFlowRouteIds.PreparationToLoading2))

            return true;



        return LoadSceneFallback("Loading2");

    }



    /// <summary>Gameplay → tela de vitória.</summary>

    public static bool ShowVictoryScreen()

    {

        EnterPhase(ScreenFlowPhase.Gameplay);

        if (TryTransition(SceneFlowRouteIds.GameplayToVictory))

            return true;



        return LoadSceneFallback("VictoryScene");

    }



    /// <summary>Gameplay → tela de derrota.</summary>

    public static bool ShowDefeatScreen()

    {

        EnterPhase(ScreenFlowPhase.Gameplay);

        if (TryTransition(SceneFlowRouteIds.GameplayToDefeat))

            return true;



        return LoadSceneFallback("GameOver");

    }



    /// <summary>Vitória/Derrota → Preparation (mantém sincronização MP).</summary>

    public static bool ContinueAfterEndGame()

    {

        EnterPhase(ScreenFlowPhase.ContractSelect);

        GameSessionContext.PendingRouteId = string.Empty;

        GameSessionContext.ResetContractRound();



        string route = SceneManager.GetActiveScene().name == "VictoryScene"

            ? SceneFlowRouteIds.VictoryToPreparation

            : SceneFlowRouteIds.DefeatToPreparation;



        if (TryTransition(route))

            return true;



        return LoadSceneFallback("Preparation");

    }



    /// <summary>Desconecta e retorna ao menu principal.</summary>

    public static bool ExitToMainMenu()

    {

        ConnectionManager.Instance?.Disconnect();

        GameSessionContext.Reset();

        LobbySelectionStore.Clear();



        if (TryTransition(SceneFlowRouteIds.ReturnToMenu))

            return true;



        return LoadSceneFallback("Menu2");

    }



    /// <summary>Legado — redireciona para ContinueAfterEndGame.</summary>

    public static bool ReturnToContractSelect() => ContinueAfterEndGame();



    /// <summary>Loading2 → cena de gameplay do contrato ativo.</summary>

    public static bool EnterGameplay()

    {

        EnterPhase(ScreenFlowPhase.Gameplay);

        string scene = string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)

            ? "Fase-1"

            : GameSessionContext.ActiveGameplaySceneName;



        if (ScreenFlowController.Instance != null

            && ScreenFlowController.Instance.RequestScene(

                scene,

                ScreenTransitionMode.Fade,

                ResolveLoadKind(),

                1f,

                0f))

        {

            return true;

        }



        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsServer)

        {

            networkManager.SceneManager.LoadScene(scene, LoadSceneMode.Single);

            return true;

        }



        SceneManager.LoadScene(scene);

        return true;

    }



    private static SceneLoadKind ResolveLoadKind()

    {

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)

            return SceneLoadKind.NetcodeHost;



        if (GameSessionContext.IsSinglePlayer)

            return SceneLoadKind.SinglePlayer;



        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)

            return SceneLoadKind.NetcodeHost;



        return SceneLoadKind.SinglePlayer;

    }



    private static bool LoadSceneFallback(string sceneName)

    {

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsServer)

        {

            networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            return true;

        }



        if (ScreenFlowController.Instance != null)

            return ScreenFlowController.Instance.TryBeginTransition(sceneName);



        SceneManager.LoadScene(sceneName);

        return true;

    }

}


