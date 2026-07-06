using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tick de servidor para progresso/cancelamento do reviver cooperativo (transplante de <see cref="RatHoleSealZoneSystem"/>).
/// </summary>
public static class DownedReviveZoneSystem
{
    public static void TickSession(
        ref DownedReviveSession session,
        DownedPlayerConfig config,
        float deltaTime)
    {
        if (!session.IsActive || session.IsCompleted || config == null)
            return;

        var zones = new List<Vector2>(2);
        zones.Add(session.ZoneA);
        if (session.ZoneCount > 1)
            zones.Add(session.ZoneB);

        int occupiedZones = CooperativeZonePlacementUtility.CountPlayersInZones(
            zones,
            config.reviveZoneRadius,
            requireDistinctZones: session.ZoneCount > 1);

        if (occupiedZones <= 0)
        {
            session.AbandonTimer += deltaTime;
            if (session.AbandonTimer >= config.reviveAbandonTimeout)
            {
                session.Flags &= unchecked((byte)~DownedReviveSession.FlagActive);
                session.Progress = 0f;
                session.AbandonTimer = 0f;
            }

            return;
        }

        session.AbandonTimer = 0f;
        float speed = 1f / Mathf.Max(0.1f, config.reviveZoneFillDuration);
        if (session.ZoneCount > 1 && occupiedZones >= 2)
            speed *= config.reviveDualPlayerSpeedMultiplier;

        session.Progress = Mathf.Clamp01(session.Progress + speed * deltaTime);
        if (session.Progress < 1f)
            return;

        session.Flags |= DownedReviveSession.FlagCompleted;
        session.Flags &= unchecked((byte)~DownedReviveSession.FlagActive);
        session.Progress = 1f;
    }
}
