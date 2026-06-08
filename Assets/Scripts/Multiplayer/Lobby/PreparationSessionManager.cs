using System;
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
    public static event Action OnInstanceAvailable;

    [SerializeField] private ContractDefinition[] contracts;
    [SerializeField] private int minimumPlayersToProceed = 2;

    public event Action<string> OnPreparationFeedback;

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
        ResolveContracts();
        OnInstanceAvailable?.Invoke();
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
        ulong caller = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton != null && caller != NetworkManager.ServerClientId)
            return;

        if (contractIndex < 0 || contracts == null || contractIndex >= contracts.Length)
            return;

        _selectedContractIndex.Value = contractIndex;
        ClearAllReady();
    }

    [Rpc(SendTo.Server)]
    public void RequestSetReadyRpc(bool isReady, RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        int index = EnsurePlayerIndex(caller);
        if (index < 0)
            return;

        if (isReady)
        {
            string error = ValidateReady(caller);
            if (!string.IsNullOrEmpty(error))
            {
                NotifyFeedbackClientRpc(error, CreateTargetClientParams(caller));
                return;
            }
        }

        PreparationPlayerState state = _players[index];
        state.IsReady = isReady;
        _players[index] = state;

        if (isReady && AreAllReady())
            BeginLoading2();
    }

    [Rpc(SendTo.Server)]
    public void RequestSetCharacterRpc(byte characterType, RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        if (!Enum.IsDefined(typeof(LobbyCharacterType), characterType))
            return;

        TrySetCharacter(caller, (LobbyCharacterType)characterType, notifyOnError: true);
    }

    public bool TrySetCharacter(ulong clientId, LobbyCharacterType type, bool notifyOnError)
    {
        if (type == LobbyCharacterType.Default)
            return false;

        if (IsCharacterTakenByOther(clientId, type))
        {
            if (notifyOnError)
                NotifyFeedbackClientRpc("Este personagem já foi escolhido por outro jogador.", CreateTargetClientParams(clientId));
            return false;
        }

        int index = EnsurePlayerIndex(clientId);
        if (index < 0)
            return false;

        PreparationPlayerState state = _players[index];
        state.CharacterType = type;
        state.IsReady = false;
        _players[index] = state;

        CharactersSessionManager.Instance?.SyncPlayerCharacter(clientId, type);
        ApplyCharacterToSave(clientId, type);
        LobbySelectionStore.CaptureFromPreparation(_players);
        return true;
    }

    [ClientRpc]
    private void NotifyFeedbackClientRpc(string message, ClientRpcParams clientRpcParams = default)
    {
        OnPreparationFeedback?.Invoke(message);
    }

    private static ClientRpcParams CreateTargetClientParams(ulong clientId) =>
        new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

    public bool IsCharacterTakenByOther(ulong callerId, LobbyCharacterType type)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId != callerId && _players[i].CharacterType == type)
                return true;
        }

        return false;
    }

    public LobbyCharacterType GetLocalCharacterType()
    {
        if (NetworkManager == null)
            return LobbyCharacterType.Default;

        ulong localId = NetworkManager.LocalClientId;
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == localId)
                return _players[i].CharacterType;
        }

        return LobbyCharacterType.Default;
    }

    public ulong? FindCharacterOwnerId(LobbyCharacterType type)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].CharacterType == type)
                return _players[i].ClientId;
        }

        return null;
    }

    private string ValidateReady(ulong callerId)
    {
        if (_selectedContractIndex.Value < 0)
            return "Escolha um contrato antes de confirmar.";

        int index = FindPlayerIndex(callerId);
        if (index < 0)
            return "Aguardando sessão de rede...";

        if (_players[index].CharacterType == LobbyCharacterType.Default)
            return "Escolha um personagem antes de confirmar.";

        int required = GameSessionContext.IsSinglePlayer ? 1 : minimumPlayersToProceed;
        if (_players.Count < required)
            return $"Aguardando {required} jogador(es) conectado(s).";

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].CharacterType == LobbyCharacterType.Default)
                return "Aguardando outro jogador escolher personagem.";
        }

        return string.Empty;
    }

    private void ApplyCharacterToSave(ulong clientId, LobbyCharacterType type)
    {
        if (NetworkManager == null || clientId != NetworkManager.LocalClientId)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        save?.SetSelectedCharacter(type);
        save?.SaveActive();
    }

    public void ResetRound()
    {
        if (!IsServer)
            return;

        _selectedContractIndex.Value = -1;
        for (int i = 0; i < _players.Count; i++)
        {
            PreparationPlayerState state = _players[i];
            state.CharacterType = LobbyCharacterType.Default;
            state.IsReady = false;
            _players[i] = state;
        }

        GameSessionContext.ResetContractRound();
    }

    private void BeginLoading2()
    {
        if (!IsServer)
            return;

        ApplySelectedContractToSession();
        ScreenFlowStateMachine.BeginGameplayLoading();
    }

    private void ApplySelectedContractToSession()
    {
        int index = _selectedContractIndex.Value;
        string sceneName = "Fase-1";

        if (contracts != null && index >= 0 && index < contracts.Length && contracts[index] != null)
            sceneName = contracts[index].gameplaySceneName;

        GameSessionContext.ActiveGameplaySceneName = sceneName;
    }

    private bool AreAllReady()
    {
        if (_selectedContractIndex.Value < 0)
            return false;

        int required = GameSessionContext.IsSinglePlayer ? 1 : minimumPlayersToProceed;
        if (_players.Count < required)
            return false;

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].CharacterType == LobbyCharacterType.Default || !_players[i].IsReady)
                return false;
        }

        return true;
    }

    private void ClearAllReady()
    {
        for (int i = 0; i < _players.Count; i++)
        {
            PreparationPlayerState state = _players[i];
            state.IsReady = false;
            _players[i] = state;
        }
    }

    private void ResolveContracts()
    {
        if (contracts != null && contracts.Length > 0)
            return;

        ContractDefinition[] loaded = Resources.FindObjectsOfTypeAll<ContractDefinition>();
        if (loaded.Length == 0)
            return;

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        contracts = new[] { loaded[0] };
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
                return i;
        }

        return -1;
    }

    private int EnsurePlayerIndex(ulong clientId)
    {
        int index = FindPlayerIndex(clientId);
        if (index >= 0 || !IsServer || NetworkManager == null)
            return index;

        _players.Add(CreateDefault(clientId));
        CharactersSessionManager.Instance?.EnsurePlayerEntry(clientId);
        return FindPlayerIndex(clientId);
    }

    private void HandleContractChanged(int _, int __) => OnPreparationStateChanged?.Invoke();
    private void HandleListChanged(NetworkListEvent<PreparationPlayerState> _) => OnPreparationStateChanged?.Invoke();
}

public struct PreparationPlayerState : INetworkSerializable, IEquatable<PreparationPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes DisplayName;
    public LobbyCharacterType CharacterType;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref CharacterType);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(PreparationPlayerState other) =>
        ClientId == other.ClientId
        && DisplayName.Equals(other.DisplayName)
        && CharacterType == other.CharacterType
        && IsReady == other.IsReady;
}
