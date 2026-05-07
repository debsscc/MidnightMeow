/// <summary>
/// Estrutura de dados replicada em rede para representar um jogador no lobby.
/// Contém identificação, nome de exibição e avatar/personagem selecionado.
/// </summary>
using System;
using Unity.Collections;
using Unity.Netcode;

public enum LobbyCharacterType : byte
{
    Default = 0,
    CharacterA = 1,
    CharacterB = 2
}

public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
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

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId
               && DisplayName.Equals(other.DisplayName)
               && CharacterType == other.CharacterType
               && IsReady == other.IsReady;
    }
}
