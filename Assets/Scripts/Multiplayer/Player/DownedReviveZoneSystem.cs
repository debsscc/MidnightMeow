using UnityEngine;

/// <summary>
/// Servidor: progresso de reviver por permanência na zona ao redor do jogador caído.
/// </summary>
public static class DownedReviveZoneSystem
{
    private static int _lastTickFrame = -1;

    public static void TickServer(DownedPlayerConfig config)
    {
        if (config == null) return;
        if (_lastTickFrame == Time.frameCount) return;
        _lastTickFrame = Time.frameCount;

        float radius = config.reviveZoneRadius;
        float fillDuration = Mathf.Max(0.1f, config.reviveZoneFillDuration);
        float decayPerSecond = Mathf.Max(0f, config.reviveZoneProgressDecayPerSecond);

        foreach (var downed in Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!downed.IsSpawned || !downed.CanBeRevived) continue;

            bool allyInside = HasAllyInsideZone(downed, radius, out int count);

            if (allyInside)
            {
                downed.ServerSetRevivePaused(true);
                float next = downed.ReviveProgress + Time.deltaTime / fillDuration;
                downed.ServerSetReviveProgress(next);

                if (next >= 1f)
                    downed.ServerReviveFromUnconscious();
            }
            else
            {
                downed.ServerSetRevivePaused(false);
                if (downed.ReviveProgress > 0f && decayPerSecond > 0f)
                {
                    float next = downed.ReviveProgress - decayPerSecond * Time.deltaTime;
                    downed.ServerSetReviveProgress(next);
                }
            }
        }
    }

    public static bool IsAllyInsideReviveZone(
        NetworkPlayerHealth downed,
        NetworkPlayerHealth ally,
        DownedPlayerConfig config)
    {
        if (downed == null || ally == null || config == null) return false;
        if (!downed.CanBeRevived || !ally.CanFight) return false;
        if (downed.OwnerClientId == ally.OwnerClientId) return false;

        float dist = Vector2.Distance(downed.transform.position, ally.transform.position);
        return dist <= config.reviveZoneRadius;
    }

    private static bool HasAllyInsideZone(NetworkPlayerHealth downed, float radius, out int count)
    {
        count = 0;
        Vector2 center = downed.transform.position;

        foreach (var ally in Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
        {
            if (!ally.IsSpawned || !ally.CanFight) continue;
            if (ally.OwnerClientId == downed.OwnerClientId) continue;

            if (Vector2.Distance(center, ally.transform.position) <= radius)
                count++;
        }

        return count > 0;
    }
}
