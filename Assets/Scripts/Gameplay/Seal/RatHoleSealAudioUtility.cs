// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: SFX de selamento de buraco via grupo SFX do AudioMixer.
// ----------------------------------------------------------------

using UnityEngine;

public static class RatHoleSealAudioUtility
{
    public static void PlaySealComplete(RatHoleSealConfig config)
    {
        AudioClip clip = config != null ? config.sealCompleteClip : null;
        if (clip == null)
            return;

        GameAudioSettings.EnsureExists();

        GameObject temp = new GameObject("RatHoleSealSfx");
        AudioSource source = temp.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        GameAudioSettings.BindSfxOutput(source);
        source.PlayOneShot(clip);
        Object.Destroy(temp, clip.length + 0.15f);
    }
}
