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

    public static void CaptureFromCharacters(NetworkList<CharactersPlayerState> players)
    {
        _characterByClientId.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            _characterByClientId[player.ClientId] = player.CharacterType;
        }
    }

    public static void CaptureFromPreparation(NetworkList<PreparationPlayerState> players)
    {
        _characterByClientId.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.CharacterType != LobbyCharacterType.Default)
                _characterByClientId[player.ClientId] = player.CharacterType;
        }
    }

    /// <summary>
    /// Mescla escolhas da sessão Characters sem apagar o que já veio da Preparação.
    /// </summary>
    public static void MergeFromCharacters(NetworkList<CharactersPlayerState> players)
    {
        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.CharacterType == LobbyCharacterType.Default)
                continue;

            if (!_characterByClientId.ContainsKey(player.ClientId)
                || _characterByClientId[player.ClientId] == LobbyCharacterType.Default)
            {
                _characterByClientId[player.ClientId] = player.CharacterType;
            }
        }
    }

    public static void CaptureSinglePlayer(LobbyCharacterType characterType)
    {
        _characterByClientId.Clear();
        _characterByClientId[0] = characterType;
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
