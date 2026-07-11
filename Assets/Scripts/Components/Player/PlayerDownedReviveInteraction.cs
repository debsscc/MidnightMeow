using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Interação local para iniciar reviver (tecla E / Interact), espelhando <see cref="PlayerRatHoleSealInteraction"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler), typeof(NetworkPlayerHealth))]
public class PlayerDownedReviveInteraction : MonoBehaviour
{
    [SerializeField] private DownedPlayerConfig downedConfig;

    private PlayerInputHandler _input;
    private NetworkObject _networkObject;
    private NetworkPlayerHealth _selfHealth;
    private NetworkPlayerHealth _targetDowned;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _networkObject = GetComponent<NetworkObject>();
        _selfHealth = GetComponent<NetworkPlayerHealth>();

        if (downedConfig == null)
            downedConfig = DownedPlayerConfigUtility.Resolve(_selfHealth != null ? _selfHealth.DownedConfig : null);
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged += HandleInteract;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged -= HandleInteract;
    }

    private void Update()
    {
        _targetDowned = FindNearestRevivableTeammate();
    }

    public NetworkPlayerHealth CurrentTargetDowned => _targetDowned;

    private void HandleInteract(bool pressed)
    {
        if (!pressed)
            return;

        TryStartRevive();
    }

    private void TryStartRevive()
    {
        if (_targetDowned == null || downedConfig == null || _selfHealth == null || !_selfHealth.CanFight)
            return;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        NetworkDownedReviveManager manager = NetworkDownedReviveManager.Instance;
        if (manager == null || !manager.IsSpawned)
            return;

        if (manager.HasActiveSession(_targetDowned.OwnerClientId))
            return;

        manager.RequestStartReviveRpc(_targetDowned.OwnerClientId);
        GameplayInteractAudio.PlayConfirm();
    }

    private NetworkPlayerHealth FindNearestRevivableTeammate()
    {
        if (downedConfig == null || _selfHealth == null || !_selfHealth.CanFight)
            return null;

        float promptRadius = downedConfig.revivePromptRadius;
        Vector2 pos = transform.position;
        NetworkPlayerHealth nearest = null;
        float minDist = float.MaxValue;

        foreach (NetworkPlayerHealth health in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (health == null || !health.IsSpawned || health == _selfHealth)
                continue;

            if (!health.CanBeRevived)
                continue;

            NetworkDownedReviveManager manager = NetworkDownedReviveManager.Instance;
            if (manager != null && manager.HasActiveSession(health.OwnerClientId))
                continue;

            float dist = Vector2.Distance(pos, health.transform.position);
            if (dist > promptRadius || dist >= minDist)
                continue;

            minDist = dist;
            nearest = health;
        }

        return nearest;
    }
}

/// <summary>
/// Interação local para iniciar conserto da carruagem (tecla E).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler), typeof(NetworkPlayerHealth))]
public class PlayerCarriageRepairInteraction : MonoBehaviour
{
    [SerializeField] private CarriageConfig carriageConfig;

    private PlayerInputHandler _input;
    private NetworkObject _networkObject;
    private NetworkPlayerHealth _selfHealth;
    private CarriageController _targetCarriage;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _networkObject = GetComponent<NetworkObject>();
        _selfHealth = GetComponent<NetworkPlayerHealth>();
        carriageConfig = CarriageConfigUtility.Resolve(carriageConfig);
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged += HandleInteract;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged -= HandleInteract;
    }

    private void Update() => _targetCarriage = ResolveRepairableCarriage();

    private void HandleInteract(bool pressed)
    {
        if (pressed)
            TryStartRepair();
    }

    private void TryStartRepair()
    {
        if (_targetCarriage == null || carriageConfig == null || _selfHealth == null || !_selfHealth.CanFight)
            return;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        NetworkCarriageHealth carriageHealth = _targetCarriage.Health;
        NetworkCarriageRepairManager repairManager = _targetCarriage.GetComponent<NetworkCarriageRepairManager>();
        if (carriageHealth == null || repairManager == null || !repairManager.IsSpawned)
            return;

        if (!carriageHealth.IsBroken || repairManager.RepairActive)
            return;

        repairManager.RequestStartRepairRpc();
        GameplayInteractAudio.PlayConfirm();
    }

    private CarriageController ResolveRepairableCarriage()
    {
        if (carriageConfig == null || _selfHealth == null || !_selfHealth.CanFight)
            return null;

        CarriageController carriage = CarriageController.Instance;
        if (carriage == null || !carriage.IsSpawned)
            return null;

        NetworkCarriageHealth health = carriage.Health;
        NetworkCarriageRepairManager repairManager = carriage.GetComponent<NetworkCarriageRepairManager>();
        if (health == null || !health.IsBroken || repairManager == null || repairManager.RepairActive)
            return null;

        float dist = Vector2.Distance(transform.position, carriage.transform.position);
        return dist <= carriageConfig.repairPromptRadius ? carriage : null;
    }
}
