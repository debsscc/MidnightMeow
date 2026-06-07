using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado replicado da tela de Preparação: contrato selecionado e prontidão dos jogadores.
/// </summary>
[DisallowMultipleComponent]
public class PreparationSessionManager : NetworkBehaviour
{
    public static PreparationSessionManager Instance { get; private set; }

    [SerializeField] private int minimumPlayersToProceed = 2;
    [SerializeField] private string loading2RouteId = SceneFlowRouteIds.PreparationToLoading2;

    private readonly NetworkVariable<int> _selectedContractIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkList<PreparationPlayerState> _players = new NetworkList<PreparationPlayerState>();

    public event Action OnPreparationStateChanged;

    public int SelectedContractIndex => _selectedContractIndex.Value;
    public NetworkList<PreparationPlayerState> Players => _players;

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
        _selectedContractIndex.OnValueChanged += HandleContractChanged;
        _players.OnListChanged += HandleListChanged;

        if (IsServer && NetworkManager != null)
        {
            SyncConnectedClients();
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        OnPreparationStateChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        _selectedContractIndex.OnValueChanged -= HandleContractChanged;
        _players.OnListChanged -= HandleListChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    [Rpc(SendTo.Server)]
    public void RequestSelectContractRpc(int contractIndex, RpcParams rpcParams = default)
    {
        _selectedContractIndex.Value = contractIndex;
    }

    [Rpc(SendTo.Server)]
    public void RequestSetReadyRpc(bool isReady, RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        int index = FindPlayerIndex(caller);
        if (index < 0)
            return;

        PreparationPlayerState state = _players[index];
        state.IsReady = isReady;
        _players[index] = state;

        if (AreAllReady())
            BeginLoading2();
    }

    private void BeginLoading2()
    {
        if (!IsServer)
            return;

        GameSessionContext.PendingRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(loading2RouteId);
        else
            ScreenFlowController.Instance?.RequestRoute(loading2RouteId);
    }

    private bool AreAllReady()
    {
        if (_players.Count < minimumPlayersToProceed)
            return false;

        for (int i = 0; i < _players.Count; i++)
        {
            if (!_players[i].IsReady)
                return false;
        }

        return true;
    }

    private void SyncConnectedClients()
    {
        _players.Clear();
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            _players.Add(CreateDefault(clientId));
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (FindPlayerIndex(clientId) >= 0)
            return;
        _players.Add(CreateDefault(clientId));
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        int index = FindPlayerIndex(clientId);
        if (index >= 0)
            _players.RemoveAt(index);
    }

    private static PreparationPlayerState CreateDefault(ulong clientId)
    {
        return new PreparationPlayerState
        {
            ClientId = clientId,
            IsReady = false,
            DisplayName = new FixedString32Bytes($"Jogador {clientId + 1}")
        };
    }

    private int FindPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
                return i;
        }

        return -1;
    }

    private void HandleContractChanged(int _, int __) => OnPreparationStateChanged?.Invoke();
    private void HandleListChanged(NetworkListEvent<PreparationPlayerState> _) => OnPreparationStateChanged?.Invoke();
}

public struct PreparationPlayerState : INetworkSerializable, IEquatable<PreparationPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes DisplayName;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(PreparationPlayerState other) =>
        ClientId == other.ClientId && DisplayName.Equals(other.DisplayName) && IsReady == other.IsReady;
}
