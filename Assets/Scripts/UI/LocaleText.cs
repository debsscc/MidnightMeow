using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Helper de idioma para textos traduzidos direto no código (pt-BR / en-US),
/// sem depender de Localization Tables. Centraliza a checagem do locale ativo.
/// </summary>
public static class LocaleText
{
    /// <summary>True quando o idioma ativo é português (ou quando não há locale definido).</summary>
    public static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }
}
