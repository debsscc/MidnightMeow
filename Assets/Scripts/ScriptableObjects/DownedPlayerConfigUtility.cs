using UnityEngine;

/// <summary>
/// Resolve <see cref="DownedPlayerConfig"/> em runtime (mesmo padrão de fallback do selamento via Resources).
/// </summary>
public static class DownedPlayerConfigUtility
{
    private static DownedPlayerConfig _cached;

    public static DownedPlayerConfig Resolve(DownedPlayerConfig candidate = null)
    {
        if (candidate != null)
            return candidate;

        if (_cached != null)
            return _cached;

        NetworkDownedReviveManager reviveManager = NetworkDownedReviveManager.Instance;
        if (reviveManager != null && reviveManager.Config != null)
        {
            _cached = reviveManager.Config;
            return _cached;
        }

        MultiplayerGameManager gameManager = MultiplayerGameManager.Instance;
        if (gameManager != null && gameManager.DownedPlayerConfig != null)
        {
            _cached = gameManager.DownedPlayerConfig;
            return _cached;
        }

        _cached = Resources.Load<DownedPlayerConfig>("DownedPlayerConfig");
        return _cached;
    }

    public static void ClearCache() => _cached = null;
}
