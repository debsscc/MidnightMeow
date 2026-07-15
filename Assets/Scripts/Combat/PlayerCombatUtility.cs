using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Aplica dano em jogadores e estruturas no servidor (telegraph inimigo, projéteis, etc.) com feedback replicado.
/// </summary>
public static class PlayerCombatUtility
{
    private static int _structureLayer = int.MinValue;

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

        if (TryApplyStructureDamage(collider, amount, instigator))
            return true;

        var damageable = collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                return false;

            damageable.TakeDamage(amount, instigator);
            return true;
        }

        return false;
    }

    private static bool TryApplyStructureDamage(Collider2D collider, float amount, GameObject instigator)
    {
        if (!IsStructureCollider(collider))
            return false;

        var structureHealth = collider.GetComponentInParent<HealthComponent>();
        if (structureHealth == null || !structureHealth.IsAlive)
            return false;

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return false;

        structureHealth.TakeDamage(amount, instigator);
        return true;
    }

    private static bool IsStructureCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        if (collider.CompareTag("Structure"))
            return true;

        Transform root = collider.transform;
        HealthComponent health = collider.GetComponentInParent<HealthComponent>();
        if (health != null)
            root = health.transform;

        if (root.CompareTag("Structure"))
            return true;

        if (root.GetComponent<NetworkCarriageHealth>() != null
            || root.GetComponent<CarriageController>() != null)
            return true;

        int structureLayer = ResolveStructureLayer();
        if (structureLayer >= 0 && collider.gameObject.layer == structureLayer)
            return true;

        if (structureLayer >= 0 && root.gameObject.layer == structureLayer)
            return true;

        return false;
    }

    private static int ResolveStructureLayer()
    {
        if (_structureLayer == int.MinValue)
            _structureLayer = LayerMask.NameToLayer("Structure");
        return _structureLayer;
    }
}
