///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Formata o texto da dica do tutorial (contador 0/3 e teclas Q/R).
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Lógica pura de formatação do texto da HUD do tutorial.
/// </summary>
public static class TutorialTipDisplayFormatter
{
    /// <summary>
    /// Monta o texto exibido. Com <paramref name="requiredCount"/> &gt; 1, anexa " atual/total".
    /// </summary>
    public static string Format(string tipText, int currentProgress, int requiredCount)
    {
        string text = tipText ?? string.Empty;
        int required = Mathf.Max(1, requiredCount);
        if (required <= 1)
            return text;

        int current = Mathf.Clamp(currentProgress, 0, required);
        return $"{text} {current}/{required}";
    }

    /// <summary>
    /// Anexa as teclas ainda pendentes (Q / R). Teclas já usadas não aparecem.
    /// </summary>
    /// <param name="usedAbilityMask">Bit 0 = Q usado; bit 1 = R usado.</param>
    public static string FormatAbilityKeys(string tipText, int usedAbilityMask)
    {
        string text = tipText ?? string.Empty;
        bool showQ = (usedAbilityMask & (1 << 0)) == 0;
        bool showR = (usedAbilityMask & (1 << 1)) == 0;

        if (!showQ && !showR)
            return text;

        if (showQ && showR)
            return $"{text} Q R";
        if (showQ)
            return $"{text} Q";
        return $"{text} R";
    }
}
