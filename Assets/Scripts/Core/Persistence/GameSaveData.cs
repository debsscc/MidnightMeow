using System;

/// <summary>
/// Snapshot persistido de uma partida / perfil do jogador.
/// </summary>
[Serializable]
public class GameSaveData
{
    public const int MaxSlots = 3;

    public int slotIndex;
    public string saveId = Guid.NewGuid().ToString("N");
    public long lastPlayedUtcTicks;
    public bool wasHost;
    public string lastJoinCode = string.Empty;
    public int magiculas;
    public int selectedContractIndex = -1;
    public string lastSceneName = string.Empty;

    public CharacterSaveData nix = new CharacterSaveData { characterType = LobbyCharacterType.CharacterA };
    public CharacterSaveData cora = new CharacterSaveData { characterType = LobbyCharacterType.CharacterB };

    public LobbyCharacterType SelectedCharacter =>
        nix.characterType != LobbyCharacterType.Default ? nix.characterType : LobbyCharacterType.CharacterA;

    public CharacterSaveData GetCharacterData(LobbyCharacterType type)
    {
        return type == LobbyCharacterType.CharacterB ? cora : nix;
    }

    public void Touch(bool host, string joinCode, string sceneName)
    {
        lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
        wasHost = host;
        if (!string.IsNullOrEmpty(joinCode))
            lastJoinCode = joinCode;
        if (!string.IsNullOrEmpty(sceneName))
            lastSceneName = sceneName;
    }
}
