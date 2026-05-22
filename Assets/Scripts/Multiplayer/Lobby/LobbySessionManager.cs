/// <summary>
/// Gerencia o estado do lobby em rede (jogadores conectados, seleção de personagem,
/// status de pronto e início da partida). O servidor é autoritativo.
/// </summary>
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LobbySessionManager : NetworkBehaviour
{
    public static LobbySessionManager Instance { get; private set; }

    [Header("Fluxo de cenas")]
    [SerializeField] private string gameplaySceneName = "Fase-1";
    [SerializeField] private int minimumPlayersToStart = 1;

    private readonly NetworkList<LobbyPlayerState> _players = new NetworkList<LobbyPlayerState>();
    private readonly NetworkVariable<FixedString32Bytes> _joinCode = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public event Action OnLobbyPlayersChanged;
    public event Action<string> OnJoinCodeChanged;
    public event Action<string> OnLobbyError;

    public NetworkList<LobbyPlayerState> Players => _players;
    public string CurrentJoinCode => _joinCode.Value.ToString();
    public bool CanStartMatch => IsServer && _players.Count >= minimumPlayersToStart && AreAllPlayersReady();

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
        _players.OnListChanged += HandlePlayerListChanged;
        _joinCode.OnValueChanged += HandleJoinCodeChanged;

        if (IsServer && NetworkManager != null)
        {
            SyncJoinCodeFromConnectionManager();
            SyncConnectedClientsAsLobbyPlayers();
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        OnLobbyPlayersChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        _players.OnListChanged -= HandlePlayerListChanged;
        _joinCode.OnValueChanged -= HandleJoinCodeChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }

    [Rpc(SendTo.Server)]
    public void RequestSetCharacterRpc(byte characterType, RpcParams rpcParams = default)
    {
        ulong callerId = rpcParams.Receive.SenderClientId;
        if (!Enum.IsDefined(typeof(LobbyCharacterType), characterType))
        {
            OnLobbyError?.Invoke($"Tipo de personagem inválido recebido: {characterType}.");
            return;
        }

        SetPlayerCharacter(callerId, (LobbyCharacterType)characterType);
    }

    [Rpc(SendTo.Server)]
    public void RequestSetReadyRpc(bool isReady, RpcParams rpcParams = default)
    {
        ulong callerId = rpcParams.Receive.SenderClientId;
        SetPlayerReady(callerId, isReady);
    }

    [Rpc(SendTo.Server)]
    public void RequestStartGameRpc(RpcParams rpcParams = default)
    {
        ulong callerId = rpcParams.Receive.SenderClientId;
        if (!IsServer || NetworkManager == null) return;

        if (callerId != NetworkManager.ServerClientId)
        {
            OnLobbyError?.Invoke("Apenas o host pode iniciar a partida.");
            return;
        }

        if (!CanStartMatch)
        {
            OnLobbyError?.Invoke("Nem todos os jogadores estao prontos.");
            return;
        }

        LobbySelectionStore.Capture(_players);
        NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public bool TryGetPlayerState(ulong clientId, out LobbyPlayerState state)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
            {
                state = _players[i];
                return true;
            }
        }

        state = default;
        return false;
    }

    private void SyncJoinCodeFromConnectionManager()
    {
        if (ConnectionManager.Instance == null) return;
        string code = ConnectionManager.Instance.CurrentJoinCode;
        if (!string.IsNullOrEmpty(code))
        {
            _joinCode.Value = new FixedString32Bytes(code);
        }
    }

    private void SyncConnectedClientsAsLobbyPlayers()
    {
        if (!IsServer || NetworkManager == null) return;

        _players.Clear();
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            _players.Add(CreateDefaultPlayerState(clientId));
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (TryGetPlayerState(clientId, out _)) return;
        _players.Add(CreateDefaultPlayerState(clientId));
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        int index = FindPlayerIndex(clientId);
        if (index >= 0)
        {
            _players.RemoveAt(index);
        }
    }

    private void SetPlayerCharacter(ulong clientId, LobbyCharacterType type)
    {
        int index = FindPlayerIndex(clientId);
        if (index < 0) return;

        LobbyPlayerState current = _players[index];
        current.CharacterType = type;
        _players[index] = current;
    }

    private void SetPlayerReady(ulong clientId, bool isReady)
    {
        int index = FindPlayerIndex(clientId);
        if (index < 0) return;

        LobbyPlayerState current = _players[index];
        current.IsReady = isReady;
        _players[index] = current;
    }

    private LobbyPlayerState CreateDefaultPlayerState(ulong clientId)
    {
        return new LobbyPlayerState
        {
            ClientId = clientId,
            CharacterType = LobbyCharacterType.Default,
            IsReady = false,
            DisplayName = new FixedString32Bytes($"Jogador {clientId + 1}")
        };
    }

    private int FindPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool AreAllPlayersReady()
    {
        if (_players.Count == 0) return false;
        for (int i = 0; i < _players.Count; i++)
        {
            if (!_players[i].IsReady)
            {
                return false;
            }
        }

        return true;
    }

    private void HandlePlayerListChanged(NetworkListEvent<LobbyPlayerState> _)
    {
        OnLobbyPlayersChanged?.Invoke();
    }

    private void HandleJoinCodeChanged(FixedString32Bytes _, FixedString32Bytes current)
    {
        OnJoinCodeChanged?.Invoke(current.ToString());
    }
}
