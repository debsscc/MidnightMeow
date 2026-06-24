/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Spawn autoritativo de jogadores em SpawnPoints (NGO).
---------------------------------------------------------------- */

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
    [SerializeField] private float gameplaySpawnDelaySeconds = 0.35f;
    [Tooltip("Distância entre jogadores que compartilham o mesmo spawn point.")]
    [SerializeField] private float coSpawnSeparation = 1.35f;

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
            StartCoroutine(DespawnPlayersWhenTransitionCovered());
            return;
        }

        if (!ShouldReconcilePlayersForScene(sceneEvent.SceneName))
            return;

        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            ScheduleGameplaySpawnForClient(sceneEvent.ClientId, waitSeconds: 0f, forceRespawn: true);
            return;
        }

        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            ScheduleGameplaySpawnForClient(
                sceneEvent.ClientId,
                waitSeconds: gameplaySpawnDelaySeconds,
                forceRespawn: true);
        }
    }

    private void ScheduleGameplaySpawnForClient(ulong clientId, float waitSeconds, bool forceRespawn)
    {
        StartCoroutine(DelayedReconcileClientAfterSceneLoad(clientId, waitSeconds, forceRespawn));
    }

    private static bool ShouldReconcilePlayersForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return sceneName.StartsWith("Fase-", System.StringComparison.Ordinal)
               || sceneName is "Game" or "Gameplay";
    }

    private System.Collections.IEnumerator DelayedReconcileClientAfterSceneLoad(
        ulong clientId,
        float waitSeconds,
        bool forceRespawn)
    {
        if (waitSeconds > 0f)
            yield return new WaitForSeconds(waitSeconds);

        yield return null;
        yield return null;

        if (!IsServer || NetworkManager == null || !IsGameplaySceneLoaded())
            yield break;

        if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
            yield break;

        if (enableDiagnosticsLogs)
        {
            Debug.Log($"[PlayerSpawnManager][DIAG] Reconcile pós-cena para ClientId={clientId} " +
                      $"(forceRespawn={forceRespawn}, cena='{SceneManager.GetActiveScene().name}').");
        }

        ReconcilePlayerAfterSceneLoad(clientId, forceRespawn);
    }

    private System.Collections.IEnumerator DespawnPlayersWhenTransitionCovered()
    {
        yield return GameplayTransitionCover.WaitUntilOpaque();
        DespawnAllPlayers();
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

    public void DespawnAllPlayersForRestart()
    {
        if (!IsServer)
            return;

        DespawnAllPlayers();

        NetworkObject[] networkObjects = Object.FindObjectsByType<NetworkObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < networkObjects.Length; i++)
        {
            NetworkObject networkObject = networkObjects[i];
            if (networkObject == null || !networkObject.IsSpawned || !networkObject.IsPlayerObject)
                continue;

            networkObject.Despawn(true);
        }
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
            if (existing != null && existing.IsSpawned)
            {
                bool staleScene = !IsPlayerInActiveGameplayScene(existing);
                if (forceRespawn || staleScene)
                {
                    if (enableDiagnosticsLogs || staleScene)
                    {
                        Debug.Log($"[PlayerSpawnManager][DIAG] Force respawn ClientId={clientId} " +
                                  $"(force={forceRespawn}, staleScene={staleScene}, objScene='{existing.gameObject.scene.name}').");
                    }

                    existing.Despawn(true);
                    _spawnedPlayers.Remove(clientId);
                }
                else
                {
                    ApplySpawnTransformToPlayer(existing, clientId);
                    _spawnedPlayers[clientId] = existing;
                    return;
                }
            }
        }

        SpawnPlayerForClient(clientId);
    }

    private static bool IsPlayerInActiveGameplayScene(NetworkObject player)
    {
        if (player == null)
            return false;

        string activeScene = SceneManager.GetActiveScene().name;
        return player.gameObject.scene.name == activeScene
               && ShouldReconcilePlayersForScene(activeScene);
    }

    private void ApplySpawnTransformToPlayer(NetworkObject player, ulong clientId)
    {
        int spawnIndex = GetSpawnIndexForClient(clientId);
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex, clientId);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);

        player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (player.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPosition;
        }
    }

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
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex, clientId);
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

        // destroyWithScene=false evita perder o jogador quando NGO sincroniza cena antes do cliente terminar Loading2.
        networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: false);
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

    private void HandleClientDisconnected(ulong clientId)
    {
        if (_spawnedPlayers.ContainsKey(clientId))
        {
            _spawnedPlayers.Remove(clientId);
            Debug.Log($"[PlayerSpawnManager] Registro do jogador removido. ClientId={clientId}");
        }
    }

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

    private Vector3 GetSpawnPosition(int spawnIndex, ulong clientId)
    {
        Vector3 basePosition = Vector3.zero;
        if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[spawnIndex] != null)
            basePosition = spawnPoints[spawnIndex].position;

        int coSpawnOffsetIndex = CountClientsSharingSpawnIndex(spawnIndex, clientId);
        if (coSpawnOffsetIndex <= 0 || coSpawnSeparation <= 0f)
            return basePosition;

        float angle = coSpawnOffsetIndex * Mathf.PI;
        return basePosition + new Vector3(
            Mathf.Cos(angle) * coSpawnSeparation,
            Mathf.Sin(angle) * coSpawnSeparation,
            0f);
    }

    private int CountClientsSharingSpawnIndex(int spawnIndex, ulong clientId)
    {
        int count = 0;
        foreach (var kvp in _clientSpawnIndex)
        {
            if (kvp.Value != spawnIndex)
                continue;

            if (kvp.Key < clientId)
                count++;
        }

        return count;
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

    public NetworkObject GetPlayerForClient(ulong clientId) =>
        _spawnedPlayers.TryGetValue(clientId, out var obj) ? obj : null;

    public IReadOnlyDictionary<ulong, NetworkObject> GetAllSpawnedPlayers() => _spawnedPlayers;
}
