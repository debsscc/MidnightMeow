using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante carruagem spawnada (servidor) e configurada (todos os peers) na Fase-2.
/// </summary>
public sealed class NetworkCarriageSpawner : MonoBehaviour
{
    private const string CarriagePrefabPath = "Assets/Prefabs/Gameplay/Carriage.prefab";
    private const float SetupTimeoutSeconds = 30f;
    private const float PollIntervalSeconds = 0.1f;

    private static bool _scheduled;

    public static void EnsureCarriageSpawned()
    {
        if (SceneManager.GetActiveScene().name != "Fase-2")
            return;

        if (_scheduled)
            return;

        _scheduled = true;
        var host = new GameObject(nameof(NetworkCarriageSpawner));
        host.AddComponent<NetworkCarriageSpawner>();
    }

    private void OnDestroy()
    {
        _scheduled = false;
    }

    private void OnEnable()
    {
        StartCoroutine(SetupRoutine());
    }

    private IEnumerator SetupRoutine()
    {
        float waited = 0f;
        while (waited < SetupTimeoutSeconds)
        {
            NetworkCarriage carriage = Object.FindFirstObjectByType<NetworkCarriage>(FindObjectsInactive.Include);
            NetworkManager networkManager = NetworkManager.Singleton;
            bool isServer = networkManager != null && networkManager.IsServer;

            if (carriage == null && isServer)
                carriage = TryInstantiateCarriage();

            if (carriage != null)
            {
                PhaseGameplayContentInstaller.ConfigureCarriage(carriage);

                if (isServer)
                {
                    NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
                    if (networkObject != null && !networkObject.IsSpawned)
                        networkObject.Spawn(true);
                }

                if (IsCarriageReady(carriage))
                {
                    Debug.Log("[NetworkCarriageSpawner] Carruagem pronta (path + spawn).");
                    Destroy(gameObject);
                    yield break;
                }
            }

            waited += PollIntervalSeconds;
            yield return new WaitForSeconds(PollIntervalSeconds);
        }

        Debug.LogWarning("[NetworkCarriageSpawner] Timeout configurando carruagem — verifique host/solo e CarriageConfig.");
        Destroy(gameObject);
    }

    private static bool IsCarriageReady(NetworkCarriage carriage)
    {
        if (carriage == null || carriage.Path == null || carriage.Path.WaypointCount < 2)
            return false;

        NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned)
            return false;

        return true;
    }

    private static NetworkCarriage TryInstantiateCarriage()
    {
        GameObject prefab = ResolveCarriagePrefab();
        if (prefab == null)
        {
            Debug.LogError("[NetworkCarriageSpawner] Prefab Carriage não encontrado (GameplayPrefabCatalog).");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = "Carriage";
        NetworkCarriage carriage = instance.GetComponent<NetworkCarriage>();
        if (carriage != null)
            return carriage;

        Debug.LogError("[NetworkCarriageSpawner] Prefab sem NetworkCarriage.");
        Object.Destroy(instance);
        return null;
    }

    private static GameObject ResolveCarriagePrefab()
    {
        GameplayPrefabCatalog catalog = GameplayPrefabCatalog.LoadCached();
        if (catalog != null && catalog.carriagePrefab != null)
            return catalog.carriagePrefab;

#if UNITY_EDITOR
        GameObject editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CarriagePrefabPath);
        if (editorPrefab != null)
            return editorPrefab;
#endif

        return null;
    }
}

