using UnityEngine;
using UnityEngine.Audio;

/// Volumes persistidos (PlayerPrefs) aplicados ao mixer único do projeto:
/// <c>Assets/Resources/MidnightMeowAudioMixer.mixer</c> (grupos Master / Music / SFX).
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class GameAudioSettings : Singleton<GameAudioSettings>
{
    public const string MixerResourceName = "MidnightMeowAudioMixer";
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

    /// Roteia um <see cref="AudioSource"/> para o grupo SFX do mixer do projeto.
    public static bool BindSfxOutput(AudioSource source)
    {
        if (source == null)
            return false;

        EnsureExists();
        GameAudioSettings settings = Instance;
        if (settings == null)
            return false;

        if (settings.SfxGroup == null)
            settings.ResolveMixerReferences();

        if (settings.SfxGroup == null)
            return false;

        source.outputAudioMixerGroup = settings.SfxGroup;
        return true;
    }

    /// Roteia um <see cref="AudioSource"/> para o grupo Music.
    public static bool BindMusicOutput(AudioSource source)
    {
        if (source == null)
            return false;

        EnsureExists();
        GameAudioSettings settings = Instance;
        if (settings == null)
            return false;

        if (settings.MusicGroup == null)
            settings.ResolveMixerReferences();

        if (settings.MusicGroup == null)
            return false;

        source.outputAudioMixerGroup = settings.MusicGroup;
        return true;
    }

    public static void EnsureExists()
    {
        if (Instance != null)
        {
            if (Instance.SfxGroup == null)
                Instance.ResolveMixerReferences();
            return;
        }

        GameAudioSettings existing = FindFirstObjectByType<GameAudioSettings>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (existing.SfxGroup == null)
                existing.ResolveMixerReferences();
            return;
        }

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
            ResolveMixerReferences();

        if (audioMixer == null)
            return;

        if (!audioMixer.SetFloat(mixerParam, LinearToDecibels(linear)))
            Debug.LogWarning($"[GameAudioSettings] Parâmetro '{mixerParam}' não encontrado em {MixerResourceName}.");
    }

    private void ResolveMixerReferences()
    {
        if (audioMixer == null)
            audioMixer = FindProjectMixer();

        if (audioMixer == null)
        {
            Debug.LogWarning($"[GameAudioSettings] {MixerResourceName} não encontrado em Resources.");
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
        AudioMixer mixer = Resources.Load<AudioMixer>(MixerResourceName);
        if (mixer != null)
            return mixer;

        // Fallback só se o asset ainda estiver com nome antigo em alguma build residual.
        return Resources.Load<AudioMixer>("NewAudioMixer");
    }
}
