using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado replicado de conserto da carruagem (espelha <see cref="DownedReviveSession"/>).
/// </summary>
public struct CarriageRepairSession : INetworkSerializable, IEquatable<CarriageRepairSession>
{
    public const byte FlagCompleted = 1;
    public const byte FlagActive = 2;
    public const int MaxZones = 4;

    public byte Flags;
    public float Progress;
    public float AbandonTimer;
    public Vector2 ZoneA;
    public Vector2 ZoneB;
    public Vector2 ZoneC;
    public Vector2 ZoneD;
    public byte ZoneCount;

    public bool IsCompleted => (Flags & FlagCompleted) != 0;
    public bool IsActive => (Flags & FlagActive) != 0;

    public void CollectZones(List<Vector2> into)
    {
        into.Clear();
        if (ZoneCount <= 0)
            return;

        into.Add(ZoneA);
        if (ZoneCount >= 2) into.Add(ZoneB);
        if (ZoneCount >= 3) into.Add(ZoneC);
        if (ZoneCount >= 4) into.Add(ZoneD);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Flags);
        serializer.SerializeValue(ref Progress);
        serializer.SerializeValue(ref AbandonTimer);
        serializer.SerializeValue(ref ZoneA);
        serializer.SerializeValue(ref ZoneB);
        serializer.SerializeValue(ref ZoneC);
        serializer.SerializeValue(ref ZoneD);
        serializer.SerializeValue(ref ZoneCount);
    }

    public bool Equals(CarriageRepairSession other) =>
        Flags == other.Flags &&
        Mathf.Approximately(Progress, other.Progress) &&
        Mathf.Approximately(AbandonTimer, other.AbandonTimer) &&
        ZoneA == other.ZoneA &&
        ZoneB == other.ZoneB &&
        ZoneC == other.ZoneC &&
        ZoneD == other.ZoneD &&
        ZoneCount == other.ZoneCount;

    public override bool Equals(object obj) => obj is CarriageRepairSession other && Equals(other);
    public override int GetHashCode() => Flags.GetHashCode();
}

/// <summary>Tick de progresso: pelo menos 1 jogador em qualquer zona avança o conserto.</summary>
public static class CarriageRepairZoneSystem
{
    private static readonly List<Vector2> ZoneBuffer = new List<Vector2>(CarriageRepairSession.MaxZones);

    public static void TickSession(ref CarriageRepairSession session, CarriageConfig config, float deltaTime)
    {
        if (!session.IsActive || session.IsCompleted || config == null)
            return;

        session.CollectZones(ZoneBuffer);

        int occupiedZones = CooperativeZonePlacementUtility.CountPlayersInZones(
            ZoneBuffer, config.repairZoneRadius, requireDistinctZones: false);

        if (occupiedZones <= 0)
        {
            session.AbandonTimer += deltaTime;
            if (session.AbandonTimer >= config.repairAbandonTimeout)
            {
                session.Flags &= unchecked((byte)~CarriageRepairSession.FlagActive);
                session.Progress = 0f;
                session.AbandonTimer = 0f;
            }

            return;
        }

        session.AbandonTimer = 0f;
        float speed = 1f / Mathf.Max(0.1f, config.repairDuration);
        if (session.ZoneCount > 1 && occupiedZones >= 2)
            speed *= config.repairDualPlayerSpeedMultiplier;

        session.Progress = Mathf.Clamp01(session.Progress + speed * deltaTime);
        if (session.Progress < 1f)
            return;

        session.Flags |= CarriageRepairSession.FlagCompleted;
        session.Flags &= unchecked((byte)~CarriageRepairSession.FlagActive);
        session.Progress = 1f;
    }
}
