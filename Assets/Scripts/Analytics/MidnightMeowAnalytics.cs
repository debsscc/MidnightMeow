using System.Collections.Generic;
using GameAnalyticsSDK;
using UnityEngine;

/// <summary>
/// API central de analytics do MidnightMeow (GameAnalytics).
/// Cada método documenta o que mede e onde ver no dashboard (Design / Progression / Resource).
/// </summary>
public static class MidnightMeowAnalytics
{
    private static MidnightMeowAnalyticsConfig _config;

    public static bool IsReady => GameAnalytics.Initialized;

    internal static void BindConfig(MidnightMeowAnalyticsConfig config) => _config = config;

    // -------------------------------------------------------------------------
    // Fluxo de telas (UI/UX) — Design Event: screen:*
    // Dashboard → criar dashboard custom → filtrar event_id "screen:..."
    // Mede: tempo entre telas, rotas mais usadas, abandono no fluxo Menu→Lobby→Prep→Gameplay
    // -------------------------------------------------------------------------

    /// <summary>Transição de cena/rota iniciada (fade/loading).</summary>
    public static void TrackScreenTransitionStarted(string routeId, string targetScene)
    {
        string id = Sanitize($"screen:transition_start:{routeId}");
        SendDesign(id, 0f, new Dictionary<string, object>
        {
            { "target_scene", targetScene ?? string.Empty }
        });
    }

    /// <summary>Cena carregada após transição — fim do loading.</summary>
    public static void TrackScreenArrived(string sceneName, string routeId = "")
    {
        string id = Sanitize(string.IsNullOrEmpty(routeId)
            ? $"screen:arrived:{sceneName}"
            : $"screen:arrived:{routeId}");
        SendDesign(id);
    }

    /// <summary>Fase macro do fluxo (ContractSelect, Gameplay, etc.).</summary>
    public static void TrackFlowPhase(ScreenFlowPhase phase)
    {
        // Mede em qual etapa do funil o jogador está (preparação vs gameplay).
        SendDesign(Sanitize($"screen:phase:{phase}"));
    }

    /// <summary>Clique em botão de UI — mede uso de menus e painéis.</summary>
    public static void TrackUiClick(string screen, string action)
    {
        SendDesign(Sanitize($"ui:{screen}:{action}"));
    }

    // -------------------------------------------------------------------------
    // Sessão / modo — Design Event: session:*
    // -------------------------------------------------------------------------

    /// <summary>Define dimensão custom 01: solo vs multiplayer (persiste na sessão GA).</summary>
    public static void SetSessionMode(bool isMultiplayer)
    {
        if (!IsReady)
            return;

        // Custom Dimension 01 — configurada em Settings (solo / multiplayer).
        GameAnalytics.SetCustomDimension01(isMultiplayer ? "multiplayer" : "solo");
    }

    /// <summary>Contrato/fase escolhida antes do gameplay.</summary>
    public static void TrackContractSelected(int contractIndex, string sceneName)
    {
        SendDesign(Sanitize($"session:contract:{contractIndex}"), contractIndex, new Dictionary<string, object>
        {
            { "gameplay_scene", sceneName ?? string.Empty }
        });
    }

    // -------------------------------------------------------------------------
    // Gameplay — Progression + Design
    // Progression: Dashboard → Progression (funil por fase/wave)
    // -------------------------------------------------------------------------

    /// <summary>Início de uma run (entrou na cena Fase-*).</summary>
    public static void TrackRunStart(string sceneName)
    {
        // Progression Start — início da tentativa na fase.
        SendProgression(GAProgressionStatus.Start, sceneName, string.Empty);
        SendDesign(Sanitize($"run:start:{sceneName}"));
    }

    /// <summary>Nova wave iniciada.</summary>
    public static void TrackWaveStarted(int wave, int totalWaves, string sceneName)
    {
        // Progression Start no "nível" wave — até onde a partida chegou.
        SendProgression(GAProgressionStatus.Start, sceneName, $"wave_{wave}");
        SendDesign(Sanitize($"run:wave_start:{wave}"), wave, new Dictionary<string, object>
        {
            { "total_waves", totalWaves }
        });
    }

    /// <summary>Wave concluída (todos inimigos mortos).</summary>
    public static void TrackWaveCompleted(int wave, string sceneName)
    {
        SendProgression(GAProgressionStatus.Complete, sceneName, $"wave_{wave}");
        SendDesign(Sanitize($"run:wave_complete:{wave}"), wave);
    }

    /// <summary>Jogador local morreu (ainda pode reviver em MP).</summary>
    public static void TrackPlayerDowned(int downCountThisRun)
    {
        SendDesign(Sanitize("run:player_downed"), downCountThisRun);
    }

    /// <summary>Inimigo morto pelo jogador local.</summary>
    public static void TrackEnemyKill(int totalKillsThisRun)
    {
        SendDesign(Sanitize("run:enemy_kill"), totalKillsThisRun);
    }

    /// <summary>Pausa/despausa — mede fricção e tempo parado.</summary>
    public static void TrackPauseChanged(bool paused, float pausedSecondsSoFar)
    {
        SendDesign(Sanitize(paused ? "run:pause" : "run:resume"), pausedSecondsSoFar);
    }

    /// <summary>Adrenalina baixa — mecânica de pressão do combate.</summary>
    public static void TrackAdrenalineLow()
    {
        SendDesign(Sanitize("run:adrenaline_low"));
    }

    // -------------------------------------------------------------------------
    // Economia da run — Resource Event
    // Dashboard → Economy / Resources (fluxo de ciência e magículas)
    // Enviado agregado no fim da run para não spammar o SDK.
    // -------------------------------------------------------------------------

    /// <summary>Total de ciência/magículas coletadas na run (agregado).</summary>
    public static void TrackScienceCollectedTotal(int totalAmount)
    {
        if (totalAmount <= 0 || !IsReady)
            return;

        // Resource Source — moeda "science" ganha na run.
        GameAnalytics.NewResourceEvent(
            GAResourceFlowType.Source,
            "science",
            totalAmount,
            "pickup",
            "run_total");
    }

    /// <summary>Munição coletada na run (agregado).</summary>
    public static void TrackAmmoCollectedTotal(int totalAmount)
    {
        if (totalAmount <= 0 || !IsReady)
            return;

        GameAnalytics.NewResourceEvent(
            GAResourceFlowType.Source,
            "ammo",
            totalAmount,
            "pickup",
            "run_total");
    }

    // -------------------------------------------------------------------------
    // Fim de run — Design + Progression (Fail/Complete)
    // value = segundos vivos; use média no dashboard custom.
    // -------------------------------------------------------------------------

    /// <summary>Resumo ao terminar vitória ou derrota.</summary>
    public static void TrackRunEnd(
        string sceneName,
        bool victory,
        float survivalSeconds,
        int enemiesKilled,
        int scienceCollected,
        int ammoCollected,
        int maxWaveReached,
        int playerDownCount,
        float totalPausedSeconds)
    {
        GAProgressionStatus status = victory ? GAProgressionStatus.Complete : GAProgressionStatus.Fail;
        string waveStep = maxWaveReached > 0 ? $"wave_{maxWaveReached}" : "start";

        // Progression Complete/Fail — funil principal de "até onde foi".
        SendProgression(status, sceneName, waveStep);

        var fields = new Dictionary<string, object>
        {
            { "enemies_killed", enemiesKilled },
            { "science_collected", scienceCollected },
            { "ammo_collected", ammoCollected },
            { "max_wave", maxWaveReached },
            { "player_downs", playerDownCount },
            { "paused_seconds", Mathf.RoundToInt(totalPausedSeconds) },
            { "victory", victory ? 1 : 0 }
        };

        // Design — value = tempo vivo (segundos) para gráfico de média/mediana.
        SendDesign(Sanitize(victory ? "run:end:victory" : "run:end:defeat"), survivalSeconds, fields);

        TrackScienceCollectedTotal(scienceCollected);
        TrackAmmoCollectedTotal(ammoCollected);
    }

    // -------------------------------------------------------------------------
    // Multiplayer — Design Event: mp:*
    // -------------------------------------------------------------------------

    public static void TrackPlayerJoined(ulong clientId, bool isLocalPlayer)
    {
        if (!isLocalPlayer)
            return;

        SendDesign(Sanitize("mp:local_joined"), clientId);
    }

    public static void TrackPlayerLeft(ulong clientId)
    {
        SendDesign(Sanitize("mp:player_left"), clientId);
    }

    public static void TrackAllPlayersDefeated()
    {
        SendDesign(Sanitize("mp:team_wipe"));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void SendDesign(string eventId, float value = 0f, Dictionary<string, object> fields = null)
    {
        if (!IsReady)
            return;

        if (_config != null && _config.logEventsInConsole)
            Debug.Log($"[Analytics] Design: {eventId} value={value}");

        if (fields != null && fields.Count > 0)
            GameAnalytics.NewDesignEvent(eventId, value, fields);
        else if (value > 0f)
            GameAnalytics.NewDesignEvent(eventId, value);
        else
            GameAnalytics.NewDesignEvent(eventId);
    }

    private static void SendProgression(GAProgressionStatus status, string world, string step)
    {
        if (!IsReady)
            return;

        world = Sanitize(world);
        step = Sanitize(step);

        if (_config != null && _config.logEventsInConsole)
            Debug.Log($"[Analytics] Progression: {status} {world}/{step}");

        if (string.IsNullOrEmpty(step))
            GameAnalytics.NewProgressionEvent(status, world);
        else
            GameAnalytics.NewProgressionEvent(status, world, step);
    }

    private static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "unknown";

        return raw.ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('-', '_')
            .Replace('.', '_');
    }
}
