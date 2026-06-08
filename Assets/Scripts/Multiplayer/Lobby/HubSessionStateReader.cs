using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Leitura unificada do estado do hub Preparation/Characters em multiplayer.
/// </summary>
public static class HubSessionStateReader
{
    public static bool IsHubSessionReady()
    {
        if (GameSessionContext.IsSinglePlayer)
            return true;

        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsListening)
            return false;

        PreparationSessionManager prep = PreparationSessionManager.Instance;
        return prep != null && prep.IsSpawned;
    }

    public static PreparationSessionManager GetPreparationSession()
    {
        PreparationSessionManager prep = PreparationSessionManager.Instance;
        if (prep == null)
            prep = Object.FindFirstObjectByType<PreparationSessionManager>();

        if (prep == null || !prep.IsSpawned)
            return null;

        return prep;
    }

    public static LobbyCharacterType GetLocalCharacterType()
    {
        PreparationSessionManager prep = GetPreparationSession();
        return prep != null ? prep.GetLocalCharacterType() : LobbyCharacterType.Default;
    }

    public static bool IsCharacterTakenByOther(ulong localClientId, LobbyCharacterType type)
    {
        PreparationSessionManager prep = GetPreparationSession();
        return prep != null && prep.IsCharacterTakenByOther(localClientId, type);
    }

    public static ulong? FindCharacterOwnerId(LobbyCharacterType type)
    {
        PreparationSessionManager prep = GetPreparationSession();
        return prep?.FindCharacterOwnerId(type);
    }
}
