using UnityEngine;

public static class PlayerSfxUtility
{
    public static void PlayOneShot(AudioSource source, AudioEventSO audioEvent)
    {
        if (source == null || audioEvent == null)
            return;

        if (!audioEvent.TryResolvePlayback(out AudioClip clip, out float volume, out float pitch))
            return;

        GameAudioSettings.BindSfxOutput(source);
        float previousPitch = source.pitch;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        source.pitch = previousPitch;
    }
}
