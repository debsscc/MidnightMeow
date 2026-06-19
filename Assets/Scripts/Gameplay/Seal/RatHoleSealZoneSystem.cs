using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tick de servidor para progresso/cancelamento do selamento cooperativo.
/// </summary>
public static class RatHoleSealZoneSystem
{
    public static void TickSession(
        ref RatHoleSealSession session,
        RatHoleSealConfig config,
        float deltaTime)
    {
        if (!session.IsActive || session.IsSealed || config == null)
            return;

        var zones = new List<Vector2>(2);
        zones.Add(session.ZoneA);
        if (session.ZoneCount > 1)
            zones.Add(session.ZoneB);

        int occupiedZones = CooperativeZonePlacementUtility.CountPlayersInZones(
            zones,
            config.zoneRadius,
            requireDistinctZones: session.ZoneCount > 1);

        if (occupiedZones <= 0)
        {
            session.AbandonTimer += deltaTime;
            if (session.AbandonTimer >= config.abandonTimeout)
            {
                session.Flags &= unchecked((byte)~RatHoleSealSession.FlagActive);
                session.Progress = 0f;
                session.AbandonTimer = 0f;
            }

            return;
        }

        session.AbandonTimer = 0f;
        float speed = 1f / Mathf.Max(0.1f, config.sealDuration);
        if (session.ZoneCount > 1 && occupiedZones >= 2)
            speed *= config.dualPlayerSpeedMultiplier;

        session.Progress = Mathf.Clamp01(session.Progress + speed * deltaTime);
        if (session.Progress >= 1f)
        {
            session.Flags |= RatHoleSealSession.FlagSealed;
            session.Flags &= unchecked((byte)~RatHoleSealSession.FlagActive);
            session.Progress = 1f;
        }
    }
}
