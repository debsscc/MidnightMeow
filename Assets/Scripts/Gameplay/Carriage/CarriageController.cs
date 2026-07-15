using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Estado autoritativo da carruagem (replicado via NetworkVariable).</summary>
public enum CarriageState : byte
{
    Idle = 0,
    Moving = 1,
    Broken = 2
}

/// <summary>
/// Carruagem da Fase 2: movimento autoritativo no servidor, progresso e estado replicados via NetworkVariable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkCarriageHealth))]
public class CarriageController : NetworkBehaviour
{
    public static CarriageController Instance { get; private set; }

    public static event System.Action<CarriageController> OnInstanceAvailable;

    [SerializeField] private CarriageConfig config;
    [SerializeField] private CarriagePath path;

    private NetworkCarriageHealth _health;
    private static Sprite _cachedPlaceholderSprite;
    private Coroutine _clientVisualRefreshRoutine;
    private readonly Collider2D[] _presenceHits = new Collider2D[16];

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private float _movementDebugNextLogTime;
    private bool _movementBlockedLogged;
#endif

    private const float TargetVisualWidth = 2.4f;
    private const float TargetVisualHeight = 1.6f;
    private const int CarriageSortingOrder = 25;

    private readonly NetworkVariable<float> _pathProgress = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _hasArrived = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<CarriageState> _carriageState = new NetworkVariable<CarriageState>(
        CarriageState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float PathProgress => _pathProgress.Value;
    public bool HasArrived => _hasArrived.Value;
    public CarriageState CurrentState => _carriageState.Value;
    public CarriageConfig Config => config;
    public CarriagePath Path => path;
    public NetworkVariable<float> PathProgressVariable => _pathProgress;
    public NetworkVariable<CarriageState> CarriageStateVariable => _carriageState;
    public NetworkCarriageHealth Health => _health;

    private void Awake()
    {
        _health = GetComponent<NetworkCarriageHealth>();
        config = CarriageConfigUtility.Resolve(config);
        _health.SetAllowDestroyOnDeath(false);

        if (TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            networkObject.SynchronizeTransform = false;

        EnsureRuntimePresentation();
    }

    public void ConfigurePath(CarriagePath carriagePath)
    {
        if (carriagePath != null)
            path = carriagePath;
    }

    public void EnsureRuntimePresentation()
    {
        if (config != null && config.useOfficialArt)
            EnsureOfficialPresentation();
        else
        {
            EnsurePlaceholderSprite();
            ApplyPlaceholderVisualScale();
        }

        EnsureWheelSpinner();
        EnsureHealthBar();
        ApplyRepairLabelOffset();
    }

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[CarriageController] Substituindo instância local por carruagem spawnada na rede.");

        Instance = this;
        OnInstanceAvailable?.Invoke(this);

        PhaseGameplayContentInstaller.ConfigureCarriage(this);

        if (IsServer)
        {
            SnapToPathProgress(_pathProgress.Value);
            if (_health != null && config != null)
                _health.ServerInitialize(config.maxHealth);
        }

        StartCoroutine(EnsurePathConfiguredRoutine());
        EnsureRuntimePresentation();

        if (!IsServer)
            BeginClientVisualRefresh();
    }

    public override void OnNetworkDespawn()
    {
        if (_clientVisualRefreshRoutine != null)
        {
            StopCoroutine(_clientVisualRefreshRoutine);
            _clientVisualRefreshRoutine = null;
        }

        if (Instance == this)
            Instance = null;

        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
            return;

        if (_health != null && _health.IsBroken)
        {
            SetCarriageState(CarriageState.Broken);
            return;
        }

        if (!CanAdvanceMovement())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogMovementBlockedOnce();
#endif
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _movementBlockedLogged = false;
#endif

        if (GameEvents.IsPaused || _hasArrived.Value)
            return;

        bool playersNearby = HasLivingPlayerNearby();
        SetCarriageState(playersNearby ? CarriageState.Moving : CarriageState.Idle);

        if (!playersNearby)
            return;

        float totalLength = Mathf.Max(0.1f, path.GetTotalLength());
        float delta = (config.moveSpeed * Time.deltaTime) / totalLength;
        float nextProgress = Mathf.Min(1f, _pathProgress.Value + delta);
        _pathProgress.Value = nextProgress;

        SnapToPathProgress(nextProgress);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Time.time >= _movementDebugNextLogTime)
        {
            _movementDebugNextLogTime = Time.time + 2f;
            Debug.Log(
                $"[CarriageController] Movendo (servidor). pos={transform.position}, progress={nextProgress:P1}, " +
                $"speed={config.moveSpeed}, pathLen={totalLength:F1}, NetworkId={NetworkObjectId}");
        }
#endif

        if (nextProgress >= 0.999f
            || Vector2.Distance(transform.position, path.ArrivalPosition) <= config.arrivalZoneRadius)
        {
            CompleteArrival();
        }
    }

    /// <summary>Servidor: vida zerada — interrompe movimento e replica Broken.</summary>
    public void ServerNotifyBroken()
    {
        if (!IsServer)
            return;

        SetCarriageState(CarriageState.Broken);
    }

    /// <summary>Servidor: após conserto — reavalia Idle/Moving pela presença de jogadores.</summary>
    public void ServerNotifyRepaired()
    {
        if (!IsServer)
            return;

        if (_health != null && _health.IsBroken)
            return;

        bool playersNearby = HasLivingPlayerNearby();
        SetCarriageState(playersNearby ? CarriageState.Moving : CarriageState.Idle);
    }

    private void SetCarriageState(CarriageState next)
    {
        if (_carriageState.Value == next)
            return;

        _carriageState.Value = next;
    }

    private bool HasLivingPlayerNearby()
    {
        if (config == null)
            return false;

        float radius = config.GetPlayerPresenceRadius();
        LayerMask mask = config.ResolvePlayerPresenceLayerMask();
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _presenceHits, mask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _presenceHits[i];
            if (hit == null)
                continue;

            NetworkPlayerHealth playerHealth = hit.GetComponentInParent<NetworkPlayerHealth>();
            if (playerHealth != null && playerHealth.IsSpawned && playerHealth.CanFight)
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        float radius = config != null ? config.GetPlayerPresenceRadius() : 8f;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogMovementBlockedOnce()
    {
        if (!IsServer || _movementBlockedLogged)
            return;

        _movementBlockedLogged = true;
        string pathInfo = path == null ? "null" : $"{path.WaypointCount} wp, len={path.GetTotalLength():F1}";
        Debug.LogWarning(
            $"[CarriageController] Movimento bloqueado no servidor. IsSpawned={IsSpawned}, path={pathInfo}, " +
            $"config={(config != null)}, InstanceMatch={(Instance == this)}, NetworkId={NetworkObjectId}");
    }
#endif

    private void CompleteArrival()
    {
        if (_hasArrived.Value)
            return;

        SnapToPathProgress(1f);
        _pathProgress.Value = 1f;
        _hasArrived.Value = true;

        GameEvents.InvokeCarriageArrived();
        EnsurePhaseObjectiveAndNotifyVictory();
    }

    private static void EnsurePhaseObjectiveAndNotifyVictory()
    {
        if (PhaseObjectiveManager.Instance == null)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.LogWarning($"[CarriageController] PhaseObjectiveManager ausente em {sceneName} — reconfigurando fase.");
            PhaseGameplayContentInstaller.ApplyPhaseContent(sceneName);
        }

        if (PhaseObjectiveManager.Instance != null)
        {
            PhaseObjectiveManager.Instance.NotifyCarriageArrived();
            return;
        }

        Debug.LogError("[CarriageController] Falha ao configurar PhaseObjectiveManager — fallback de vitória.");
        MultiplayerVictoryCoordinator.TryBeginVictoryFromPhaseObjective();
        PhaseObjectiveManager.Instance?.BeginVictoryScreenFallback();
    }

    private bool CanAdvanceMovement()
    {
        if (!IsServer || !IsSpawned || config == null)
            return false;

        if (path != null && path.WaypointCount >= 2)
            return true;

        PhaseGameplayContentInstaller.ConfigureCarriage(this);
        return path != null && path.WaypointCount >= 2;
    }

    private IEnumerator EnsurePathConfiguredRoutine()
    {
        const float timeoutSeconds = 15f;
        const float pollIntervalSeconds = 0.25f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            PhaseGameplayContentInstaller.ConfigureCarriage(this);

            if (path != null && path.WaypointCount >= 2)
            {
                EnsureRuntimePresentation();
                if (!IsServer)
                    BeginClientVisualRefresh();
                yield break;
            }

            elapsed += pollIntervalSeconds;
            yield return new WaitForSeconds(pollIntervalSeconds);
        }

        Debug.LogWarning(
            $"[CarriageController] Timeout aguardando CarriagePath (IsServer={IsServer}, IsSpawned={IsSpawned}) — verifique Fase-2 e CarriageConfig.");
    }

    private void SnapToPathProgress(float normalized)
    {
        if (path == null)
            return;

        Vector3 pos = path.EvaluatePosition(normalized);
        pos.z = transform.position.z;
        transform.position = pos;
    }

    private void BeginClientVisualRefresh()
    {
        if (!IsSpawned || IsServer)
            return;

        if (_clientVisualRefreshRoutine != null)
            StopCoroutine(_clientVisualRefreshRoutine);

        _clientVisualRefreshRoutine = StartCoroutine(RefreshClientVisualAfterNetworkSync());
    }

    private IEnumerator RefreshClientVisualAfterNetworkSync()
    {
        const int refreshFrames = 3;
        for (int i = 0; i < refreshFrames; i++)
        {
            yield return null;
            EnsureRuntimePresentation();
        }

        _clientVisualRefreshRoutine = null;
    }

    private void EnsureHealthBar()
    {
        if (GetComponent<EnemyHealthBarDisplay>() == null)
            gameObject.AddComponent<EnemyHealthBarDisplay>();
    }

    private Transform ResolveVisualRoot()
    {
        Transform root = transform.Find("VisualRoot");
        if (root != null)
            return root;

        Transform legacy = transform.Find("Visual");
        if (legacy != null)
            return legacy;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            string name = renderers[i].gameObject.name;
            if (name == "VisualRoot" || name == "Visual" || name == "Body")
                return renderers[i].transform;
        }

        return renderers.Length > 0 ? renderers[0].transform : null;
    }

    private void EnsureOfficialPresentation()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Transform visualRoot = ResolveVisualRoot();
        if (visualRoot == null)
            return;

        if (!visualRoot.gameObject.activeSelf)
            visualRoot.gameObject.SetActive(true);

        float scale = config != null ? Mathf.Max(0.05f, config.visualRootScale) : 0.3f;
        visualRoot.localScale = new Vector3(scale, scale, 1f);
        visualRoot.gameObject.layer = gameObject.layer;

        SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;

            sr.enabled = true;
            sr.forceRenderingOff = false;
            sr.gameObject.layer = gameObject.layer;
        }

        Vector3 rootPos = transform.position;
        if (Mathf.Abs(rootPos.z) > 0.001f)
        {
            rootPos.z = 0f;
            transform.position = rootPos;
        }

        ApplyConfiguredCollider();
    }

    private void EnsurePlaceholderSprite()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Transform visual = ResolveVisualRoot();
        if (visual == null)
            return;

        if (!visual.gameObject.activeSelf)
            visual.gameObject.SetActive(true);

        visual.gameObject.layer = gameObject.layer;

        Vector3 localPos = visual.localPosition;
        localPos.z = 0f;
        visual.localPosition = localPos;

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        spriteRenderer.enabled = true;
        spriteRenderer.forceRenderingOff = false;
        spriteRenderer.sprite = ResolvePlaceholderSprite();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.color = new Color(0.75f, 0.55f, 0.25f, 1f);
        spriteRenderer.sortingOrder = CarriageSortingOrder;

        Vector3 rootPos = transform.position;
        if (Mathf.Abs(rootPos.z) > 0.001f)
        {
            rootPos.z = 0f;
            transform.position = rootPos;
        }
    }

    private static Sprite ResolvePlaceholderSprite()
    {
        if (_cachedPlaceholderSprite != null)
            return _cachedPlaceholderSprite;

        Sprite fromResources = Resources.Load<Sprite>("CarriagePlaceholderSprite");
        if (fromResources != null)
        {
            _cachedPlaceholderSprite = fromResources;
            return _cachedPlaceholderSprite;
        }

        _cachedPlaceholderSprite = CreateRuntimePlaceholderSprite();
        return _cachedPlaceholderSprite;
    }

    private static Sprite CreateRuntimePlaceholderSprite()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color fill = new Color(0.75f, 0.55f, 0.25f, 1f);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = fill;

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }

    private void ApplyPlaceholderVisualScale()
    {
        Transform visual = ResolveVisualRoot();
        if (visual == null)
            return;

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float scaleX = TargetVisualWidth / Mathf.Max(0.01f, spriteSize.x);
        float scaleY = TargetVisualHeight / Mathf.Max(0.01f, spriteSize.y);
        float multiplier = config != null ? Mathf.Max(0.25f, config.visualScale) : 1f;
        visual.localScale = new Vector3(scaleX * multiplier, scaleY * multiplier, 1f);

        if (TryGetComponent<BoxCollider2D>(out var box))
        {
            Bounds bounds = spriteRenderer.bounds;
            box.size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.85f);
            box.offset = transform.InverseTransformPoint(bounds.center);
        }
    }

    private void ApplyConfiguredCollider()
    {
        if (config == null || !TryGetComponent<BoxCollider2D>(out var box))
            return;

        box.size = config.colliderSize;
        box.offset = config.colliderOffset;
    }

    private void ApplyRepairLabelOffset()
    {
        if (config == null || !config.useOfficialArt)
            return;

        if (!TryGetComponent<CarriageRepairWorldUI>(out var repairUi))
            return;

        repairUi.SetOffset(config.repairLabelOffset);
    }

    private void EnsureWheelSpinner()
    {
        if (config == null || !config.useOfficialArt)
            return;

        CarriageWheelSpinner spinner = GetComponent<CarriageWheelSpinner>();
        if (spinner == null)
            spinner = gameObject.AddComponent<CarriageWheelSpinner>();

        Transform visualRoot = ResolveVisualRoot();
        Transform front = visualRoot != null ? visualRoot.Find("Layer_Wheels/Wheel_Front") : null;
        Transform back = visualRoot != null ? visualRoot.Find("Layer_Wheels/Wheel_Back") : null;
        spinner.Configure(front, back, config.frontWheelRadius, config.backWheelRadius);
    }
}

public static class CarriageConfigUtility
{
    private static CarriageConfig _cached;

    public static CarriageConfig Resolve(CarriageConfig candidate = null)
    {
        if (candidate != null)
            return candidate;

        if (_cached != null)
            return _cached;

        CarriageController carriage = CarriageController.Instance;
        if (carriage != null && carriage.Config != null)
        {
            _cached = carriage.Config;
            return _cached;
        }

        _cached = Resources.Load<CarriageConfig>("CarriageConfig");
        if (_cached == null)
            _cached = Resources.Load<CarriageConfig>("Gameplay/CarriageConfig");

        if (_cached == null)
        {
            _cached = ScriptableObject.CreateInstance<CarriageConfig>();
            Debug.LogWarning("[CarriageConfigUtility] CarriageConfig ausente — usando instância padrão em memória.");
        }

        return _cached;
    }

    public static void ClearCache() => _cached = null;
}

/// <summary>Marca carruagens instanciadas em runtime pelo servidor (não remover como placeholder de cena).</summary>
internal sealed class RuntimeCarriageInstance : MonoBehaviour
{
}

public sealed class CarriageSpawner : MonoBehaviour
{
    private const string CarriagePrefabPath = "Assets/Prefabs/Gameplay/Carriage.prefab";
    private const string CarriageSceneName = "Fase-2";
    private const float SetupTimeoutSeconds = 45f;
    private const float PollIntervalSeconds = 0.1f;

    private static bool _scheduled;
    private static readonly HashSet<ulong> ClientsReadyForCarriageScene = new();

    private const float SyncGraceBeforeForceSpawnSeconds = 2.5f;

    public static void NotifyClientSceneReady(ulong clientId)
    {
        ClientsReadyForCarriageScene.Add(clientId);

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        if (SceneManager.GetActiveScene().name != CarriageSceneName)
            return;

        if (HasAuthoritativeRuntimeCarriage())
            return;

        if (!ShouldAllowSpawn(networkManager, 0f))
            return;

        if (TrySpawnCarriageOnServer(out CarriageController carriage))
        {
            Debug.Log(
                $"[CarriageSpawner] Carruagem spawnada (NotifyClientSceneReady). NetworkId={carriage.NetworkObjectId}, " +
                $"clientes={networkManager.ConnectedClientsIds.Count}, prontos={ClientsReadyForCarriageScene.Count}");
            return;
        }

        EnsureCarriageSpawned();
    }

    public static void ClearClientSceneReadyState()
    {
        ClientsReadyForCarriageScene.Clear();
    }

    public static bool HasAuthoritativeRuntimeCarriage()
    {
        CarriageController[] carriages = Object.FindObjectsByType<CarriageController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < carriages.Length; i++)
        {
            CarriageController carriage = carriages[i];
            if (carriage == null)
                continue;

            NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
                return true;
        }

        return false;
    }

    public static void EnsureCarriageSpawned()
    {
        if (SceneManager.GetActiveScene().name != CarriageSceneName)
            return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        if (HasAuthoritativeRuntimeCarriage())
            return;

        if (_scheduled)
            return;

        _scheduled = true;
        var host = new GameObject(nameof(CarriageSpawner));
        host.AddComponent<CarriageSpawner>();
    }

    /// <summary>
    /// Remove apenas placeholders colocados na cena pelo editor. Instâncias runtime (RuntimeCarriageInstance) são preservadas.
    /// </summary>
    public static void RemoveScenePlaceholders()
    {
        CarriageController[] carriages = Object.FindObjectsByType<CarriageController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int removed = 0;
        NetworkManager networkManager = NetworkManager.Singleton;
        bool isServer = networkManager != null && networkManager.IsServer;

        for (int i = carriages.Length - 1; i >= 0; i--)
        {
            CarriageController carriage = carriages[i];
            if (carriage == null)
                continue;

            if (carriage.GetComponent<RuntimeCarriageInstance>() != null)
                continue;

            NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
            if (isServer && networkObject != null && networkObject.IsSpawned)
                networkObject.Despawn(true);

            Object.Destroy(carriage.gameObject);
            removed++;
        }

        if (removed > 0)
        {
            Debug.Log(
                $"[CarriageSpawner] Removida(s) {removed} carruagem(ns) placeholder da cena — servidor fará spawn runtime.");
        }
    }

    private void OnDestroy() => _scheduled = false;

    private static bool AreAllConnectedClientsSceneReady(NetworkManager networkManager)
    {
        if (networkManager.ConnectedClientsIds.Count == 0)
            return false;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (!ClientsReadyForCarriageScene.Contains(clientId))
                return false;
        }

        return true;
    }

    private static bool AllConnectedClientsHavePlayerObjects(NetworkManager networkManager)
    {
        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
                return false;

            if (client.PlayerObject == null)
                return false;
        }

        return true;
    }

    private static bool ShouldAllowSpawn(NetworkManager networkManager, float syncWaitSeconds)
    {
        if (SceneManager.GetActiveScene().name != CarriageSceneName)
            return false;

        if (AreAllConnectedClientsSceneReady(networkManager))
            return true;

        if (networkManager.ConnectedClientsIds.Count == 1)
            return true;

        if (syncWaitSeconds >= SyncGraceBeforeForceSpawnSeconds
            && AllConnectedClientsHavePlayerObjects(networkManager))
        {
            Debug.LogWarning(
                $"[CarriageSpawner] Fallback de spawn — scene events incompletos (prontos={ClientsReadyForCarriageScene.Count}, " +
                $"conectados={networkManager.ConnectedClientsIds.Count}).");
            return true;
        }

        return false;
    }

    private void OnEnable() => StartCoroutine(SetupRoutine());

    private IEnumerator SetupRoutine()
    {
        float waited = 0f;
        float syncWait = 0f;

        while (waited < SetupTimeoutSeconds)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || !networkManager.IsServer)
            {
                waited += PollIntervalSeconds;
                yield return new WaitForSeconds(PollIntervalSeconds);
                continue;
            }

            ClientsReadyForCarriageScene.Add(networkManager.LocalClientId);

            if (!ShouldAllowSpawn(networkManager, syncWait))
            {
                syncWait += PollIntervalSeconds;
                waited += PollIntervalSeconds;
                yield return new WaitForSeconds(PollIntervalSeconds);
                continue;
            }

            RemoveScenePlaceholders();

            if (TrySpawnCarriageOnServer(out CarriageController carriage))
            {
                Debug.Log(
                    $"[CarriageSpawner] Carruagem spawnada na rede. NetworkId={carriage.NetworkObjectId}, " +
                    $"clientes={networkManager.ConnectedClientsIds.Count}, pos={carriage.transform.position}, " +
                    $"pathLen={carriage.Path.GetTotalLength():F1}");
                Destroy(gameObject);
                yield break;
            }

            syncWait += PollIntervalSeconds;
            waited += PollIntervalSeconds;
            yield return new WaitForSeconds(PollIntervalSeconds);
        }

        Debug.LogWarning(
            "[CarriageSpawner] Timeout aguardando clientes/sincronização — carruagem não spawnada para todos os peers.");
        Destroy(gameObject);
    }

    private static bool TrySpawnCarriageOnServer(out CarriageController carriage)
    {
        carriage = FindRuntimeCarriageCandidate();
        if (carriage == null)
            carriage = TryInstantiateCarriage();

        if (carriage == null)
            return false;

        PhaseGameplayContentInstaller.ConfigureCarriage(carriage);

        NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned)
            networkObject.Spawn(true);

        return IsCarriageReady(carriage);
    }

    private static CarriageController FindRuntimeCarriageCandidate()
    {
        RuntimeCarriageInstance[] markers = Object.FindObjectsByType<RuntimeCarriageInstance>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] == null)
                continue;

            CarriageController carriage = markers[i].GetComponent<CarriageController>();
            if (carriage != null)
                return carriage;
        }

        return null;
    }

    private static bool IsCarriageReady(CarriageController carriage)
    {
        if (carriage == null || carriage.Path == null || carriage.Path.WaypointCount < 2)
            return false;

        NetworkObject networkObject = carriage.GetComponent<NetworkObject>();
        return networkObject != null && networkObject.IsSpawned;
    }

    private static CarriageController TryInstantiateCarriage()
    {
        GameObject prefab = ResolveCarriagePrefab();
        if (prefab == null)
        {
            Debug.LogError("[CarriageSpawner] Prefab Carriage não encontrado (GameplayPrefabCatalog).");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = "Carriage";
        if (instance.GetComponent<RuntimeCarriageInstance>() == null)
            instance.AddComponent<RuntimeCarriageInstance>();

        CarriageController carriage = instance.GetComponent<CarriageController>();
        if (carriage != null)
            return carriage;

        Debug.LogError("[CarriageSpawner] Prefab sem CarriageController.");
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
