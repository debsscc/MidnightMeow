using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawna os session managers do hub Preparation/Characters como NetworkObjects registrados,
/// replicados a todos os clientes e persistentes (DDOL) entre cargas locais de cena.
/// </summary>
public static class HubSessionNetworkSpawner
{
    public static void EnsureSpawned()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
            return;

        HubSessionPrefabCatalog catalog = HubSessionPrefabCatalog.LoadCached();
        if (catalog == null)
        {
            Debug.LogError("[HubSessionNetworkSpawner] HubSessionPrefabCatalog não encontrado em Resources/HubSessionPrefabCatalog.");
            return;
        }

        EnsureSpawned<PreparationSessionManager>(catalog.preparationSessionManagerPrefab);
        EnsureSpawned<CharactersSessionManager>(catalog.charactersSessionManagerPrefab);
    }

    private static void EnsureSpawned<T>(GameObject prefab) where T : NetworkBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null)
            return;

        if (prefab == null)
        {
            Debug.LogError($"[HubSessionNetworkSpawner] Prefab ausente para {typeof(T).Name} no HubSessionPrefabCatalog.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[HubSessionNetworkSpawner] Prefab '{prefab.name}' sem NetworkObject.");
            Object.Destroy(instance);
            return;
        }

        if (!networkObject.IsSpawned)
            networkObject.Spawn(destroyWithScene: false);

        Debug.Log($"[HubSessionNetworkSpawner] Spawned {typeof(T).Name} (NetworkObjectId={networkObject.NetworkObjectId}).");
    }
}
