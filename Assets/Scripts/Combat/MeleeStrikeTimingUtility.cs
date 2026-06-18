using UnityEngine;

/// <summary>
/// Calcula strike/recovery do melee a partir do clip e multiplicador de velocidade (data-driven).
/// </summary>
public static class MeleeStrikeTimingUtility
{
    public static float ComputeStrikeDelay(MeleeCombatStats stats, float clipLength, float attackSpeedMultiplier)
    {
        if (stats == null)
            return 0.25f;

        float normalized = Mathf.Clamp01(stats.strikeNormalizedTime);
        float clip = clipLength > 0f ? clipLength : 0.333f;
        float speed = Mathf.Max(0.1f, attackSpeedMultiplier);
        return clip * normalized / speed;
    }

    public static float ComputeRecoveryDelay(MeleeCombatStats stats, float clipLength, float attackSpeedMultiplier)
    {
        if (stats == null)
            return 0f;

        float speed = Mathf.Max(0.1f, attackSpeedMultiplier);
        float clip = clipLength > 0f ? clipLength : 0.333f;
        float recoveryNormalized = stats.recoveryNormalizedTime > 0f
            ? Mathf.Clamp01(stats.recoveryNormalizedTime)
            : 1f - Mathf.Clamp01(stats.strikeNormalizedTime);

        if (recoveryNormalized <= 0f)
            return 0f;

        return clip * recoveryNormalized / speed;
    }

    public static float ComputeStrikeDeadline(MeleeCombatStats stats, float clipLength, float attackSpeedMultiplier)
    {
        return ComputeStrikeDelay(stats, clipLength, attackSpeedMultiplier);
    }
}
