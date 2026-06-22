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
    [Tooltip("Ao spawnar nesta cena (servidor), inicia Playing automaticamente para o NetworkWaveManager.")]
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

    private bool _defeatSequenceStarted;

    public GameState CurrentState => _networkGameState.Value;
    public int PlayersFighting => _playersFighting.Value;
    public int PlayersAlive => _playersFighting.Value;

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
    }

    public override void OnNetworkSpawn()
    {
        _networkGameState.OnValueChanged += HandleGameStateChanged;
        _playersFighting.OnValueChanged += HandlePlayersFightingChanged;

        if (IsServer)
        {
            GameEvents.OnNightEnded += HandleNightEnded;
            TryAutoBeginGameplayOnServer();
        }

        Debug.Log($"[MultiplayerGameManager] Spawned. IsServer={IsServer}, IsHost={IsHost}");
    }

    public override void OnNetworkDespawn()
    {
        _networkGameState.OnValueChanged -= HandleGameStateChanged;
        _playersFighting.OnValueChanged -= HandlePlayersFightingChanged;

        if (IsServer)
            GameEvents.OnNightEnded -= HandleNightEnded;
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
        if (activeScene != gameplaySceneName) return;

        ServerBeginGameplaySession();
    }

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
        // Muda o estado no servidor ANTES do ClientRpc para evitar bug de write em cliente
        _networkGameState.Value = GameState.Paused;
        ApplyPauseClientRpc(true);
    }

    [Rpc(SendTo.Server)]
    public void RequestResumeRpc()
    {
        if (_networkGameState.Value != GameState.Paused) return;
        _networkGameState.Value = GameState.Playing;
        ApplyPauseClientRpc(false);
    }

    /// <summary>
    /// ClientRpc apenas aplica Time.timeScale e dispara o evento local de pause.
    /// A NetworkVariable já foi alterada no servidor antes desta chamada.
    /// </summary>
    [ClientRpc]
    private void ApplyPauseClientRpc(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        GameEvents.InvokePauseChanged(paused);

        GameFlowOrchestrator.Instance?.NotifyPauseChanged(paused);

        GameManager2 localGameManager = FindFirstObjectByType<GameManager2>();
        if (localGameManager != null)
        {
            if (paused)
                localGameManager.ShowPauseOverlay();
            else
                localGameManager.HidePauseOverlay();
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
        if (!IsServer) return;
        StartCoroutine(TriggerVictoryRoutine());
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
        yield return new WaitForSeconds(delay);
        _networkGameState.Value = GameState.Victory;
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
            ReturnToPreparationOnServer(newState);
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
