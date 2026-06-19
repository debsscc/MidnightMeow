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

    public float PathProgress => _pathProgress.Value;
    public bool IsBroken => _isBroken.Value;
    public bool RepairActive => _repairActive.Value;
    public float RepairProgress => _repairProgress.Value;
    public bool HasArrived => _arrived.Value;
    public CarriageConfig Config => config;
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
        _health = GetComponent<HealthComponent>();
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<CarriageConfig>();
            Debug.LogWarning("[NetworkCarriage] CarriageConfig não atribuído — usando instância padrão em memória.");
        }

        _health.SetAllowDestroyOnDeath(false);
        _health.OnDied.AddListener(HandleBroken);
        _health.OnHealthChanged.AddListener(HandleHealthChanged);
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
        if (!IsServer)
            return;

        if (config != null)
            _health.Initialize(config.maxHealth);

        ApplyPathPosition();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || config == null || path == null)
            return;

        if (_arrived.Value)
            return;

        if (_isBroken.Value)
        {
            TickRepair();
            return;
        }

        float next = _pathProgress.Value + config.moveSpeed * Time.deltaTime / Mathf.Max(0.1f, GetPathLengthEstimate());
        _pathProgress.Value = Mathf.Clamp01(next);
        ApplyPathPosition();

        GameEvents.InvokeCarriagePathProgressChanged(_pathProgress.Value);

        if (_pathProgress.Value >= 1f || Vector2.Distance(transform.position, path.ArrivalPosition) <= config.arrivalZoneRadius)
        {
            _arrived.Value = true;
            GameEvents.InvokeCarriageArrived();
        }
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
    }

    private void HandleBroken()
    {
        if (!IsServer)
            return;

        _isBroken.Value = true;
        _repairActive.Value = false;
        _repairProgress.Value = 0f;
    }

    private void HandleHealthChanged(float current, float max)
    {
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
        if (path == null || path.WaypointCount <= 1)
            return 10f;

        float total = 0f;
        for (int i = 0; i < path.WaypointCount - 1; i++)
            total += 1f;

        return Mathf.Max(1f, total * 4f);
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
