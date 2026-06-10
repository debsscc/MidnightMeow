using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Aplica dano em jogadores no servidor (telegraph inimigo, projéteis, etc.) com feedback replicado.
/// </summary>
public static class PlayerCombatUtility
{
    public static bool TryApplyDamage(Collider2D collider, float amount, GameObject instigator)
    {
        if (collider == null || amount <= 0f)
            return false;

        var networkHealth = collider.GetComponentInParent<NetworkPlayerHealth>();
        if (networkHealth != null && networkHealth.IsSpawned)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return false;

            return networkHealth.ServerApplyExternalDamage(amount, instigator);
        }

        var damageable = collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(amount, instigator);
            return true;
        }

        return false;
    }
}
