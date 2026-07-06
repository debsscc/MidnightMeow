using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado replicado de reviver por jogador caído (espelha <see cref="RatHoleSealSession"/>).
/// </summary>
public struct DownedReviveSession : INetworkSerializable, IEquatable<DownedReviveSession>
{
    public const byte FlagCompleted = 1;
    public const byte FlagActive = 2;

    public ulong DownedClientId;
    public byte Flags;
    public float Progress;
    public float AbandonTimer;
    public Vector2 ZoneA;
    public Vector2 ZoneB;
    public byte ZoneCount;

    public bool IsCompleted => (Flags & FlagCompleted) != 0;
    public bool IsActive => (Flags & FlagActive) != 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DownedClientId);
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Progress);
        serializer.SerializeValue(ref AbandonTimer);
        serializer.SerializeValue(ref ZoneA);
        serializer.SerializeValue(ref ZoneB);
        serializer.SerializeValue(ref ZoneCount);
    }

    public bool Equals(DownedReviveSession other) =>
        DownedClientId == other.DownedClientId &&
        Flags == other.Flags &&
        Mathf.Approximately(Progress, other.Progress) &&
        Mathf.Approximately(AbandonTimer, other.AbandonTimer) &&
        ZoneA == other.ZoneA &&
        ZoneB == other.ZoneB &&
        ZoneCount == other.ZoneCount;

    public override bool Equals(object obj) => obj is DownedReviveSession other && Equals(other);
    public override int GetHashCode() => DownedClientId.GetHashCode();
}
