using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Interação local para iniciar conserto da carruagem (tecla E / Interact).
/// Espelha <see cref="PlayerRatHoleSealInteraction"/> e <see cref="PlayerDownedReviveInteraction"/>.
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
    private float _lastRequestTime = -1f;

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

    private void Update()
    {
        _targetCarriage = ResolveRepairableCarriage();

        // Fallback defensivo (mesmo histórico do selamento): se a action Interact falhar, E direto.
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryStartRepair();
    }

    private void HandleInteract(bool pressed)
    {
        if (!pressed)
            return;

        TryStartRepair();
    }

    private void TryStartRepair()
    {
        // Evita Rpc duplicado no mesmo frame (Interact action + fallback Keyboard E).
        if (Time.unscaledTime - _lastRequestTime < 0.05f)
            return;

        if (_selfHealth == null || !_selfHealth.CanFight)
            return;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        CarriageController carriage = _targetCarriage != null ? _targetCarriage : ResolveRepairableCarriage();
        if (carriage == null || !carriage.IsSpawned)
            return;

        NetworkCarriageHealth carriageHealth = carriage.Health != null
            ? carriage.Health
            : carriage.GetComponent<NetworkCarriageHealth>();
        NetworkCarriageRepairManager repairManager = carriage.GetComponent<NetworkCarriageRepairManager>();
        if (carriageHealth == null || repairManager == null || !repairManager.IsSpawned)
            return;

        if (!carriageHealth.IsBroken || repairManager.RepairActive)
            return;

        _lastRequestTime = Time.unscaledTime;
        repairManager.RequestStartRepairRpc();
        GameplayInteractAudio.PlayConfirm();
    }

    private CarriageController ResolveRepairableCarriage()
    {
        carriageConfig = CarriageConfigUtility.Resolve(carriageConfig);
        if (carriageConfig == null || _selfHealth == null || !_selfHealth.CanFight)
            return null;

        CarriageController carriage = CarriageController.Instance;
        if (carriage == null || !carriage.IsSpawned)
            carriage = FindNearestBrokenCarriage(transform.position);

        if (carriage == null || !carriage.IsSpawned)
            return null;

        NetworkCarriageHealth health = carriage.Health != null
            ? carriage.Health
            : carriage.GetComponent<NetworkCarriageHealth>();
        NetworkCarriageRepairManager repairManager = carriage.GetComponent<NetworkCarriageRepairManager>();
        if (health == null || !health.IsBroken || repairManager == null || repairManager.RepairActive)
            return null;

        float dist = Vector2.Distance(transform.position, carriage.transform.position);
        return dist <= carriageConfig.repairPromptRadius ? carriage : null;
    }

    private static CarriageController FindNearestBrokenCarriage(Vector2 from)
    {
        CarriageController[] carriages = Object.FindObjectsByType<CarriageController>(FindObjectsSortMode.None);
        CarriageController nearest = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < carriages.Length; i++)
        {
            CarriageController c = carriages[i];
            if (c == null || !c.IsSpawned)
                continue;

            NetworkCarriageHealth health = c.Health != null ? c.Health : c.GetComponent<NetworkCarriageHealth>();
            if (health == null || !health.IsBroken)
                continue;

            float dist = Vector2.Distance(from, c.transform.position);
            if (dist >= minDist)
                continue;

            minDist = dist;
            nearest = c;
        }

        return nearest;
    }
}
