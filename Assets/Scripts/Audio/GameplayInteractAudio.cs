// ----------------------------------------------------------------
// DESCRIÇÃO: SFX de confirmação ao pressionar Interact (E / gamepad) em ações de gameplay.
// ----------------------------------------------------------------

using UnityEngine;

public static class GameplayInteractAudio
{
    private const string InteractEventResource = "InteractEAudioEvent";

    private static AudioEventSO _cachedEvent;
    private static AudioSource _sharedSource;

    public static void PlayConfirm()
    {
        AudioEventSO audioEvent = ResolveEvent();
        if (audioEvent == null || !audioEvent.HasClip)
            return;

        EnsureSource();
        GameAudioSettings.BindSfxOutput(_sharedSource);
        PlayerSfxUtility.PlayOneShot(_sharedSource, audioEvent);
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
