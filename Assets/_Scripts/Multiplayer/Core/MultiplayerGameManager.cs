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

public class MultiplayerGameManager : NetworkBehaviour
{
    public static MultiplayerGameManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private MultiplayerConfig multiplayerConfig;
    [SerializeField] private GameConfig gameConfig;

    private NetworkVariable<GameState> _networkGameState = new NetworkVariable<GameState>(
        GameState.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> _playersAlive = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public GameState CurrentState => _networkGameState.Value;
    public int PlayersAlive => _playersAlive.Value;

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
        _playersAlive.OnValueChanged += HandlePlayersAliveChanged;

        if (IsServer)
            GameEvents.OnNightEnded += HandleNightEnded;

        Debug.Log($"[MultiplayerGameManager] Spawned. IsServer={IsServer}, IsHost={IsHost}");
    }

    public override void OnNetworkDespawn()
    {
        _networkGameState.OnValueChanged -= HandleGameStateChanged;
        _playersAlive.OnValueChanged -= HandlePlayersAliveChanged;

        if (IsServer)
            GameEvents.OnNightEnded -= HandleNightEnded;
    }

    /// <summary>
    /// Inicia a partida. Chamado pelo host após todos os jogadores estarem prontos.
    /// Rpc: executa no servidor; qualquer cliente pode invocar.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestStartGameRpc()
    {
        if (_networkGameState.Value != GameState.WaitingForPlayers) return;

        _playersAlive.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        _networkGameState.Value = GameState.Playing;
        Debug.Log($"[MultiplayerGameManager] Jogo iniciado com {_playersAlive.Value} jogadores!");
    }

    public void RegisterPlayerDeath()
    {
        if (!IsServer) return;
        _playersAlive.Value = Mathf.Max(0, _playersAlive.Value - 1);
        Debug.Log($"[MultiplayerGameManager] Jogador morreu. Restantes: {_playersAlive.Value}");
    }

    public void RegisterPlayerRespawn()
    {
        if (!IsServer) return;
        int maxPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
        _playersAlive.Value = Mathf.Min(_playersAlive.Value + 1, maxPlayers);
    }

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
        Debug.Log($"[MultiplayerGameManager] Jogo {(paused ? "pausado" : "retomado")} em todos os clientes.");
    }

    private void HandleNightEnded()
    {
        if (!IsServer) return;
        StartCoroutine(TriggerVictoryRoutine());
    }

    private void HandlePlayersAliveChanged(int oldValue, int newValue)
    {
        if (!IsServer) return;
        if (newValue <= 0 && _networkGameState.Value == GameState.Playing)
            StartCoroutine(TriggerDefeatRoutine());
    }

    private IEnumerator TriggerVictoryRoutine()
    {
        float delay = gameConfig != null ? gameConfig.victoryDelay : 2f;
        yield return new WaitForSeconds(delay);
        _networkGameState.Value = GameState.Victory;
    }

    private IEnumerator TriggerDefeatRoutine()
    {
        float delay = gameConfig != null ? gameConfig.defeatDelay : 2f;
        yield return new WaitForSeconds(delay);
        _networkGameState.Value = GameState.Defeat;
    }

    private void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[MultiplayerGameManager] Estado: {oldState} → {newState}");

        switch (newState)
        {
            case GameState.Victory: OnVictory?.Invoke(); break;
            case GameState.Defeat:  OnDefeat?.Invoke();  break;
        }
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
