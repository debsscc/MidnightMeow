using UnityEngine;

/// <summary>
/// Lê o momento de soltura (PerformFire) diretamente do AnimationClip.
/// </summary>
public static class AnimationClipFireReleaseUtility
{
    public const string PerformFireFunctionName = "PerformFire";

    public static bool TryGetReleaseNormalizedTime(AnimationClip clip, out float normalizedTime)
    {
        normalizedTime = 0.45f;

        if (clip == null || clip.length <= Mathf.Epsilon)
            return false;

        AnimationEvent[] events = clip.events;
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i].functionName != PerformFireFunctionName)
                continue;

            normalizedTime = Mathf.Clamp01(events[i].time / clip.length);
            return true;
        }

        return false;
    }
}
