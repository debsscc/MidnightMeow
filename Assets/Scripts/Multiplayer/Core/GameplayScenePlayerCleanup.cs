using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Remove personagens colocados manualmente na cena de gameplay (ex.: Nixie/Cora em Fase-2)
/// que não são o <see cref="NetworkManager.ConnectedClients"/> PlayerObject.
/// </summary>
public static class GameplayScenePlayerCleanup
{
    public static void RemoveOrphanScenePlayers()
    {
        if (!GameplaySceneBootstrap.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            return;

        HashSet<NetworkObject> keep = CollectTrackedPlayerObjects();
        NetworkPlayerHealth[] candidates = Object.FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        NetworkManager networkManager = NetworkManager.Singleton;
        bool isServer = networkManager != null && networkManager.IsServer;

        for (int i = 0; i < candidates.Length; i++)
        {
            NetworkPlayerHealth health = candidates[i];
            if (health == null)
                continue;

            NetworkObject netObj = health.GetComponent<NetworkObject>();
            if (netObj != null && keep.Contains(netObj))
                continue;

            if (isServer)
            {
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
                else
                    Object.Destroy(health.gameObject);
            }
            else
            {
                health.gameObject.SetActive(false);
            }
        }
    }

    private static HashSet<NetworkObject> CollectTrackedPlayerObjects()
    {
        var keep = new HashSet<NetworkObject>();
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return keep;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
                continue;

            if (client.PlayerObject != null)
                keep.Add(client.PlayerObject);
        }

        return keep;
    }
}
