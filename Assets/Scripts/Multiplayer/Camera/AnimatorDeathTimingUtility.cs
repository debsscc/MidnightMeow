using UnityEngine;

/// <summary>
/// Lê duração de clip de morte a partir do Animator em runtime (após transição).
/// </summary>
public static class AnimatorDeathTimingUtility
{
    public static float MeasureCurrentStateLength(Animator animator, int layer = 0, float fallbackSeconds = 1f)
    {
        if (animator == null)
            return fallbackSeconds;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
        if (state.length > 0.05f)
            return state.length;

        return fallbackSeconds;
    }

    public static float ResolveConfiguredClipLength(CharacterAnimationProfile profile, float fallbackSeconds = 1f)
    {
        if (profile == null)
            return fallbackSeconds;

        string stateName = string.IsNullOrEmpty(profile.deathAnimatorStateName)
            ? "Dying"
            : profile.deathAnimatorStateName;

        if (profile.clipOverrides != null)
        {
            for (int i = 0; i < profile.clipOverrides.Length; i++)
            {
                AnimatorClipOverrideEntry entry = profile.clipOverrides[i];
                if (entry.clip == null || string.IsNullOrEmpty(entry.stateName))
                    continue;

                if (string.Equals(entry.stateName, stateName, System.StringComparison.OrdinalIgnoreCase))
                    return entry.clip.length;
            }
        }

        if (profile.deathClipLengthFallback > 0f)
            return profile.deathClipLengthFallback;

        return fallbackSeconds;
    }
}
