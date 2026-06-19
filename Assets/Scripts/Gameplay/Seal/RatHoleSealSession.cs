using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado replicado de selamento por buraco (servidor autoritativo).
/// </summary>
public struct RatHoleSealSession : INetworkSerializable, IEquatable<RatHoleSealSession>
{
    public const byte FlagSealed = 1;
    public const byte FlagActive = 2;

    public ushort HoleId;
    public byte Flags;
    public float Progress;
    public float AbandonTimer;
    public Vector2 ZoneA;
    public Vector2 ZoneB;
    public byte ZoneCount;

    public bool IsSealed => (Flags & FlagSealed) != 0;
    public bool IsActive => (Flags & FlagActive) != 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HoleId);
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Progress);
        serializer.SerializeValue(ref AbandonTimer);
        serializer.SerializeValue(ref ZoneA);
        serializer.SerializeValue(ref ZoneB);
        serializer.SerializeValue(ref ZoneCount);
    }

    public bool Equals(RatHoleSealSession other) =>
        HoleId == other.HoleId &&
        Flags == other.Flags &&
        Mathf.Approximately(Progress, other.Progress) &&
        Mathf.Approximately(AbandonTimer, other.AbandonTimer) &&
        ZoneA == other.ZoneA &&
        ZoneB == other.ZoneB &&
        ZoneCount == other.ZoneCount;

    public override bool Equals(object obj) => obj is RatHoleSealSession other && Equals(other);
    public override int GetHashCode() => HoleId;
}
