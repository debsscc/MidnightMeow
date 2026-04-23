/// <summary>
/// PlayerSpawnManager.cs
/// NetworkBehaviour server-autoritativo responsável por spawnar o prefab de jogador
/// em SpawnPoints designados para cada cliente que se conecta.
/// Separa completamente a lógica de spawn do ConnectionManager e do NetworkManager,
/// permitindo configuração visual dos pontos de spawn diretamente no Editor.
///
/// CONFIGURAÇÃO NO EDITOR:
///   1. Desmarque "Auto Spawn Player" no NetworkManager (ou deixe Player Prefab vazio).
///   2. Adicione o prefab do jogador na lista NetworkPrefabs do NetworkManager.
///   3. Arraste o prefab e os SpawnPoints para os campos deste componente.
///   4. Este GameObject PRECISA de NetworkObject para funcionar como NetworkBehaviour.
///
/// SRP: exclusivamente gerencia o spawning de jogadores na rede.
/// </summary>

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Prefab do Jogador")]
    [Tooltip("Prefab do jogador multiplayer. Deve estar na lista NetworkPrefabs do NetworkManager.")]
    [SerializeField] private GameObject playerNetworkPrefab;

    [Header("Pontos de Spawn")]
    [Tooltip("Posições onde os jogadores surgem. O índice é distribuído por ClientId.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Configuração")]
    [Tooltip("Se verdadeiro, respawna o jogador no spawn point original ao reconectar.")]
    [SerializeField] private bool respawnAtOriginalPoint = true;

    private readonly Dictionary<ulong, NetworkObject> _spawnedPlayers = new Dictionary<ulong, NetworkObject>();
    private readonly Dictionary<ulong, int> _clientSpawnIndex = new Dictionary<ulong, int>();
    private int _nextSpawnIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Debug.Log("[PlayerSpawnManager] Inicializado como servidor. Aguardando jogadores...");

        if (playerNetworkPrefab == null)
            Debug.LogError("[PlayerSpawnManager] ERRO: playerNetworkPrefab não atribuído no Inspector!");

        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning("[PlayerSpawnManager] AVISO: nenhum SpawnPoint configurado. Jogadores surgirão em (0,0,0).");

        // Spawna jogadores que já estão conectados (evita race condition com a ordem do OnNetworkSpawn)
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            SpawnPlayerForClient(clientId);

        NetworkManager.OnClientConnectedCallback    += SpawnPlayerForClient;
        NetworkManager.OnClientDisconnectCallback   += HandleClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback    -= SpawnPlayerForClient;
        NetworkManager.OnClientDisconnectCallback   -= HandleClientDisconnected;
    }

    /// <summary>
    /// Spawna o prefab do jogador para o cliente especificado num SpawnPoint disponível.
    /// Executado apenas no servidor.
    /// </summary>
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        if (_spawnedPlayers.ContainsKey(clientId))
        {
            Debug.Log($"[PlayerSpawnManager] Cliente {clientId} já tem um jogador spawnado. Ignorando.");
            return;
        }

        if (playerNetworkPrefab == null)
        {
            Debug.LogError("[PlayerSpawnManager] Não é possível spawnar: playerNetworkPrefab é nulo.");
            return;
        }

        int spawnIndex = GetSpawnIndexForClient(clientId);
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);

        Debug.Log($"[PlayerSpawnManager] Spawnando jogador para ClientId={clientId} no SpawnPoint[{spawnIndex}] em {spawnPosition}");

        GameObject playerObj = Instantiate(playerNetworkPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError($"[PlayerSpawnManager] O prefab '{playerNetworkPrefab.name}' não tem NetworkObject! " +
                           "Adicione o componente NetworkObject ao prefab do jogador.");
            Destroy(playerObj);
            return;
        }

        // SpawnAsPlayerObject vincula o objeto ao cliente e atribui ownership
        networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        _spawnedPlayers[clientId] = networkObject;

        Debug.Log($"[PlayerSpawnManager] Jogador spawnado com sucesso. ClientId={clientId}, NetworkObjectId={networkObject.NetworkObjectId}");
    }

    /// <summary>
    /// Remove o registro do jogador ao desconectar.
    /// O NetworkObject é destruído automaticamente pelo NGO ao desconectar com destroyWithScene=true.
    /// </summary>
    private void HandleClientDisconnected(ulong clientId)
    {
        if (_spawnedPlayers.ContainsKey(clientId))
        {
            _spawnedPlayers.Remove(clientId);
            Debug.Log($"[PlayerSpawnManager] Registro do jogador removido. ClientId={clientId}");
        }
    }

    /// <summary>
    /// Retorna o SpawnPoint atribuído a um cliente. Distribui por ordem de chegada,
    /// com fallback para round-robin se houver mais clientes que spawn points.
    /// </summary>
    private int GetSpawnIndexForClient(ulong clientId)
    {
        if (_clientSpawnIndex.TryGetValue(clientId, out int existingIndex))
            return existingIndex;

        int index = spawnPoints != null && spawnPoints.Length > 0
            ? _nextSpawnIndex % spawnPoints.Length
            : 0;

        _clientSpawnIndex[clientId] = index;
        _nextSpawnIndex++;
        return index;
    }

    private Vector3 GetSpawnPosition(int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || spawnPoints[spawnIndex] == null)
            return Vector3.zero;
        return spawnPoints[spawnIndex].position;
    }

    private Quaternion GetSpawnRotation(int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || spawnPoints[spawnIndex] == null)
            return Quaternion.identity;
        return spawnPoints[spawnIndex].rotation;
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Retorna o NetworkObject do jogador de um cliente específico.</summary>
    public NetworkObject GetPlayerForClient(ulong clientId) =>
        _spawnedPlayers.TryGetValue(clientId, out var obj) ? obj : null;

    /// <summary>Retorna todos os jogadores spawnados atualmente.</summary>
    public IReadOnlyDictionary<ulong, NetworkObject> GetAllSpawnedPlayers() => _spawnedPlayers;
}
