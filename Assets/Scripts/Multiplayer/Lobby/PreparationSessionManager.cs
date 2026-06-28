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

    private readonly NetworkVariable<bool> _contractConfirmed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> _startCountdown = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkList<PreparationPlayerState> _players = new NetworkList<PreparationPlayerState>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    public event Action OnPreparationStateChanged;

    public int SelectedContractIndex
    {
        get
        {
            if (IsServer)
                return _selectedContractIndex.Value;

            return _clientContractIndex >= 0 ? _clientContractIndex : _selectedContractIndex.Value;
        }
    }

    public NetworkList<PreparationPlayerState> Players => _players;

    public bool ContractConfirmed => _contractConfirmed.Value;
    public int StartCountdown => _startCountdown.Value;

    private int _clientContractIndex = -1;
    private Coroutine _countdownCoroutine;

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
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _selectedContractIndex.OnValueChanged += HandleContractChanged;
        _contractConfirmed.OnValueChanged += HandleContractConfirmedChanged;
        _startCountdown.OnValueChanged += HandleCountdownChanged;
        _players.OnListChanged += HandleListChanged;

        if (IsServer && NetworkManager != null)
        {
            SyncConnectedClients();
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            PushCharactersSessionSnapshot();
        }

        if (!IsServer)
            _clientContractIndex = _selectedContractIndex.Value;

        OnInstanceAvailable?.Invoke();
        OnPreparationStateChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        _selectedContractIndex.OnValueChanged -= HandleContractChanged;
        _contractConfirmed.OnValueChanged -= HandleContractConfirmedChanged;
        _startCountdown.OnValueChanged -= HandleCountdownChanged;
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

    public void SetContractIndexOnServer(int contractIndex)
    {
        if (!IsServer)
            return;

        if (contractIndex < 0 || contracts == null || contractIndex >= contracts.Length)
            return;

        _selectedContractIndex.Value = contractIndex;
        ContractSceneResolver.ApplyToSession(contractIndex);
        _contractConfirmed.Value = false;
        _startCountdown.Value = -1;
        ClearAllReady();
        BroadcastHubStateChanged();
    }

    [Rpc(SendTo.Server)]
    public void RequestConfirmContractRpc(RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton != null && caller != NetworkManager.ServerClientId)
            return;

        if (_selectedContractIndex.Value < 0)
        {
            NotifyFeedbackClientRpc("Escolha um contrato antes de confirmar.", CreateTargetClientParams(caller));
            return;
        }

        _contractConfirmed.Value = true;
        _startCountdown.Value = -1;
        ClearAllReady();
        BroadcastHubStateChanged();
        NotifyContractConfirmedClientRpc();
    }

    [ClientRpc]
    private void NotifyContractConfirmedClientRpc()
    {
        ScreenFlowStateMachine.OpenCharactersFromPreparation();
    }

    [Rpc(SendTo.Server)]
    public void RequestSelectContractRpc(int contractIndex, RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton != null && caller != NetworkManager.ServerClientId)
            return;

        SetContractIndexOnServer(contractIndex);
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
        {
            if (_startCountdown.Value < 0)
                BeginStartCountdown();
        }
        else if (!isReady)
        {
            CancelStartCountdown();
        }

        BroadcastHubStateChanged();
    }

    [Rpc(SendTo.Server)]
    public void RequestClearCharacterRpc(RpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        int index = FindPlayerIndex(caller);
        if (index < 0)
            return;

        PreparationPlayerState state = _players[index];
        if (state.CharacterType == LobbyCharacterType.Default)
            return;

        state.CharacterType = LobbyCharacterType.Default;
        state.IsReady = false;
        _players[index] = state;

        CancelStartCountdown();
        PushCharactersSessionSnapshot();
        LobbySelectionStore.CaptureFromPreparation(_players);
        BroadcastHubStateChanged();
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

        int index = EnsurePlayerIndex(clientId);
        if (index < 0)
            return false;

        PreparationPlayerState state = _players[index];

        if (state.CharacterType == type)
        {
            state.CharacterType = LobbyCharacterType.Default;
            state.IsReady = false;
            _players[index] = state;
            CancelStartCountdown();
            PushCharactersSessionSnapshot();
            LobbySelectionStore.CaptureFromPreparation(_players);
            BroadcastHubStateChanged();
            return true;
        }

        if (IsCharacterTakenByOther(clientId, type))
        {
            if (notifyOnError)
                NotifyFeedbackClientRpc("Este personagem já foi escolhido por outro jogador.", CreateTargetClientParams(clientId));
            return false;
        }

        state.CharacterType = type;
        state.IsReady = false;
        _players[index] = state;

        CancelStartCountdown();
        PushCharactersSessionSnapshot();
        ApplyCharacterToSave(clientId, type);
        LobbySelectionStore.CaptureFromPreparation(_players);
        BroadcastHubStateChanged();
        return true;
    }

    [ClientRpc]
    private void NotifyHubStateChangedClientRpc(int contractIndex)
    {
        _clientContractIndex = contractIndex;
        OnPreparationStateChanged?.Invoke();
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

    public bool GetLocalReadyState()
    {
        if (NetworkManager == null)
            return false;

        ulong localId = NetworkManager.LocalClientId;
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == localId)
                return _players[i].IsReady;
        }

        return false;
    }

    public void ResyncPlayerRoster()
    {
        if (!IsServer || NetworkManager == null)
            return;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            EnsurePlayerIndex(clientId);

        PushCharactersSessionSnapshot();
        BroadcastHubStateChanged();
    }

    private string ValidateReady(ulong callerId)
    {
        if (!_contractConfirmed.Value)
            return "Aguarde o host confirmar o contrato.";

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
        _contractConfirmed.Value = false;
        _startCountdown.Value = -1;
        CancelStartCountdown();
        for (int i = 0; i < _players.Count; i++)
        {
            PreparationPlayerState state = _players[i];
            state.CharacterType = LobbyCharacterType.Default;
            state.IsReady = false;
            _players[i] = state;
        }

        GameSessionContext.ResetContractRound();
    }

    private void BeginStartCountdown()
    {
        if (!IsServer || _countdownCoroutine != null)
            return;

        _countdownCoroutine = StartCoroutine(StartCountdownRoutine());
    }

    private void CancelStartCountdown()
    {
        if (!IsServer)
            return;

        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        _startCountdown.Value = -1;
    }

    private System.Collections.IEnumerator StartCountdownRoutine()
    {
        for (int seconds = 5; seconds >= 0; seconds--)
        {
            _startCountdown.Value = seconds;
            OnPreparationStateChanged?.Invoke();
            BroadcastHubStateChanged();

            if (seconds == 0)
                break;

            yield return new WaitForSeconds(1f);
        }

        _countdownCoroutine = null;
        BeginLoading2();
    }

    private void BeginLoading2()
    {
        if (!IsServer)
            return;

        LobbySelectionStore.CaptureFromPreparation(_players);

        string gameplayScene = ApplySelectedContractToSession();
        SyncGameplaySceneClientRpc(new FixedString64Bytes(gameplayScene));
        ScreenFlowStateMachine.BeginGameplayLoading();
    }

    private string ApplySelectedContractToSession()
    {
        int index = _selectedContractIndex.Value;
        if (index < 0)
            index = ContractSceneResolver.ResolveActiveContractIndex();

        ContractSceneResolver.ApplyToSession(index);
        return GameSessionContext.ActiveGameplaySceneName;
    }

    [ClientRpc]
    private void SyncGameplaySceneClientRpc(FixedString64Bytes sceneName)
    {
        if (IsServer || sceneName.Length == 0)
            return;

        GameSessionContext.ActiveGameplaySceneName = sceneName.ToString();
        GameSessionContext.ActiveContractIndex = _selectedContractIndex.Value;
    }

    private bool AreAllReady()
    {
        if (!_contractConfirmed.Value)
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
        if (contracts == null || contracts.Length < 3)
            contracts = new ContractDefinition[3];

        ContractSceneResolver.FillMissingSlots(contracts);
    }

    private static ContractDefinition FindContractAsset(string assetName)
    {
        return ContractSceneResolver.ResolveContract(
            assetName == "Contract_1" ? 0 : assetName == "Contract_2" ? 1 : assetName == "Contract_3" ? 2 : -1);
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

    private void HandleContractConfirmedChanged(bool _, bool next)
    {
        OnPreparationStateChanged?.Invoke();
    }

    private void HandleCountdownChanged(int _, int next) => OnPreparationStateChanged?.Invoke();

    private void HandleContractChanged(int _, int next)
    {
        if (!IsServer)
            _clientContractIndex = next;

        OnPreparationStateChanged?.Invoke();
        if (IsServer)
            BroadcastHubStateChanged();
    }

    private void HandleListChanged(NetworkListEvent<PreparationPlayerState> _)
    {
        if (IsServer)
            PushCharactersSessionSnapshot();

        OnPreparationStateChanged?.Invoke();
        if (IsServer)
            BroadcastHubStateChanged();
    }

    private void PushCharactersSessionSnapshot()
    {
        CharactersSessionManager.Instance?.SyncAllFromPreparation(_players);
    }

    private void BroadcastHubStateChanged()
    {
        if (!IsServer)
            return;

        NotifyHubStateChangedClientRpc(_selectedContractIndex.Value);
        CharactersSessionManager.Instance?.NotifyStateChangedFromPreparation();
    }
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
