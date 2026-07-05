using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Carruagem da Fase 2: vida, movimento no trajeto, quebra e conserto cooperativo (servidor autoritativo).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(HealthComponent))]
public class NetworkCarriage : NetworkBehaviour
{
    public static NetworkCarriage Instance { get; private set; }

    [SerializeField] private CarriageConfig config;
    [SerializeField] private CarriagePath path;

    private HealthComponent _health;
    private static Sprite _cachedPlaceholderSprite;
    private Coroutine _pathSetupRoutine;

    private const float PathSetupTimeoutSeconds = 2f;
    private const float PathSetupPollSeconds = 0.1f;

    private const float TargetVisualWidth = 2.4f;
    private const float TargetVisualHeight = 1.6f;

    private readonly NetworkVariable<float> _pathProgress = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isBroken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _repairActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _repairProgress = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _repairAbandonTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector2> _repairZoneA = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector2> _repairZoneB = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> _repairZoneCount = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _arrived = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _syncHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _syncMaxHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float PathProgress => _pathProgress.Value;
    public bool IsBroken => _isBroken.Value;
    public bool RepairActive => _repairActive.Value;
    public float RepairProgress => _repairProgress.Value;
    public bool HasArrived => _arrived.Value;
    public CarriageConfig Config => config;
    public CarriagePath Path => path;
    public Vector2 RepairZoneA => _repairZoneA.Value;
    public Vector2 RepairZoneB => _repairZoneB.Value;
    public byte RepairZoneCount => _repairZoneCount.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveSingleHealthComponent();
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<CarriageConfig>();
            Debug.LogWarning("[NetworkCarriage] CarriageConfig não atribuído — usando instância padrão em memória.");
        }

        _health.SetAllowDestroyOnDeath(false);
        _health.OnDied.AddListener(HandleBroken);
        _health.OnHealthChanged.AddListener(HandleHealthChanged);
        EnsureRuntimePresentation();
    }

    public void ConfigurePath(CarriagePath carriagePath)
    {
        if (carriagePath != null)
            path = carriagePath;
    }

    public void EnsureRuntimePresentation()
    {
        EnsurePlaceholderSprite();
        ApplyVisualScale();
        EnsureHealthBar();
    }

    private void ResolveSingleHealthComponent()
    {
        HealthComponent[] healthComponents = GetComponents<HealthComponent>();
        if (healthComponents.Length == 0)
        {
            _health = gameObject.AddComponent<HealthComponent>();
            return;
        }

        _health = healthComponents[0];
        for (int i = 1; i < healthComponents.Length; i++)
        {
            if (healthComponents[i] != null)
                Destroy(healthComponents[i]);
        }
    }

    private void EnsurePlaceholderSprite()
    {
        Transform visual = transform.Find("Visual");
        if (visual == null)
        {
            SpriteRenderer rootSprite = GetComponentInChildren<SpriteRenderer>();
            if (rootSprite != null)
                visual = rootSprite.transform;
        }

        if (visual == null)
            return;

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = ResolvePlaceholderSprite();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.color = new Color(0.75f, 0.55f, 0.25f, 1f);
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 2);
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

    private void ApplyVisualScale()
    {
        Transform visual = transform.Find("Visual");
        if (visual == null)
        {
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
                visual = sprite.transform;
        }

        if (visual == null)
            return;

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
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

    private void EnsureHealthBar()
    {
        if (GetComponent<EnemyHealthBarDisplay>() != null)
            return;

        gameObject.AddComponent<EnemyHealthBarDisplay>();
    }

    public override void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDied.RemoveListener(HandleBroken);
            _health.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }

        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _pathProgress.OnValueChanged += HandlePathProgressChanged;
        _syncHealth.OnValueChanged += HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged += HandleSyncedMaxHealthChanged;

        EnsureLocalPathConfigured();
        ApplyPathPosition();
        SyncCarriageHudProgress();
        ApplySyncedHealthToComponent();

        if (IsServer && config != null)
        {
            _health.Initialize(config.maxHealth);
            PublishHealthToNetwork();
        }

        if (!IsPathReady())
            BeginPathSetupRetry();
    }

    public override void OnNetworkDespawn()
    {
        if (_pathSetupRoutine != null)
        {
            StopCoroutine(_pathSetupRoutine);
            _pathSetupRoutine = null;
        }

        _pathProgress.OnValueChanged -= HandlePathProgressChanged;
        _syncHealth.OnValueChanged -= HandleSyncedHealthChanged;
        _syncMaxHealth.OnValueChanged -= HandleSyncedMaxHealthChanged;
        base.OnNetworkDespawn();
    }

    private void EnsureLocalPathConfigured()
    {
        if (IsPathReady())
            return;

        PhaseGameplayContentInstaller.ConfigureCarriage(this);
    }

    private bool IsPathReady() => path != null && path.WaypointCount >= 2;

    private void SyncCarriageHudProgress() =>
        GameEvents.InvokeCarriagePathProgressChanged(_pathProgress.Value);

    private void BeginPathSetupRetry()
    {
        if (_pathSetupRoutine != null)
            StopCoroutine(_pathSetupRoutine);

        _pathSetupRoutine = StartCoroutine(EnsurePathConfiguredRoutine());
    }

    private IEnumerator EnsurePathConfiguredRoutine()
    {
        float elapsed = 0f;

        while (elapsed < PathSetupTimeoutSeconds)
        {
            if (!IsPathReady())
                PhaseGameplayContentInstaller.ConfigureCarriage(this);

            if (IsPathReady())
            {
                ApplyPathPosition();
                SyncCarriageHudProgress();
                _pathSetupRoutine = null;
                yield break;
            }

            elapsed += PathSetupPollSeconds;
            yield return new WaitForSeconds(PathSetupPollSeconds);
        }

        Debug.LogWarning(
            $"[NetworkCarriage] Timeout aguardando CarriagePath no peer (IsServer={IsServer}) — verifique Fase-2 e CarriageConfig.");
        _pathSetupRoutine = null;
    }

    private void HandlePathProgressChanged(float previous, float current)
    {
        ApplyPathPosition();
        GameEvents.InvokeCarriagePathProgressChanged(current);
    }

    private void HandleSyncedHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void HandleSyncedMaxHealthChanged(float previous, float current) => ApplySyncedHealthToComponent();

    private void ApplySyncedHealthToComponent()
    {
        if (_health == null || _syncMaxHealth.Value <= 0f)
            return;

        bool isDead = _isBroken.Value;
        _health.ApplyNetworkMirror(_syncHealth.Value, _syncMaxHealth.Value, isDead);
    }

    private void PublishHealthToNetwork()
    {
        if (_health == null)
            return;

        _syncHealth.Value = _health.CurrentHealth;
        _syncMaxHealth.Value = _health.MaxHealth;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null || path == null)
            return;

        if (GameEvents.IsPaused)
            return;

        if (_arrived.Value)
            return;

        if (_isBroken.Value)
        {
            TickRepair();
            return;
        }

        Vector3 arrival = path.ArrivalPosition;
        Vector3 toEnd = arrival - transform.position;
        float distanceToEnd = toEnd.magnitude;
        float step = config.moveSpeed * Time.deltaTime;

        if (distanceToEnd <= Mathf.Max(config.arrivalZoneRadius, step))
        {
            CompleteArrival(arrival);
            return;
        }

        transform.position += toEnd / distanceToEnd * step;
        _pathProgress.Value = path.GetNormalizedProgress(transform.position);
        ApplyPathPosition();

        if (_pathProgress.Value >= 0.98f
            || Vector2.Distance(transform.position, arrival) <= config.arrivalZoneRadius)
        {
            CompleteArrival(arrival);
        }
    }

    private void CompleteArrival(Vector3 arrival)
    {
        if (_arrived.Value)
            return;

        transform.position = arrival;
        _pathProgress.Value = 1f;
        ApplyPathPosition();
        _arrived.Value = true;

        if (IsServer)
        {
            GameEvents.InvokeCarriagePathProgressChanged(1f);
            GameEvents.InvokeCarriageArrived();
            EnsurePhaseObjectiveAndNotifyVictory();
        }
    }

    private static void EnsurePhaseObjectiveAndNotifyVictory()
    {
        if (PhaseObjectiveManager.Instance == null)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.LogWarning($"[NetworkCarriage] PhaseObjectiveManager ausente em {sceneName} — reconfigurando fase.");
            PhaseGameplayContentInstaller.ApplyPhaseContent(sceneName);
        }

        if (PhaseObjectiveManager.Instance != null)
        {
            PhaseObjectiveManager.Instance.NotifyCarriageArrived();
            return;
        }

        Debug.LogError("[NetworkCarriage] Falha ao configurar PhaseObjectiveManager — fallback de vitória.");
        MultiplayerVictoryCoordinator.TryBeginVictoryFromPhaseObjective();
        PhaseObjectiveManager.Instance?.BeginVictoryScreenFallback();
    }

    private void TickRepair()
    {
        if (!_repairActive.Value)
            return;

        var zones = new List<Vector2>(_repairZoneCount.Value);
        zones.Add(_repairZoneA.Value);
        if (_repairZoneCount.Value > 1)
            zones.Add(_repairZoneB.Value);

        int occupied = CooperativeZonePlacementUtility.CountPlayersInZones(
            zones,
            config.repairZoneRadius,
            requireDistinctZones: _repairZoneCount.Value > 1);

        if (occupied <= 0)
        {
            _repairAbandonTimer.Value += Time.deltaTime;
            if (_repairAbandonTimer.Value >= config.repairAbandonTimeout)
            {
                _repairActive.Value = false;
                _repairProgress.Value = 0f;
                _repairAbandonTimer.Value = 0f;
            }

            return;
        }

        _repairAbandonTimer.Value = 0f;
        float speed = 1f / Mathf.Max(0.1f, config.repairDuration);
        if (_repairZoneCount.Value > 1 && occupied >= 2)
            speed *= config.repairDualPlayerSpeedMultiplier;

        float next = Mathf.Clamp01(_repairProgress.Value + speed * Time.deltaTime);
        _repairProgress.Value = next;

        if (next < 1f)
            return;

        _isBroken.Value = false;
        _repairActive.Value = false;
        _repairProgress.Value = 0f;
        _health.Initialize(config.maxHealth * 0.5f);
        PublishHealthToNetwork();
    }

    private void HandleBroken()
    {
        if (!IsServer)
            return;

        _isBroken.Value = true;
        _repairActive.Value = false;
        _repairProgress.Value = 0f;
        PublishHealthToNetwork();
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (IsServer)
            PublishHealthToNetwork();

        if (!IsServer || _isBroken.Value)
            return;

        if (current <= 0f)
            HandleBroken();
    }

    [Rpc(SendTo.Server)]
    public void RequestStartRepairRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || config == null || !_isBroken.Value || _repairActive.Value)
            return;

        int alivePlayers = CountAlivePlayers();
        int zoneCount = alivePlayers >= 2 ? 2 : 1;

        CooperativeZonePlacementUtility.PlacementResult placement =
            CooperativeZonePlacementUtility.TryPlaceZones(
                transform.position,
                zoneCount,
                config.repairZoneRadius,
                config.repairMinDistance,
                config.repairMaxDistance,
                config.repairMinZoneSeparation);

        if (!placement.Success || placement.Positions == null || placement.Positions.Length == 0)
            return;

        _repairZoneA.Value = placement.Positions[0];
        _repairZoneB.Value = placement.Positions.Length > 1 ? placement.Positions[1] : placement.Positions[0];
        _repairZoneCount.Value = (byte)Mathf.Clamp(placement.Positions.Length, 1, 2);
        _repairActive.Value = true;
        _repairProgress.Value = 0f;
        _repairAbandonTimer.Value = 0f;
    }

    private void ApplyPathPosition()
    {
        if (path == null)
            return;

        Vector3 pos = path.EvaluatePosition(_pathProgress.Value);
        pos.z = transform.position.z;
        transform.position = pos;
    }

    private float GetPathLengthEstimate()
    {
        if (path == null)
            return 62f;

        return path.GetTotalLength();
    }

    private static int CountAlivePlayers()
    {
        int count = 0;
        foreach (NetworkPlayerHealth player in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (player != null && player.IsSpawned && player.CanFight)
                count++;
        }

        return Mathf.Max(1, count);
    }
}
