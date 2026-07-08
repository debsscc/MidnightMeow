/// <summary>
/// MultiplayerGameManager.cs
/// NetworkBehaviour server-autoritativo que gerencia o estado global da partida multiplayer.
/// Replica o estado de jogo (em andamento, pausado, vitória, derrota) para todos os clientes
/// via NetworkVariable. Ouve eventos de ondas para vitória; mortes de jogadores vêm de
/// NetworkPlayerHealth.RegisterPlayerDeath no servidor (sem depender de GameEvents).
/// IMPORTANTE NO EDITOR: Este componente DEVE estar num GameObject que também tenha NetworkObject.
/// Como é um scene-object NetworkBehaviour, é automaticamente sincronizado ao host iniciar.
/// SRP: gerencia apenas o estado macro da partida, não lógica de spawn ou UI.
/// </summary>

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerGameManager : NetworkBehaviour
{
    public static MultiplayerGameManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private MultiplayerConfig multiplayerConfig;
    [SerializeField] private GameConfig gameConfig;
    [Tooltip("Cena de gameplay legada (ex.: Game). Fases Fase-* são detectadas automaticamente.")]
    [SerializeField] private string gameplaySceneName = "Fase-1";

    private NetworkVariable<GameState> _networkGameState = new NetworkVariable<GameState>(
        GameState.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> _playersFighting = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _resumeCountdown = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool _defeatSequenceStarted;
    private bool _victorySequenceStarted;
    private Coroutine _resumeCountdownRoutine;

    public GameState CurrentState => _networkGameState.Value;
    public bool HasReachedVictoryState => IsVictoryTransitionComplete();
    public int PlayersFighting => _playersFighting.Value;
    public int PlayersAlive => _playersFighting.Value;
    public int ResumeCountdown => _resumeCountdown.Value;
    public bool IsResumeCountdownActive => _resumeCountdown.Value > 0;
    public DownedPlayerConfig DownedPlayerConfig =>
        multiplayerConfig != null ? multiplayerConfig.downedPlayerConfig : null;

    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action OnVictory;
    public static event System.Action OnDefeat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameEvents.OnNightEnded += HandleNightEndedFromEvent;

        if (GetComponent<NetworkDownedReviveManager>() == null)
            gameObject.AddComponent<NetworkDownedReviveManager>();
    }

    public override void OnDestroy()
    {
        GameEvents.OnNightEnded -= HandleNightEndedFromEvent;

        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    private void HandleNightEndedFromEvent()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
            return;

        HandleNightEnded();
    }

    public void RequestVictoryFromPhaseObjective()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
            return;

        if (IsVictoryTransitionComplete())
            return;

        if (_victorySequenceStarted)
        {
            StartCoroutine(ForceVictoryFallbackRoutine());
            return;
        }

        BeginVictoryTransition();
    }

    public override void OnNetworkSpawn()
    {
        _networkGameState.OnValueChanged += HandleGameStateChanged;
        _playersFighting.OnValueChanged += HandlePlayersFightingChanged;

        if (IsServer)
            TryAutoBeginGameplayOnServer();

        Debug.Log($"[MultiplayerGameManager] Spawned. IsServer={IsServer}, IsHost={IsHost}");
    }

    public override void OnNetworkDespawn()
    {
        _networkGameState.OnValueChanged -= HandleGameStateChanged;
        _playersFighting.OnValueChanged -= HandlePlayersFightingChanged;
    }

    /// <summary>
    /// Inicia a partida. Chamado pelo host após todos os jogadores estarem prontos.
    /// Rpc: executa no servidor; qualquer cliente pode invocar.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestStartGameRpc() => ServerBeginGameplaySession();

    /// <summary>
    /// Coloca a sessão em Playing no servidor (dispara ondas via NetworkWaveManager).
    /// </summary>
    public void ServerBeginGameplaySession()
    {
        if (!IsServer || _networkGameState.Value != GameState.WaitingForPlayers) return;

        _defeatSequenceStarted = false;
        _playersFighting.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        _networkGameState.Value = GameState.Playing;
        Debug.Log($"[MultiplayerGameManager] Jogo iniciado com {_playersFighting.Value} jogador(es).");
    }

    private void TryAutoBeginGameplayOnServer()
    {
        if (!IsServer || _networkGameState.Value != GameState.WaitingForPlayers) return;

        string activeScene = SceneManager.GetActiveScene().name;
        if (!IsActiveGameplayScene(activeScene)) return;

        ServerBeginGameplaySession();
    }

    private bool IsActiveGameplayScene(string sceneName) =>
        GameplaySceneBootstrap.IsGameplayScene(sceneName)
        || (!string.IsNullOrEmpty(gameplaySceneName) && sceneName == gameplaySceneName);

    public void RegisterPlayerDowned()
    {
        if (!IsServer) return;

        if (_networkGameState.Value == GameState.WaitingForPlayers)
            ServerBeginGameplaySession();

        _playersFighting.Value = Mathf.Max(0, _playersFighting.Value - 1);
        Debug.Log($"[MultiplayerGameManager] Jogador inconsciente. Em combate: {_playersFighting.Value}");

        TryBeginDefeatSequence();
    }

    public void RegisterPlayerRevived()
    {
        if (!IsServer) return;
        int maxPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
        _playersFighting.Value = Mathf.Min(_playersFighting.Value + 1, maxPlayers);
        Debug.Log($"[MultiplayerGameManager] Jogador revivido. Em combate: {_playersFighting.Value}");
    }

    public void RegisterPlayerDeath() => RegisterPlayerDowned();
    public void RegisterPlayerRespawn() => RegisterPlayerRevived();

    [Rpc(SendTo.Server)]
    public void RequestPauseRpc()
    {
        if (_networkGameState.Value != GameState.Playing) return;
        if (_resumeCountdownRoutine != null) return;

        _networkGameState.Value = GameState.Paused;
        Time.timeScale = 0f;
        GameEvents.InvokePauseChanged(true);
        ApplyPauseClientRpc(true);
    }

    [Rpc(SendTo.Server)]
    public void RequestResumeRpc()
    {
        if (_networkGameState.Value != GameState.Paused) return;
        if (_resumeCountdownRoutine != null) return;

        _resumeCountdownRoutine = StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        for (int seconds = 3; seconds >= 1; seconds--)
        {
            _resumeCountdown.Value = seconds;
            BroadcastResumeCountdownClientRpc(seconds);
            yield return new WaitForSecondsRealtime(1f);
        }

        _resumeCountdown.Value = 0;
        BroadcastResumeCountdownClientRpc(0);

        _networkGameState.Value = GameState.Playing;
        _resumeCountdown.Value = -1;
        GameEvents.InvokePauseChanged(false);
        ApplyPauseClientRpc(false);

        _resumeCountdownRoutine = null;
    }

    [ClientRpc]
    private void BroadcastResumeCountdownClientRpc(int seconds)
    {
        GameManager2 gameManager = FindFirstObjectByType<GameManager2>();
        gameManager?.ShowResumeCountdown(seconds);
    }

    /// <summary>
    /// ClientRpc apenas aplica Time.timeScale e dispara o evento local de pause.
    /// A NetworkVariable já foi alterada no servidor antes desta chamada.
    /// </summary>
    [ClientRpc]
    private void ApplyPauseClientRpc(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        if (!IsServer)
            GameEvents.InvokePauseChanged(paused);
        else if (!paused)
            GameEvents.InvokePauseChanged(false);

        GameFlowOrchestrator.Instance?.NotifyPauseChanged(paused);

        GameManager2 localGameManager = FindFirstObjectByType<GameManager2>();
        if (localGameManager != null)
        {
            if (paused)
            {
                localGameManager.ShowPauseOverlay();
            }
            else
            {
                localGameManager.HideResumeCountdown();
                localGameManager.HidePauseOverlay();
            }
        }
        else
        {
            SceneOverlayController overlay = FindFirstObjectByType<SceneOverlayController>();
            if (overlay != null)
            {
                if (paused)
                    overlay.OpenOverlay("pause");
                else
                    overlay.CloseOverlay("pause");
            }
        }

        Debug.Log($"[MultiplayerGameManager] Jogo {(paused ? "pausado" : "retomado")} em todos os clientes.");
    }

    private void HandleNightEnded()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
            return;

        if (IsVictoryTransitionComplete())
            return;

        if (_victorySequenceStarted)
            return;

        BeginVictoryTransition();
    }

    private void BeginVictoryTransition()
    {
        _victorySequenceStarted = true;

        if (IsSpawned)
            StartCoroutine(TriggerVictoryRoutine());
        else
            StartCoroutine(TriggerVictoryWhenSpawnedRoutine());
    }

    private bool IsVictoryTransitionComplete()
    {
        return TryGetSafeGameState(out GameState state) && state == GameState.Victory;
    }

    private bool TryGetSafeGameState(out GameState state)
    {
        state = GameState.WaitingForPlayers;
        if (!IsSpawned)
            return false;

        state = _networkGameState.Value;
        return true;
    }

    private IEnumerator TriggerVictoryWhenSpawnedRoutine()
    {
        const float timeout = 5f;
        float elapsed = 0f;

        while (!IsSpawned && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (IsSpawned)
        {
            yield return TriggerVictoryRoutine();
            yield break;
        }

        Debug.LogWarning("[MultiplayerGameManager] Vitória sem NetworkObject spawnado — fallback local.");
        if (ScreenFlowStateMachine.ShowVictoryScreen())
            yield break;

        SaveProfileStore.Instance?.MarkActiveContractCompleted();
        GameSessionContext.ResetContractRound();
    }

    private void HandlePlayersFightingChanged(int oldValue, int newValue)
    {
        if (!IsServer) return;
        if (newValue <= 0 && oldValue > newValue)
            TryBeginDefeatSequence();
    }

    private void TryBeginDefeatSequence()
    {
        if (_defeatSequenceStarted || !IsServer) return;
        if (_networkGameState.Value != GameState.Playing) return;
        if (_playersFighting.Value > 0) return;

        _defeatSequenceStarted = true;
        StopWaveSpawningOnServer();
        PlayDefeatAmbienceClientRpc();
        StartCoroutine(TriggerDefeatRoutine());
    }

    private static void StopWaveSpawningOnServer()
    {
        NetworkWaveManager waveManager = NetworkWaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.StopSpawning();
            return;
        }

        StopLocalWaveSystemsIfPresent();
    }

    [ClientRpc]
    private void PlayDefeatAmbienceClientRpc()
    {
        StartCoroutine(PlayDefeatAmbienceWhenReady());
    }

    private IEnumerator PlayDefeatAmbienceWhenReady()
    {
        const float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (NetworkPlayerHealth.TryGetLastDownedFocusTarget(out Transform focus))
            {
                float defeatUiDelay = DefeatPresentationTiming.ResolveMaxDefeatUiDelayForUnconsciousPlayers();
                DeathHordePresentation.TryBeginFinalDefeat(this, focus, null, defeatUiDelay);
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator TriggerVictoryRoutine()
    {
        float delay = gameConfig != null ? gameConfig.victoryDelay : 2f;
        yield return new WaitForSecondsRealtime(delay);

        if (IsSpawned)
        {
            _networkGameState.Value = GameState.Victory;
            yield break;
        }

        yield return ForceVictoryFallbackRoutine();
    }

    private IEnumerator ForceVictoryFallbackRoutine()
    {
        float delay = gameConfig != null ? gameConfig.victoryDelay : 2f;
        yield return new WaitForSecondsRealtime(delay);

        if (IsVictoryTransitionComplete())
            yield break;

        if (IsSpawned)
        {
            _networkGameState.Value = GameState.Victory;
            yield break;
        }

        Debug.LogWarning("[MultiplayerGameManager] Vitória — fallback direto para tela de vitória.");
        Time.timeScale = 1f;
        SaveProfileStore.Instance?.MarkActiveContractCompleted();
        GameSessionContext.ResetContractRound();
        ScreenFlowStateMachine.ShowVictoryScreen();
    }

    private IEnumerator TriggerDefeatRoutine()
    {
        float delay = ResolveDefeatPresentationDelay();
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        Time.timeScale = 1f;
        _networkGameState.Value = GameState.Defeat;
    }

    private float ResolveDefeatPresentationDelay()
    {
        float delay = DefeatPresentationTiming.ResolveMaxDefeatUiDelayForUnconsciousPlayers();

        if (delay <= 0f)
            delay = gameConfig != null ? gameConfig.defeatDelay : 2f;

        return Mathf.Max(delay, 0.5f);
    }

    private static void StopLocalWaveSystemsIfPresent()
    {
        var nightManager = Object.FindFirstObjectByType<NightManager>(FindObjectsInactive.Include);
        if (nightManager != null)
            nightManager.ForceStop();

        var waveGenerator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        if (waveGenerator != null)
            waveGenerator.StopSpawning();
    }

    private void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.Playing && IsServer)
            StopLocalWaveSystemsIfPresent();

        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[MultiplayerGameManager] Estado: {oldState} → {newState}");

        switch (newState)
        {
            case GameState.Victory: OnVictory?.Invoke(); break;
            case GameState.Defeat:  OnDefeat?.Invoke();  break;
        }

        if (IsServer && (newState == GameState.Victory || newState == GameState.Defeat))
        {
            if (newState == GameState.Victory)
                SaveProfileStore.Instance?.MarkActiveContractCompleted();

            ReturnToPreparationOnServer(newState);
        }
    }

    private void ReturnToPreparationOnServer(GameState endState)
    {
        Time.timeScale = 1f;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null)
        {
            save.Active.Touch(NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost,
                ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty,
                "Preparation");
            save.SaveActive();
        }

        GameSessionContext.ResetContractRound();

        BeginEndGameScreenTransitionClientRpc(endState);
    }

    [ClientRpc]
    private void BeginEndGameScreenTransitionClientRpc(GameState endState)
    {
        if (endState == GameState.Victory)
            ScreenFlowStateMachine.ShowVictoryScreen();
        else
            ScreenFlowStateMachine.ShowDefeatScreen();
    }
}

public enum GameState
{
    WaitingForPlayers,
    Playing,
    Paused,
    Victory,
    Defeat
}
