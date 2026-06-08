using System;

using Unity.Collections;

using Unity.Netcode;

using UnityEngine;



/// <summary>

/// Estado replicado da tela Characters: personagem escolhido (sincronizado com Preparation).

/// </summary>

[DisallowMultipleComponent]

public class CharactersSessionManager : NetworkBehaviour

{

    public static CharactersSessionManager Instance { get; private set; }



    private readonly NetworkList<CharactersPlayerState> _players = new NetworkList<CharactersPlayerState>();



    public event Action OnCharactersStateChanged;

    public event Action<string> OnCharactersFeedback;



    public NetworkList<CharactersPlayerState> Players => _players;



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

        _players.OnListChanged += HandleListChanged;



        if (IsServer && NetworkManager != null)

        {

            SyncFromPreparationOrConnected();

            NetworkManager.OnClientConnectedCallback += HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        }



        OnCharactersStateChanged?.Invoke();

    }



    public override void OnNetworkDespawn()

    {

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

    public void RequestSetCharacterRpc(byte characterType, RpcParams rpcParams = default)

    {

        ulong caller = rpcParams.Receive.SenderClientId;

        if (!Enum.IsDefined(typeof(LobbyCharacterType), characterType))

            return;



        var type = (LobbyCharacterType)characterType;

        if (type == LobbyCharacterType.Default)

            return;



        if (IsCharacterTakenByOther(caller, type))

        {

            NotifyFeedbackClientRpc("Este personagem já foi escolhido por outro jogador.", CreateTargetClientParams(caller));

            return;

        }



        int index = FindPlayerIndex(caller);

        if (index < 0)

            return;



        CharactersPlayerState state = _players[index];

        state.CharacterType = type;

        _players[index] = state;



        PreparationSessionManager prep = PreparationSessionManager.Instance;
        prep?.TrySetCharacter(caller, type, notifyOnError: false);



        ApplyCharacterToSave(caller, type);

    }



    [ClientRpc]

    private void NotifyFeedbackClientRpc(string message, ClientRpcParams clientRpcParams = default)

    {

        OnCharactersFeedback?.Invoke(message);

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



    private void ApplyCharacterToSave(ulong clientId, LobbyCharacterType type)

    {

        if (NetworkManager == null || clientId != NetworkManager.LocalClientId)

            return;



        SaveProfileStore save = SaveProfileStore.Instance;

        save?.SetSelectedCharacter(type);

        save?.SaveActive();

    }



    private void SyncFromPreparationOrConnected()

    {

        _players.Clear();



        PreparationSessionManager prep = PreparationSessionManager.Instance;

        if (prep != null && prep.Players.Count > 0)

        {

            for (int i = 0; i < prep.Players.Count; i++)

            {

                PreparationPlayerState p = prep.Players[i];

                _players.Add(new CharactersPlayerState

                {

                    ClientId = p.ClientId,

                    CharacterType = p.CharacterType,

                    DisplayName = p.DisplayName

                });

            }



            return;

        }



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



    private static CharactersPlayerState CreateDefault(ulong clientId)

    {

        return new CharactersPlayerState

        {

            ClientId = clientId,

            CharacterType = LobbyCharacterType.Default,

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



    private void HandleListChanged(NetworkListEvent<CharactersPlayerState> _) =>

        OnCharactersStateChanged?.Invoke();

}



public struct CharactersPlayerState : INetworkSerializable, IEquatable<CharactersPlayerState>

{

    public ulong ClientId;

    public FixedString32Bytes DisplayName;

    public LobbyCharacterType CharacterType;



    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter

    {

        serializer.SerializeValue(ref ClientId);

        serializer.SerializeValue(ref DisplayName);

        serializer.SerializeValue(ref CharacterType);

    }



    public bool Equals(CharactersPlayerState other) =>

        ClientId == other.ClientId

        && DisplayName.Equals(other.DisplayName)

        && CharacterType == other.CharacterType;

}


