/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Acesso síncrono a strings da tabela UI (Unity Localization) quando for trocado por script.
---------------------------------------------------------------- */

using System.Globalization;
using UnityEngine.Localization.Settings;

public static class UiLocalization
{
    private const string TableName = "UI";

    public static string Get(string key, string fallback)
    {
        if (string.IsNullOrEmpty(key))
            return fallback;

        if (LocalizationSettings.Instance == null
            || LocalizationSettings.StringDatabase == null
            || !LocalizationSettings.HasSettings)
            return fallback;

        try
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        string template = Get(key, fallback);
        return args.Length > 0
            ? string.Format(CultureInfo.CurrentCulture, template, args)
            : template;
    }

    public static string FormatLoadingProgress(float progress)
    {
        string prefix = Get("title.loading", "Carregando");
        string percent = progress.ToString("P0", CultureInfo.CurrentCulture);
        return $"{prefix}... {percent}";
    }

    public static string FormatLobbyPlayerCount(int current, int max)
    {
        string label = Get("title.lobby.players", "Jogadores");
        return $"{label} {current}/{max}";
    }

    public static string FormatLobbyCode(string code)
    {
        string label = Get("lobby.code.label", "Codigo");
        return string.IsNullOrWhiteSpace(code)
            ? $"{label}: --"
            : $"{label}: <b>{code.Trim().ToUpper()}</b>";
    }

    public static string TranslateLobbyConnectionMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        string lower = message.ToLowerInvariant();

        if (lower.Contains("join code") && lower.Contains("vazio"))
            return Get("lobby.status.empty_code", message);
        if (lower.Contains("timeout"))
            return Get("lobby.status.connection_timeout", message);
        if (lower.Contains("perdida") || lower.Contains("connection lost"))
            return Get("lobby.status.connection_lost", message);
        if (lower.Contains("erro ao entrar") || lower.Contains("falha ao entrar"))
            return Get("lobby.status.join_failed", message);
        if (lower.Contains("erro ao hospedar"))
            return Get("lobby.status.host_failed", message);
        if (lower.Contains("conectado com sucesso"))
            return Get("lobby.status.connected", message);
        if (lower.Contains("apenas o host"))
            return Get("lobby.status.only_host_starts", message);
        if (lower.Contains("aguardando") && lower.Contains("jogadores"))
            return Format("lobby.status.waiting_players", "Aguardando {0} jogadoras conectadas.", 2);
        if (lower.Contains("relaymanager"))
            return Get("lobby.status.relay_missing", message);
        if (lower.Contains("networkmanager"))
            return Get("lobby.status.network_missing", message);

        return message;
    }

    public static string FormatSaveSlotInfo(int slotNumber, string date, string time)
    {
        return Format(
            "saveFiles.slot.info",
            "Arquivo {0}, salvo em {1} às {2}",
            slotNumber,
            date,
            time);
    }

    public static string FormatSaveDeletePrompt(int slotNumber, string date, string time)
    {
        return Format(
            "saveFiles.delete.prompt",
            "Apagar Arquivo {0}?\n\nSalvo em {1} às {2}\n\nEsta ação não pode ser desfeita.",
            slotNumber,
            date,
            time);
    }

    public static string GetSealPrompt() =>
        Get("seal.prompt", "Aperte E para selar");

    public static string FormatSealProgress(int percent) =>
        Format("seal.progress", "Fique na Área para selar — {0}%", percent);

    public static string GetSealComplete() =>
        Get("seal.complete", "Área selada");

    public static string FormatObjectiveHolesStatus(int sealedCount, int totalHoles, int remaining, int enemiesAlive) =>
        Format(
            "objective.holes_status",
            "Buracos: {0}/{1} selados ({2} faltando)  |  Inimigos: {3}",
            sealedCount,
            totalHoles,
            remaining,
            enemiesAlive);

    public static string FormatObjectiveCarriageStatus(float carriagePercent, int sealedCount, int totalHoles, int remaining, int enemiesAlive) =>
        Format(
            "objective.holes_carriage",
            "Carruagem: {0}%  |  Buracos: {1}/{2} ({3} faltando)  |  Inimigos: {4}",
            carriagePercent.ToString("0", CultureInfo.CurrentCulture),
            sealedCount,
            totalHoles,
            remaining,
            enemiesAlive);

    public static string FormatObjectiveDefeatBoss(int enemiesAlive) =>
        Format("objective.defeat_boss", "Derrote o Boss  |  Inimigos: {0}", enemiesAlive);

    public static string FormatMagiculaCount(int count) =>
        Format("hud.magiculas_count", "{0} magículas", count);
}
