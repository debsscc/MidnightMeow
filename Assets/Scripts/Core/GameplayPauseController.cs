using UnityEngine;

/// <summary>
/// Congelamento imediato de gameplay ao pausar (solo e MP).
/// Complementa <see cref="Time.timeScale"/> e <see cref="GameEvents.IsPaused"/>.
/// Invocado por <see cref="GameEvents.InvokePauseChanged"/>.
/// </summary>
public static class GameplayPauseController
{
    public static void ApplyImmediateFreeze()
    {
        FreezePlayerBodies();
        FreezeEnemyCombat();
        FreezeSpawners();
        FreezeTelegraphZones();
    }

    private static void FreezePlayerBodies()
    {
        PlayerMovement[] movements = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < movements.Length; i++)
        {
            if (movements[i] != null)
                movements[i].FreezeForPause();
        }

        PlayerDash[] dashes = Object.FindObjectsByType<PlayerDash>(FindObjectsSortMode.None);
        for (int i = 0; i < dashes.Length; i++)
        {
            if (dashes[i] != null)
                dashes[i].FreezeForPause();
        }

        Rigidbody2D[] bodies = Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];
            if (body == null || !body.gameObject.CompareTag("Player"))
                continue;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private static void FreezeEnemyCombat()
    {
        EnemyTelegraphedAttacker[] attackers = Object.FindObjectsByType<EnemyTelegraphedAttacker>(FindObjectsSortMode.None);
        for (int i = 0; i < attackers.Length; i++)
        {
            if (attackers[i] != null)
                attackers[i].FreezeForPause();
        }

        EnemyMovement[] movers = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null)
                movers[i].FreezeForPause();
        }
    }

    private static void FreezeSpawners()
    {
        if (RatHoleSpawnOrchestrator.Instance != null)
            RatHoleSpawnOrchestrator.Instance.SetSpawnPaused(true);

        WaveGenerator waveGenerator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        waveGenerator?.SetSpawnPaused(true);
    }

    private static void FreezeTelegraphZones()
    {
        EnemyTelegraphZoneInstance[] zones = Object.FindObjectsByType<EnemyTelegraphZoneInstance>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null)
                zones[i].CancelForPause();
        }
    }

    public static void ReleaseSpawners()
    {
        if (RatHoleSpawnOrchestrator.Instance != null)
            RatHoleSpawnOrchestrator.Instance.SetSpawnPaused(false);

        WaveGenerator waveGenerator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        waveGenerator?.SetSpawnPaused(false);
    }
}
