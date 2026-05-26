/// <summary>
/// Armazena selecoes de personagem do lobby entre trocas de cena.
/// Evita acoplamento do PlayerSpawnManager com objetos de lobby que podem ser descarregados.
/// </summary>
using System.Collections.Generic;
using Unity.Netcode;

public static class LobbySelectionStore
{
    private static readonly Dictionary<ulong, LobbyCharacterType> _characterByClientId = new Dictionary<ulong, LobbyCharacterType>();

    public static void Capture(IEnumerable<LobbyPlayerState> players)
    {
        _characterByClientId.Clear();
        foreach (var player in players)
        {
            _characterByClientId[player.ClientId] = player.CharacterType;
        }
    }

    public static void Capture(NetworkList<LobbyPlayerState> players)
    {
        _characterByClientId.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            _characterByClientId[player.ClientId] = player.CharacterType;
        }
    }

    public static bool TryGetCharacter(ulong clientId, out LobbyCharacterType characterType)
    {
        return _characterByClientId.TryGetValue(clientId, out characterType);
    }

    public static void Clear()
    {
        _characterByClientId.Clear();
    }
}
