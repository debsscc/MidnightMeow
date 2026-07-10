// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: Helpers testáveis para pulso/timer de reviver no multiplayer.
// ----------------------------------------------------------------

using UnityEngine;

public static class DownedReviveFeedbackUtility
{
    public static bool ShouldShowFeedback(bool anyFightingAlly, bool canBeRevived, float remainingSeconds)
    {
        if (!anyFightingAlly || !canBeRevived)
            return false;

        return remainingSeconds > 0f;
    }

    public static float ComputeUrgency(float remainingSeconds, float durationSeconds)
    {
        float duration = Mathf.Max(1f, durationSeconds);
        float remaining = Mathf.Max(0f, remainingSeconds);
        return 1f - Mathf.Clamp01(remaining / duration);
    }

    public static float ComputePulseStress(float baseIntensity, float urgency)
    {
        float intensity = Mathf.Clamp(baseIntensity, 0.1f, 0.8f);
        return Mathf.Lerp(intensity * 0.75f, intensity * 1.15f, Mathf.Clamp01(urgency));
    }
}
