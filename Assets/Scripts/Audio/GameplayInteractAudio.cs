// ----------------------------------------------------------------
// DESCRIÇÃO: SFX de confirmação ao pressionar Interact (E / gamepad) e de conclusão
// (Reviver) em ações cooperativas de gameplay (selar / consertar / reviver).
// ----------------------------------------------------------------

using UnityEngine;

public static class GameplayInteractAudio
{
    private const string InteractEventResource = "InteractEAudioEvent";
    private const float ReviveCompleteVolume = 0.95f;

    private static AudioEventSO _cachedEvent;
    private static AudioSource _sharedSource;

    /// <summary>Interacao.wav — ao pressionar E para selar, consertar ou iniciar revive.</summary>
    public static void PlayConfirm()
    {
        AudioEventSO audioEvent = ResolveEvent();
        if (audioEvent == null || !audioEvent.HasClip)
            return;

        EnsureSource();
        GameAudioSettings.BindSfxOutput(_sharedSource);
        PlayerSfxUtility.PlayOneShot(_sharedSource, audioEvent);
    }

    /// <summary>Reviver.wav — ao concluir revive de aliado ou conserto da carruagem.</summary>
    public static void PlayReviveComplete()
    {
        AudioClip clip = ResolveReviveCompleteClip();
        if (clip == null)
            return;

        EnsureSource();
        GameAudioSettings.BindSfxOutput(_sharedSource);
        _sharedSource.PlayOneShot(clip, ReviveCompleteVolume);
    }

    private static AudioClip ResolveReviveCompleteClip()
    {
        DownedPlayerConfig downedConfig = DownedPlayerConfigUtility.Resolve();
        if (downedConfig != null && downedConfig.reviveCompleteClip != null)
            return downedConfig.reviveCompleteClip;

        UIAudioConfigSO uiConfig = Resources.Load<UIAudioConfigSO>("UIAudioConfig");
        if (uiConfig != null && uiConfig.reviveComplete != null &&
            uiConfig.reviveComplete.TryResolvePlayback(out AudioClip clip, out _, out _))
            return clip;

        return null;
    }

    private static AudioEventSO ResolveEvent()
    {
        if (_cachedEvent != null)
            return _cachedEvent;

        UIAudioConfigSO uiConfig = Resources.Load<UIAudioConfigSO>("UIAudioConfig");
        if (uiConfig != null && uiConfig.interactE != null)
        {
            _cachedEvent = uiConfig.interactE;
            return _cachedEvent;
        }

        _cachedEvent = Resources.Load<AudioEventSO>(InteractEventResource);
        return _cachedEvent;
    }

    private static void EnsureSource()
    {
        if (_sharedSource != null)
            return;

        GameAudioSettings.EnsureExists();
        var go = new GameObject("GameplayInteractSfx");
        Object.DontDestroyOnLoad(go);
        _sharedSource = go.AddComponent<AudioSource>();
        _sharedSource.playOnAwake = false;
        _sharedSource.spatialBlend = 0f;
        _sharedSource.loop = false;
        GameAudioSettings.BindSfxOutput(_sharedSource);
    }
}
