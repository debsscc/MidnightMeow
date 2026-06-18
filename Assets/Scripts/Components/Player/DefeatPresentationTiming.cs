using UnityEngine;

/// <summary>
/// Duração compartilhada entre apresentação de morte, fade da horda e UI de derrota.
/// </summary>
public static class DefeatPresentationTiming
{
    public const float DefeatUiBufferSeconds = 0.35f;

    public static float ResolveDeathClipSeconds(CharacterAnimationProfile profile)
    {
        return AnimatorDeathTimingUtility.ResolveConfiguredClipLength(profile, 1f);
    }

    public static float ResolvePostDeathHoldSeconds(PlayerDeathPresentation presentation, CharacterAnimationProfile profile)
    {
        if (presentation != null)
            return presentation.PostDeathHoldSeconds;

        if (profile != null && profile.postDeathHoldSeconds > 0f)
            return profile.postDeathHoldSeconds;

        return 5f;
    }

    /// <summary>Quando a UI de derrota deve aparecer (anim + hold + buffer).</summary>
    public static float ResolveDefeatUiDelay(CharacterAnimationProfile profile, PlayerDeathPresentation presentation = null)
    {
        return ResolveDeathClipSeconds(profile)
            + ResolvePostDeathHoldSeconds(presentation, profile)
            + DefeatUiBufferSeconds;
    }

    public static float ResolveDefeatUiDelay(NetworkPlayerHealth downedPlayer)
    {
        if (downedPlayer == null)
            return 6f;

        CharacterAnimationProfile profile = downedPlayer.GetComponent<AnimatorProfileBinder>()?.Profile;
        PlayerDeathPresentation presentation = downedPlayer.GetComponent<PlayerDeathPresentation>();
        return ResolveDefeatUiDelay(profile, presentation);
    }

    public static float ResolveMaxDefeatUiDelayForUnconsciousPlayers()
    {
        float delay = 0f;

        NetworkPlayerHealth[] players =
            Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsUnconscious)
                continue;

            delay = Mathf.Max(delay, ResolveDefeatUiDelay(health));
        }

        return delay;
    }
}
