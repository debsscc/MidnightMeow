using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawna os session managers do hub Preparation/Characters como NetworkObjects registrados,
/// replicados a todos os clientes e persistentes (DDOL) entre cargas locais de cena.
/// </summary>
public static class HubSessionNetworkSpawner
{
    private const string PreparationPrefabPath = "Multiplayer/PreparationSessionManager";
    private const string CharactersPrefabPath = "Multiplayer/CharactersSessionManager";

    public static void EnsureSpawned()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
            return;

        EnsureSpawned<PreparationSessionManager>(PreparationPrefabPath);
        EnsureSpawned<CharactersSessionManager>(CharactersPrefabPath);
    }

    private static void EnsureSpawned<T>(string resourcePath) where T : NetworkBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[HubSessionNetworkSpawner] Prefab não encontrado em Resources: '{resourcePath}'.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[HubSessionNetworkSpawner] Prefab '{resourcePath}' sem NetworkObject.");
            Object.Destroy(instance);
            return;
        }

        if (!networkObject.IsSpawned)
            networkObject.Spawn();
    }
}
