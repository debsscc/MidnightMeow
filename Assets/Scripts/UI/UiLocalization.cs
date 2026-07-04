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
}
