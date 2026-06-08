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
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : NetworkBehaviour
{
    [System.Serializable]
    private struct CharacterPrefabEntry
    {
        public LobbyCharacterType characterType;
        public GameObject prefab;
    }

    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Prefab do Jogador")]
    [Tooltip("Prefab do jogador multiplayer. Deve estar na lista NetworkPrefabs do NetworkManager.")]
    [SerializeField] private GameObject playerNetworkPrefab;
    [Tooltip("Prefabs alternativos por tipo de personagem selecionado no lobby.")]
    [SerializeField] private CharacterPrefabEntry[] characterPrefabs;

    [Header("Pontos de Spawn")]
    [Tooltip("Posições onde os jogadores surgem. O índice é distribuído por ClientId.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Configuração")]
    [Tooltip("Se verdadeiro, respawna o jogador no spawn point original ao reconectar.")]
#pragma warning disable CS0414 // reservado para fluxo de reconexão futuro
    [SerializeField] private bool respawnAtOriginalPoint = true;
#pragma warning restore CS0414
    [SerializeField] private bool enableDiagnosticsLogs = false;
    [SerializeField] private float spawnRecoveryInterval = 1f;

    private readonly Dictionary<ulong, NetworkObject> _spawnedPlayers = new Dictionary<ulong, NetworkObject>();
    private readonly Dictionary<ulong, int> _clientSpawnIndex = new Dictionary<ulong, int>();
    private int _nextSpawnIndex = 0;
    private Coroutine _spawnRecoveryCoroutine;
    private Coroutine _forceRespawnCoroutine;
    private Coroutine _gameplaySpawnRoutine;
    private bool _spawnedForCurrentGameplayRound;
    [SerializeField] private float gameplaySpawnDelaySeconds = 2.5f;

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

        NetworkManager.OnClientConnectedCallback    += SpawnPlayerForClient;
        NetworkManager.OnClientDisconnectCallback   += HandleClientDisconnected;
        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnSceneEvent += HandleSceneEvent;
        _spawnRecoveryCoroutine = StartCoroutine(EnsurePlayersSpawnedRoutine());
        _forceRespawnCoroutine = StartCoroutine(ReplaceAutoSpawnedPlayersOnceRoutine());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback    -= SpawnPlayerForClient;
        NetworkManager.OnClientDisconnectCallback   -= HandleClientDisconnected;
        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
        if (_spawnRecoveryCoroutine != null)
        {
            StopCoroutine(_spawnRecoveryCoroutine);
            _spawnRecoveryCoroutine = null;
        }
        if (_forceRespawnCoroutine != null)
        {
            StopCoroutine(_forceRespawnCoroutine);
            _forceRespawnCoroutine = null;
        }
        _spawnedPlayers.Clear();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsServer) return;

        if (sceneEvent.SceneEventType == SceneEventType.Load
            && !ShouldReconcilePlayersForScene(sceneEvent.SceneName))
        {
            _spawnedForCurrentGameplayRound = false;
            DespawnAllPlayers();
            return;
        }

        if (!ShouldReconcilePlayersForScene(sceneEvent.SceneName))
            return;

        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            ScheduleGameplaySpawn(waitSeconds: 0f);
            return;
        }

        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted
            && sceneEvent.ClientId == NetworkManager.ServerClientId)
        {
            ScheduleGameplaySpawnFallback();
        }
    }

    private void ScheduleGameplaySpawn(float waitSeconds)
    {
        if (_gameplaySpawnRoutine != null)
            StopCoroutine(_gameplaySpawnRoutine);

        _gameplaySpawnRoutine = StartCoroutine(DelayedReconcileAfterSceneLoad(waitSeconds));
    }

    private void ScheduleGameplaySpawnFallback()
    {
        StartCoroutine(GameplaySpawnFallbackRoutine());
    }

    private System.Collections.IEnumerator GameplaySpawnFallbackRoutine()
    {
        yield return new WaitForSeconds(gameplaySpawnDelaySeconds);

        if (_spawnedForCurrentGameplayRound || !IsGameplaySceneLoaded())
            yield break;

        if (enableDiagnosticsLogs)
            Debug.Log("[PlayerSpawnManager][DIAG] Fallback de spawn após LoadEventCompleted (SynchronizeComplete não disparou).");

        yield return DelayedReconcileAfterSceneLoad(0f);
    }

    private static bool ShouldReconcilePlayersForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
               || sceneName is "Game" or "Gameplay";
    }

    private System.Collections.IEnumerator DelayedReconcileAfterSceneLoad(float waitSeconds)
    {
        if (waitSeconds > 0f)
            yield return new WaitForSeconds(waitSeconds);

        yield return null;
        yield return null;

        if (!IsGameplaySceneLoaded())
        {
            _gameplaySpawnRoutine = null;
            yield break;
        }

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            ReconcilePlayerAfterSceneLoad(clientId, forceRespawn: true);

        _spawnedForCurrentGameplayRound = true;
        _gameplaySpawnRoutine = null;
    }

    private void DespawnAllPlayers()
    {
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var clientData))
                continue;

            NetworkObject existing = clientData.PlayerObject;
            if (existing != null && existing.IsSpawned)
                existing.Despawn(true);
        }

        _spawnedPlayers.Clear();
    }

    private System.Collections.IEnumerator ReplaceAutoSpawnedPlayersOnceRoutine()
    {
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (!IsServer || NetworkManager == null)
            yield break;

        if (!IsGameplaySceneLoaded())
            yield break;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var clientData))
                continue;

            NetworkObject existingPlayer = clientData.PlayerObject;
            if (existingPlayer == null || !existingPlayer.IsSpawned)
            {
                SpawnPlayerForClient(clientId);
                continue;
            }

            GameObject expectedPrefab = GetPrefabForClient(clientId);
            if (expectedPrefab == null)
                continue;

            string expectedName = expectedPrefab.name;
            if (existingPlayer.name.StartsWith(expectedName, System.StringComparison.Ordinal))
                continue;

            if (enableDiagnosticsLogs)
            {
                Debug.Log($"[PlayerSpawnManager][DIAG] Substituindo PlayerObject automático do NGO para ClientId={clientId} " +
                          $"(atual='{existingPlayer.name}', esperado='{expectedName}').");
            }

            existingPlayer.Despawn(true);
            _spawnedPlayers.Remove(clientId);
            SpawnPlayerForClient(clientId);
        }
    }

    private void ReconcilePlayerAfterSceneLoad(ulong clientId, bool forceRespawn)
    {
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var clientData))
        {
            NetworkObject existing = clientData.PlayerObject;
            if (forceRespawn && existing != null && existing.IsSpawned)
            {
                if (enableDiagnosticsLogs)
                    Debug.Log($"[PlayerSpawnManager][DIAG] Force respawn pós-sync para ClientId={clientId}.");
                existing.Despawn(true);
                _spawnedPlayers.Remove(clientId);
            }
            else if (!forceRespawn && existing != null && existing.IsSpawned)
            {
                return;
            }
        }

        SpawnPlayerForClient(clientId);
    }

    /// <summary>
    /// Spawna o prefab do jogador para o cliente especificado num SpawnPoint disponível.
    /// Executado apenas no servidor.
    /// </summary>
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;
        if (!IsGameplaySceneLoaded())
            return;

        if (_spawnedPlayers.ContainsKey(clientId))
        {
            NetworkObject cachedPlayer = _spawnedPlayers[clientId];
            if (cachedPlayer != null && cachedPlayer.IsSpawned)
            {
                if (enableDiagnosticsLogs)
                    Debug.Log($"[PlayerSpawnManager][DIAG] Cliente {clientId} ja possui player spawned (NetworkObjectId={cachedPlayer.NetworkObjectId}).");
                return;
            }

            if (enableDiagnosticsLogs)
                Debug.LogWarning($"[PlayerSpawnManager][DIAG] Cache stale detectado para ClientId={clientId}. Removendo cache para respawn.");
            _spawnedPlayers.Remove(clientId);
        }

        GameObject selectedPrefab = GetPrefabForClient(clientId);
        if (selectedPrefab == null)
        {
            Debug.LogError("[PlayerSpawnManager] Nao e possivel spawnar: prefab de jogador nulo.");
            return;
        }

        int spawnIndex = GetSpawnIndexForClient(clientId);
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);

        Debug.Log($"[PlayerSpawnManager] Spawnando jogador para ClientId={clientId} no SpawnPoint[{spawnIndex}] em {spawnPosition}");

        GameObject playerObj = Instantiate(selectedPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError($"[PlayerSpawnManager] O prefab '{selectedPrefab.name}' nao tem NetworkObject! " +
                           "Adicione o componente NetworkObject ao prefab do jogador.");
            Destroy(playerObj);
            return;
        }

        // SpawnAsPlayerObject vincula o objeto ao cliente e atribui ownership
        networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        _spawnedPlayers[clientId] = networkObject;

        Debug.Log($"[PlayerSpawnManager] Jogador spawnado com sucesso. ClientId={clientId}, NetworkObjectId={networkObject.NetworkObjectId}");
    }

    private static bool IsGameplaySceneLoaded()
    {
        return ShouldReconcilePlayersForScene(SceneManager.GetActiveScene().name);
    }

    private System.Collections.IEnumerator EnsurePlayersSpawnedRoutine()
    {
        while (IsServer && NetworkManager != null)
        {
            if (!IsGameplaySceneLoaded())
            {
                yield return new WaitForSeconds(spawnRecoveryInterval);
                continue;
            }

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                bool hasLiveSpawnInCache = _spawnedPlayers.TryGetValue(clientId, out var cachedObj)
                                           && cachedObj != null
                                           && cachedObj.IsSpawned;

                bool hasPlayerObjectInConnection = NetworkManager.ConnectedClients.TryGetValue(clientId, out var clientData)
                                                   && clientData.PlayerObject != null
                                                   && clientData.PlayerObject.IsSpawned;

                if (!hasLiveSpawnInCache || !hasPlayerObjectInConnection)
                {
                    if (enableDiagnosticsLogs)
                    {
                        Debug.LogWarning($"[PlayerSpawnManager][DIAG] Reconciliando spawn para ClientId={clientId} " +
                                         $"(cacheLive={hasLiveSpawnInCache}, connPlayerLive={hasPlayerObjectInConnection}).");
                    }

                    SpawnPlayerForClient(clientId);
                }
            }

            yield return new WaitForSeconds(spawnRecoveryInterval);
        }
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

    private GameObject GetPrefabForClient(ulong clientId)
    {
        LobbyCharacterType selectedType = LobbyCharacterType.Default;
        if (LobbySelectionStore.TryGetCharacter(clientId, out var savedType))
        {
            selectedType = savedType;
        }
        else if (LobbySessionManager.Instance != null
            && LobbySessionManager.Instance.TryGetPlayerState(clientId, out var playerState))
        {
            selectedType = playerState.CharacterType;
        }

        GameObject resolved = ResolveCharacterPrefab(selectedType);
        if (resolved != null)
            return resolved;

        return playerNetworkPrefab;
    }

    private GameObject ResolveCharacterPrefab(LobbyCharacterType selectedType)
    {
        if (selectedType == LobbyCharacterType.Default || characterPrefabs == null)
            return null;

        GameObject mappedPrefab = null;
        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            if (characterPrefabs[i].characterType == selectedType)
                mappedPrefab = characterPrefabs[i].prefab;
        }

        if (mappedPrefab != null && PrefabMatchesCharacterType(mappedPrefab, selectedType))
            return mappedPrefab;

        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            GameObject candidate = characterPrefabs[i].prefab;
            if (candidate != null && PrefabMatchesCharacterType(candidate, selectedType))
                return candidate;
        }

        return mappedPrefab;
    }

    private static bool PrefabMatchesCharacterType(GameObject prefab, LobbyCharacterType type)
    {
        if (prefab == null)
            return false;

        string name = prefab.name.ToLowerInvariant();
        bool isNix = name.Contains("nix");
        bool isCora = name.Contains("cora");

        return type switch
        {
            LobbyCharacterType.CharacterA => isNix && !isCora,
            LobbyCharacterType.CharacterB => isCora && !isNix,
            _ => false
        };
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Retorna o NetworkObject do jogador de um cliente específico.</summary>
    public NetworkObject GetPlayerForClient(ulong clientId) =>
        _spawnedPlayers.TryGetValue(clientId, out var obj) ? obj : null;

    /// <summary>Retorna todos os jogadores spawnados atualmente.</summary>
    public IReadOnlyDictionary<ulong, NetworkObject> GetAllSpawnedPlayers() => _spawnedPlayers;
}
