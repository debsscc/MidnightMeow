/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Orquestra transições do fluxo de telas e reseta estado volátil entre fases.
---------------------------------------------------------------- */

using Unity.Netcode;

using UnityEngine;

using UnityEngine.SceneManagement;



public static class ScreenFlowStateMachine

{

    public static ScreenFlowPhase CurrentPhase { get; private set; } = ScreenFlowPhase.None;



    public static void EnterPhase(ScreenFlowPhase phase)

    {

        CurrentPhase = phase;

        GameSessionContext.CurrentPhase = phase;

        MidnightMeowAnalytics.TrackFlowPhase(phase);

    }



    public static bool TryTransition(string routeId)

    {

        if (GameFlowOrchestrator.Instance != null && GameFlowOrchestrator.Instance.TryRequestRoute(routeId))

            return true;



        if (ScreenFlowController.Instance != null && ScreenFlowController.Instance.RequestRoute(routeId))

            return true;



        return false;

    }



    public static bool BeginPreparationFromLobby()

    {

        EnterPhase(ScreenFlowPhase.ContractSelect);

        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading1ToPreparation;

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.SelectionAllowed;

        ResetPreparationContractForNewLobbyEntry();

        if (TryTransition(SceneFlowRouteIds.LobbyToLoading1))

            return true;



        return LoadSceneFallback("Loading1");

    }



    public static bool OpenCharactersFromLobby()

    {

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.UpgradesOnly;

        GameSessionContext.CharactersOrigin = GameSessionContext.CharactersScreenOrigin.Lobby;

        GameSessionContext.ReturnRouteId = SceneFlowRouteIds.ReturnToLobby;



        if (TryTransition(SceneFlowRouteIds.LobbyToCharacters))

            return true;



        return LoadSceneFallback("Characters");

    }



    public static bool OpenCharactersFromPreparation()

    {

        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.SelectionAllowed;

        GameSessionContext.CharactersOrigin = GameSessionContext.CharactersScreenOrigin.Preparation;

        GameSessionContext.ReturnRouteId = SceneFlowRouteIds.CharactersToPreparation;



        if (TryTransition(SceneFlowRouteIds.PreparationToCharacters))

            return true;



        return LoadSceneFallback("Characters");

    }



    public static bool BeginGameplayLoading()

    {

        EnterPhase(ScreenFlowPhase.LoadingToGameplay);

        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToGameplay;



        if (TryTransition(SceneFlowRouteIds.PreparationToLoading2))

            return true;



        return LoadSceneFallback("Loading2");

    }



    public static bool ShowVictoryScreen()

    {

        GameplaySessionTeardown.PrepareForEndGameScreen();
        Time.timeScale = 1f;

        EnterPhase(ScreenFlowPhase.Gameplay);

        if (TryTransition(SceneFlowRouteIds.GameplayToVictory))

            return true;



        return LoadSceneFallback("VictoryScene");

    }



    public static bool ShowDefeatScreen()

    {

        GameplaySessionTeardown.PrepareForEndGameScreen();
        Time.timeScale = 1f;
        GameplayVignetteController.ClearIfActive();

        EnterPhase(ScreenFlowPhase.Gameplay);

        if (TryTransition(SceneFlowRouteIds.GameplayToDefeat))

            return true;



        return LoadSceneFallback("GameOver");

    }



    public static bool ContinueAfterEndGame()
    {
        EnterPhase(ScreenFlowPhase.ContractSelect);
        GameSessionContext.PendingRouteId = string.Empty;
        GameSessionContext.ResetContractRound();
        ResetPreparationCharacterSelection();

        // Cliente MP: não chama LoadScene — pede ao host (evita fade preto eterno).
        if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)
        {
            PreparationSessionManager.Instance?.RequestContinueAfterEndGameServerRpc();
            return true;
        }

        string route = SceneManager.GetActiveScene().name == "VictoryScene"
            ? SceneFlowRouteIds.VictoryToPreparation
            : SceneFlowRouteIds.DefeatToPreparation;

        if (TryTransition(route))
            return true;

        return LoadSceneFallback("Preparation");
    }

    /// <summary>
    /// Vitória → próxima fase (contrato+1) ou créditos se já for a fase final.
    /// Em MP: host sincroniza cena/contrato e preserva personagem de cada cliente.
    /// </summary>
    public static bool ContinueAfterVictory()
    {
        int currentIndex = ContractSceneResolver.ResolveActiveContractIndex();
        string activeScene = GameSessionContext.ActiveGameplaySceneName;

        if (VictoryContinueResolver.IsFinalPhase(currentIndex, activeScene))
            return OpenVictoryCredits();

        // Cliente MP: host avança o contrato e carrega a próxima fase.
        if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)
        {
            PreparationSessionManager.Instance?.RequestContinueAfterVictoryServerRpc();
            return true;
        }

        return AdvanceToNextPhaseAfterVictory(currentIndex, activeScene);
    }

    public static bool IsFinalVictoryPhase()
    {
        return VictoryContinueResolver.IsFinalPhase(
            ContractSceneResolver.ResolveActiveContractIndex(),
            GameSessionContext.ActiveGameplaySceneName);
    }

    private static bool OpenVictoryCredits()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        bool isNetworkSession = !GameSessionContext.IsSinglePlayer
            && networkManager != null
            && networkManager.IsListening;

        if (!isNetworkSession)
        {
            CreditsOverlayController.Open(CreditsPresentationConfig.ManualClose);
            return true;
        }

        if (networkManager.IsServer)
        {
            PreparationSessionManager.Instance?.BroadcastVictoryCredits();
            if (PreparationSessionManager.Instance == null)
                CreditsOverlayController.Open(CreditsPresentationConfig.ManualClose);
            return true;
        }

        PreparationSessionManager.Instance?.RequestOpenVictoryCreditsServerRpc();
        if (PreparationSessionManager.Instance == null)
            CreditsOverlayController.Open(CreditsPresentationConfig.ManualClose);
        return true;
    }

    private static bool AdvanceToNextPhaseAfterVictory(int currentIndex, string activeScene)
    {
        int nextIndex = VictoryContinueResolver.ResolveNextContractIndex(currentIndex, activeScene);

        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session != null && session.IsServer)
            session.AdvanceContractPreservingCharactersOnServer(nextIndex);
        else
        {
            ContractSceneResolver.ApplyToSession(nextIndex);
            session?.CaptureSelectionsToStore();
        }

        return LoadNextGameplayPhaseAfterVictory();
    }

    private static bool LoadNextGameplayPhaseAfterVictory()
    {
        Time.timeScale = 1f;
        GameplayVignetteController.ClearIfActive();
        GameSessionContext.ResetContractRound();
        RoundMagiculaTracker.Instance?.ResetRound();

        // Nunca ResetRound() aqui — isso apagaria CharacterType de cada jogador.
        PreparationSessionManager.Instance?.CaptureSelectionsToStore();

        string sceneName = GameSessionContext.ActiveGameplaySceneName;
        if (string.IsNullOrEmpty(sceneName) || !GameplaySceneBootstrap.IsGameplayScene(sceneName))
            sceneName = ContractSceneResolver.ResolveSceneName(
                VictoryContinueResolver.ResolveCurrentContractIndex(
                    ContractSceneResolver.ResolveActiveContractIndex(),
                    GameSessionContext.ActiveGameplaySceneName));

        if (string.IsNullOrEmpty(sceneName))
            return false;

        NetworkManager networkManager = NetworkManager.Singleton;
        bool isNetworkSession = !GameSessionContext.IsSinglePlayer
            && networkManager != null
            && networkManager.IsListening;

        if (isNetworkSession)
        {
            if (!networkManager.IsServer)
                return false;

            return RestartGameplayPhaseOnServer(networkManager, sceneName);
        }

        DespawnPlayersForRestart();
        EnterPhase(ScreenFlowPhase.LoadingToGameplay);
        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        if (TryTransition(SceneFlowRouteIds.PreparationToLoading2))
            return true;

        return LoadSceneFallback("Loading2");
    }



    public static bool ExitToMainMenu()

    {

        RoundMagiculaTracker.Instance?.CommitToSave();

        ConnectionManager.Instance?.Disconnect();

        GameSessionContext.Reset();

        LobbySelectionStore.Clear();



        if (TryTransition(SceneFlowRouteIds.ReturnToMenu))

            return true;



        return LoadSceneFallback("Menu2");

    }



    public static bool ReturnToLobbyFromEndGame()

    {

        Time.timeScale = 1f;

        EnterPhase(ScreenFlowPhase.Lobby);

        GameSessionContext.PendingRouteId = string.Empty;

        GameSessionContext.ResetContractRound();

        PreparationSessionManager.Instance?.ResetRound();



        if (TryTransition(SceneFlowRouteIds.ReturnToLobby))

            return true;



        return LoadSceneFallback("Lobby");

    }



    public static void RequestRestartCurrentGameplay()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        bool isNetworkSession = !GameSessionContext.IsSinglePlayer
            && networkManager != null
            && networkManager.IsListening;

        if (!isNetworkSession)
        {
            RestartCurrentGameplay();
            return;
        }

        if (networkManager.IsServer)
            RestartCurrentGameplay();
        else
            PreparationSessionManager.Instance?.RequestRestartGameplayServerRpc();
    }

    public static bool RestartCurrentGameplay()
    {
        Time.timeScale = 1f;
        GameplayVignetteController.ClearIfActive();
        GameSessionContext.ResetContractRound();
        PreparationSessionManager.Instance?.ResetRound();
        RoundMagiculaTracker.Instance?.ResetRound();

        string sceneName = ResolveRestartGameplaySceneName();
        if (string.IsNullOrEmpty(sceneName))
            return false;

        NetworkManager networkManager = NetworkManager.Singleton;
        bool isNetworkSession = !GameSessionContext.IsSinglePlayer
            && networkManager != null
            && networkManager.IsListening;

        if (isNetworkSession)
        {
            if (!networkManager.IsServer)
                return false;

            return RestartGameplayPhaseOnServer(networkManager, sceneName);
        }

        DespawnPlayersForRestart();
        EnterPhase(ScreenFlowPhase.LoadingToGameplay);
        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        if (TryTransition(SceneFlowRouteIds.PreparationToLoading2))
            return true;

        return LoadSceneFallback("Loading2");
    }

    private static string ResolveRestartGameplaySceneName()
    {
        if (!string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)
            && GameplaySceneBootstrap.IsGameplayScene(GameSessionContext.ActiveGameplaySceneName))
            return GameSessionContext.ActiveGameplaySceneName;

        string activeScene = SceneManager.GetActiveScene().name;
        if (GameplaySceneBootstrap.IsGameplayScene(activeScene))
            return activeScene;

        return string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)
            ? "Fase-1"
            : GameSessionContext.ActiveGameplaySceneName;
    }

    private static bool RestartGameplayPhaseOnServer(NetworkManager networkManager, string sceneName)
    {
        EnterPhase(ScreenFlowPhase.Gameplay);
        MultiplayerGameManager.Instance?.ServerPrepareForGameplayRestart();
        DespawnPlayersForRestart();
        networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        return true;
    }

    private static void DespawnPlayersForRestart()
    {
        if (PlayerSpawnManager.Instance != null)
        {
            PlayerSpawnManager.Instance.DespawnAllPlayersForRestart();
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
                continue;

            NetworkObject playerObject = client.PlayerObject;
            if (playerObject != null && playerObject.IsSpawned)
                playerObject.Despawn(true);
        }
    }



    public static bool ReturnToContractSelect() => ContinueAfterEndGame();



    public static bool EnterGameplay()

    {

        EnterPhase(ScreenFlowPhase.Gameplay);

        int contractIndex = ContractSceneResolver.ResolveActiveContractIndex();
        if (contractIndex >= 0)
            ContractSceneResolver.ApplyToSession(contractIndex);

        string scene = string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)

            ? ContractSceneResolver.ResolveSceneName(contractIndex)

            : GameSessionContext.ActiveGameplaySceneName;



        NetworkManager networkManager = NetworkManager.Singleton;

        if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)

            return true;



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

    public static void ResetPreparationCharacterSelection()
    {
        LobbySelectionStore.Clear();

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active == null)
            return;

        save.Active.lastSelectedCharacter = LobbyCharacterType.Default;
        save.SaveActive();
    }

    private static void ResetPreparationContractForNewLobbyEntry()
    {
        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session != null && session.IsServer)
            session.ResetRound();

        ResetPreparationCharacterSelection();

        if (!GameSessionContext.IsSinglePlayer)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active == null)
            return;

        save.Active.selectedContractIndex = -1;
        save.SaveActive();
    }

    private static bool LoadSceneFallback(string sceneName)

    {

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsServer)

        {

            networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            return true;

        }



        if (ScreenFlowController.Instance != null
            && ScreenFlowController.Instance.TryBeginTransition(sceneName))

            return true;



        SceneManager.LoadScene(sceneName);

        return true;

    }

}


