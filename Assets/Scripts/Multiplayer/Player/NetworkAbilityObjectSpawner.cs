using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawna objetos de habilidade (barreira, poça) no servidor com replicação NGO.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkAbilityObjectSpawner : NetworkBehaviour
{
    public void SpawnBarrier(GameObject prefab, Vector2 position, Vector2 direction, AbilityTierData tierData)
    {
        if (prefab == null) return;

        if (IsSpawned && IsOwner)
            SpawnAbilityObjectServerRpc(
                CharacterAbilityType.CoraBarrier,
                position,
                direction,
                PackTier(tierData));
        else if (!IsSpawned)
            SpawnLocal(prefab, position, direction, tierData, CharacterAbilityType.CoraBarrier);
    }

    public void SpawnPool(GameObject prefab, Vector2 position, AbilityTierData tierData)
    {
        if (prefab == null) return;

        if (IsSpawned && IsOwner)
            SpawnAbilityObjectServerRpc(
                CharacterAbilityType.CoraPool,
                position,
                Vector2.zero,
                PackTier(tierData));
        else if (!IsSpawned)
            SpawnLocal(prefab, position, Vector2.zero, tierData, CharacterAbilityType.CoraPool);
    }

    private void SpawnLocal(
        GameObject prefab,
        Vector2 position,
        Vector2 direction,
        AbilityTierData tierData,
        CharacterAbilityType type)
    {
        var rotation = type == CharacterAbilityType.CoraBarrier
            ? AbilityPlacementUtility.RotationFromDirection(direction)
            : Quaternion.identity;
        var instance = Instantiate(prefab, position, rotation);
        ulong ownerId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        if (type == CharacterAbilityType.CoraBarrier && instance.TryGetComponent<CoraBarrier>(out var barrier))
            barrier.Initialize(tierData, ownerId);
        else if (type == CharacterAbilityType.CoraPool && instance.TryGetComponent<CoraDamagePool>(out var pool))
            pool.Initialize(tierData, ownerId);
    }

    [Rpc(SendTo.Server)]
    private void SpawnAbilityObjectServerRpc(
        CharacterAbilityType abilityType,
        Vector2 position,
        Vector2 direction,
        AbilityTierPayload payload)
    {
        var handler = GetComponent<PlayerAbilityHandler>();
        if (handler == null) return;

        GameObject prefab = handler.GetSpawnPrefab(abilityType);
        if (prefab == null) return;

        var rotation = abilityType == CharacterAbilityType.CoraBarrier
            ? AbilityPlacementUtility.RotationFromDirection(direction)
            : Quaternion.identity;
        var instance = Instantiate(prefab, position, rotation);
        var netObj = instance.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();

        var tierData = UnpackTier(payload);
        if (abilityType == CharacterAbilityType.CoraBarrier)
        {
            if (instance.TryGetComponent<NetworkCoraBarrier>(out var networkBarrier))
                networkBarrier.ServerPublishInitialize(tierData, OwnerClientId);
            else if (instance.TryGetComponent<CoraBarrier>(out var barrier))
                barrier.Initialize(tierData, OwnerClientId);
        }
        else if (abilityType == CharacterAbilityType.CoraPool && instance.TryGetComponent<CoraDamagePool>(out var pool))
            pool.Initialize(tierData, OwnerClientId);
    }

    private static AbilityTierPayload PackTier(AbilityTierData data)
    {
        return new AbilityTierPayload
        {
            Range = data.range,
            Damage = data.damage,
            SlowMultiplier = data.slowMultiplier,
            SlowDuration = data.slowDuration,
            StunDuration = data.stunDuration,
            KnockbackDistance = data.knockbackDistance,
            KnockbackDuration = data.knockbackDuration,
            EffectDuration = data.effectDuration,
            AreaWidth = data.areaWidth,
            DamagePerSecond = data.damagePerSecond
        };
    }

    private static AbilityTierData UnpackTier(AbilityTierPayload payload)
    {
        return new AbilityTierData
        {
            range = payload.Range,
            damage = payload.Damage,
            slowMultiplier = payload.SlowMultiplier,
            slowDuration = payload.SlowDuration,
            stunDuration = payload.StunDuration,
            knockbackDistance = payload.KnockbackDistance,
            knockbackDuration = payload.KnockbackDuration,
            effectDuration = payload.EffectDuration,
            areaWidth = payload.AreaWidth,
            damagePerSecond = payload.DamagePerSecond
        };
    }

    private struct AbilityTierPayload : INetworkSerializable
    {
        public float Range;
        public float Damage;
        public float SlowMultiplier;
        public float SlowDuration;
        public float StunDuration;
        public float KnockbackDistance;
        public float KnockbackDuration;
        public float EffectDuration;
        public float AreaWidth;
        public float DamagePerSecond;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Range);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref SlowMultiplier);
            serializer.SerializeValue(ref SlowDuration);
            serializer.SerializeValue(ref StunDuration);
            serializer.SerializeValue(ref KnockbackDistance);
            serializer.SerializeValue(ref KnockbackDuration);
            serializer.SerializeValue(ref EffectDuration);
            serializer.SerializeValue(ref AreaWidth);
            serializer.SerializeValue(ref DamagePerSecond);
        }
    }
}
