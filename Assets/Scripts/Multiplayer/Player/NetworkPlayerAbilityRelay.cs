using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Replica eventos pontuais de habilidade (animações/VFX) para clientes remotos.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerAbilityRelay : NetworkBehaviour
{
    public void ReportAbilityActivated(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        if (!IsSpawned || !IsOwner) return;
        ReportAbilityActivatedServerRpc(abilityType, position, direction);
    }

    public void ReportDashStarted()
    {
        if (!IsSpawned || !IsOwner) return;
        ReportAbilityActivatedServerRpc(CharacterAbilityType.Dash, transform.position, Vector2.zero);
    }

    [Rpc(SendTo.Server)]
    private void ReportAbilityActivatedServerRpc(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        PlayAbilityVisualClientRpc(abilityType, position, direction);
    }

    [ClientRpc]
    private void PlayAbilityVisualClientRpc(CharacterAbilityType abilityType, Vector2 position, Vector2 direction)
    {
        if (IsOwner) return;

        if (TryGetComponent<PlayerAnimationHandler>(out var anim))
            anim.PlayAbilityAnimation(abilityType);
    }
}
