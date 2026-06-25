using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conta buracos selados e emite status para HUD.
/// </summary>
public static class PhaseObjectiveStatusUtility
{
    public static int CachedEnemiesAlive { get; private set; }

    public static void SetCachedEnemiesAlive(int enemiesAlive) =>
        CachedEnemiesAlive = Mathf.Max(0, enemiesAlive);

    public static void CountSealedHoles(out int sealedCount, out int totalCount)
    {
        sealedCount = 0;
        totalCount = 0;

        NetworkRatHoleSealManager sealManager = NetworkRatHoleSealManager.Instance;
        if (sealManager != null && sealManager.IsSpawned)
        {
            foreach (RatHoleSealSession session in sealManager.Sessions)
            {
                totalCount++;
                if (session.IsSealed)
                    sealedCount++;
            }
        }

        if (totalCount > 0)
            return;

        IReadOnlyList<RatHoleSpawnPoint> holes = RatHoleSpawnPoint.All;
        totalCount = holes.Count;
        for (int i = 0; i < holes.Count; i++)
        {
            if (holes[i] != null && holes[i].IsSealed)
                sealedCount++;
        }
    }

    public static int CountAliveNetworkEnemies()
    {
        NetworkWaveManager waveManager = NetworkWaveManager.Instance;
        if (waveManager != null && waveManager.IsSpawned)
            return waveManager.EnemiesAlive;

        if (CachedEnemiesAlive > 0)
            return CachedEnemiesAlive;

        int count = 0;
        NetworkEnemyController[] enemies = Object.FindObjectsByType<NetworkEnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            NetworkEnemyController enemy = enemies[i];
            if (enemy != null && enemy.IsSpawned && !enemy.IsDeadOnNetwork)
                count++;
        }

        return count;
    }

    public static void BroadcastCurrentStatus(int enemiesAlive)
    {
        SetCachedEnemiesAlive(enemiesAlive);
        CountSealedHoles(out int sealedCount, out int totalCount);
        GameEvents.InvokePhaseObjectiveStatusChanged(sealedCount, totalCount, enemiesAlive);
    }
}
