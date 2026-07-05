using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Volumes persistidos (PlayerPrefs) aplicados ao <see cref="AudioMixer"/> do projeto.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class GameAudioSettings : Singleton<GameAudioSettings>
{
    public const string PrefMasterVolume = "midnightmeow.audio.master";
    public const string PrefMusicVolume = "midnightmeow.audio.music";
    public const string PrefSfxVolume = "midnightmeow.audio.sfx";
    public const float DefaultLinearVolume = 0.75f;

    public const string MasterVolumeParam = "MasterVolume";
    public const string MusicVolumeParam = "MusicVolume";
    public const string SfxVolumeParam = "SfxVolume";

    [SerializeField] private AudioMixer audioMixer;

    private AudioMixerGroup _musicGroup;
    private AudioMixerGroup _sfxGroup;

    public AudioMixer Mixer => audioMixer;
    public AudioMixerGroup MusicGroup => _musicGroup;
    public AudioMixerGroup SfxGroup => _sfxGroup;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameAudioSettings existing = FindFirstObjectByType<GameAudioSettings>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var go = new GameObject(nameof(GameAudioSettings));
        go.AddComponent<GameAudioSettings>();
    }

    protected override void Awake()
    {
        base.Awake();
        ResolveMixerReferences();
        ApplySavedVolumes();
    }

    public void ApplySavedVolumes()
    {
        SetMasterVolume(GetSavedLinear(PrefMasterVolume), persist: false);
        SetMusicVolume(GetSavedLinear(PrefMusicVolume), persist: false);
        SetSfxVolume(GetSavedLinear(PrefSfxVolume), persist: false);
    }

    public float GetMasterVolume() => GetSavedLinear(PrefMasterVolume);

    public float GetMusicVolume() => GetSavedLinear(PrefMusicVolume);

    public float GetSfxVolume() => GetSavedLinear(PrefSfxVolume);

    public void SetMasterVolume(float linear, bool persist = true) =>
        SetVolume(PrefMasterVolume, MasterVolumeParam, linear, persist);

    public void SetMusicVolume(float linear, bool persist = true) =>
        SetVolume(PrefMusicVolume, MusicVolumeParam, linear, persist);

    public void SetSfxVolume(float linear, bool persist = true) =>
        SetVolume(PrefSfxVolume, SfxVolumeParam, linear, persist);

    public void ResetToDefaults()
    {
        SetMasterVolume(DefaultLinearVolume);
        SetMusicVolume(DefaultLinearVolume);
        SetSfxVolume(DefaultLinearVolume);
    }

    public static float LinearToDecibels(float linear)
    {
        if (linear <= 0.0001f)
            return -80f;

        return Mathf.Log10(linear) * 20f;
    }

    public static float GetSavedLinear(string prefKey) =>
        PlayerPrefs.GetFloat(prefKey, DefaultLinearVolume);

    private void SetVolume(string prefKey, string mixerParam, float linear, bool persist)
    {
        linear = Mathf.Clamp01(linear);

        if (persist)
        {
            PlayerPrefs.SetFloat(prefKey, linear);
            PlayerPrefs.Save();
        }

        if (audioMixer == null)
            return;

        if (!audioMixer.SetFloat(mixerParam, LinearToDecibels(linear)))
            Debug.LogWarning($"[GameAudioSettings] Parâmetro '{mixerParam}' não encontrado no mixer.");
    }

    private void ResolveMixerReferences()
    {
        if (audioMixer == null)
            audioMixer = FindProjectMixer();

        if (audioMixer == null)
        {
            Debug.LogWarning("[GameAudioSettings] NewAudioMixer não encontrado.");
            return;
        }

        _musicGroup = FindGroup("Music");
        _sfxGroup = FindGroup("SFX");
    }

    private AudioMixerGroup FindGroup(string groupName)
    {
        if (audioMixer == null)
            return null;

        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(groupName);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    private static AudioMixer FindProjectMixer()
    {
        AudioMixer fromResources = Resources.Load<AudioMixer>("NewAudioMixer");
        if (fromResources != null)
            return fromResources;

        AudioMixer[] mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        for (int i = 0; i < mixers.Length; i++)
        {
            AudioMixer mixer = mixers[i];
            if (mixer != null && mixer.name == "NewAudioMixer")
                return mixer;
        }

        return null;
    }
}
