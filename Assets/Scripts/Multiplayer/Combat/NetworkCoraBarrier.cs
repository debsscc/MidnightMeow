using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Replica Initialize da barreira da Cora em todos os peers e gerencia despawn em rede.
/// </summary>
[RequireComponent(typeof(CoraBarrier), typeof(NetworkObject))]
public class NetworkCoraBarrier : NetworkBehaviour
{
    private CoraBarrier _barrier;

    private void Awake()
    {
        _barrier = GetComponent<CoraBarrier>();
    }

    public void ServerPublishInitialize(AbilityTierData tierData, ulong ownerClientId)
    {
        if (!IsServer)
            return;

        _barrier.Initialize(tierData, ownerClientId);
        SyncInitializeClientRpc(PackTier(tierData), ownerClientId);
    }

    [ClientRpc]
    private void SyncInitializeClientRpc(AbilityTierPayload payload, ulong ownerClientId)
    {
        if (IsServer)
            return;

        _barrier.Initialize(UnpackTier(payload), ownerClientId);
    }

    public void ServerScheduleDespawn(float delay)
    {
        if (!IsServer)
            return;

        if (delay <= 0f)
            DespawnBarrier();
        else
            Invoke(nameof(DespawnBarrier), delay);
    }

    private void DespawnBarrier()
    {
        if (!IsServer)
            return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
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
