/// <summary>
/// Estado local do aliado dentro de zonas de reviver ativas (gerenciadas por <see cref="NetworkDownedReviveManager"/>).
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkPlayerHealth))]
public class NetworkPlayerRevive : NetworkBehaviour
{
    [SerializeField] private DownedPlayerConfig downedConfig;

    private NetworkPlayerHealth _selfHealth;

    public bool IsContributingToRevive { get; private set; }

    public bool IsReviving => false;

    private void Awake()
    {
        _selfHealth = GetComponent<NetworkPlayerHealth>();
        if (downedConfig == null)
            downedConfig = _selfHealth.DownedConfig;
    }

    private void Update()
    {
        if (!IsOwner || downedConfig == null || !_selfHealth.CanFight)
        {
            IsContributingToRevive = false;
            return;
        }

        IsContributingToRevive = IsInsideAnyActiveReviveZone();
    }

    private bool IsInsideAnyActiveReviveZone()
    {
        NetworkDownedReviveManager manager = NetworkDownedReviveManager.Instance;
        if (manager == null || !manager.IsSpawned)
            return false;

        Vector2 pos = transform.position;
        foreach (DownedReviveSession session in manager.Sessions)
        {
            if (!session.IsActive)
                continue;

            if (CooperativeZonePlacementUtility.IsInsideZone(pos, session.ZoneA, downedConfig.reviveZoneRadius))
                return true;

            if (session.ZoneCount > 1 &&
                CooperativeZonePlacementUtility.IsInsideZone(pos, session.ZoneB, downedConfig.reviveZoneRadius))
                return true;
        }

        return false;
    }
}
