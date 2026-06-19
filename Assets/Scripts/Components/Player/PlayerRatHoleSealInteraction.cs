using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Interação local do jogador para iniciar selamento (tecla Interact / F).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerRatHoleSealInteraction : MonoBehaviour
{
    [SerializeField] private RatHoleSealConfig config;

    private PlayerInputHandler _input;
    private NetworkObject _networkObject;
    private RatHoleSpawnPoint _targetHole;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _networkObject = GetComponent<NetworkObject>();
        if (config == null)
            config = Resources.Load<RatHoleSealConfig>("RatHoleSealConfig");
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
        _targetHole = FindNearestUnsealedHole();
    }

    public RatHoleSpawnPoint CurrentTargetHole => _targetHole;

    private void HandleInteract(bool pressed)
    {
        if (!pressed || _targetHole == null)
            return;

        if (_networkObject != null && _networkObject.IsSpawned && !_networkObject.IsOwner)
            return;

        if (TryGetComponent<NetworkPlayerHealth>(out var health) && !health.CanFight)
            return;

        NetworkRatHoleSealManager manager = NetworkRatHoleSealManager.Instance;
        if (manager == null)
            return;

        manager.RequestStartSealRpc(_targetHole.HoleId);
    }

    private RatHoleSpawnPoint FindNearestUnsealedHole()
    {
        if (config == null)
            return null;

        RatHoleSpawnPoint nearest = null;
        float minDist = float.MaxValue;
        Vector2 pos = transform.position;

        foreach (RatHoleSpawnPoint hole in RatHoleSpawnPoint.All)
        {
            if (hole == null || !hole.isActiveAndEnabled || hole.IsSealed)
                continue;

            float dist = Vector2.Distance(pos, hole.AnchorPosition);
            if (dist > config.promptRadius || dist >= minDist)
                continue;

            minDist = dist;
            nearest = hole;
        }

        return nearest;
    }
}
