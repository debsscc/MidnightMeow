/// <summary>
/// Reviver por zona: aliados permanecem na área ao redor do jogador caído (lógica em <see cref="DownedReviveZoneSystem"/>).
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkPlayerHealth))]
public class NetworkPlayerRevive : NetworkBehaviour
{
    [SerializeField] private DownedPlayerConfig downedConfig;

    private NetworkPlayerHealth _selfHealth;

    public bool IsContributingToRevive { get; private set; }

    /// <summary>Alias usado por movimento/combate: jogador está na zona reanimando um aliado.</summary>
    public bool IsReviving => IsContributingToRevive;

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

        IsContributingToRevive = false;
        foreach (var downed in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!downed.IsSpawned || !downed.CanBeRevived) continue;
            if (DownedReviveZoneSystem.IsAllyInsideReviveZone(downed, _selfHealth, downedConfig))
            {
                IsContributingToRevive = true;
                break;
            }
        }
    }
}
