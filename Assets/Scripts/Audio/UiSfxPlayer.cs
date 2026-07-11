// ----------------------------------------------------------------
// DESCRIÇÃO: Bus global de SFX de UI (hover / click). Um único caminho de áudio.
// ----------------------------------------------------------------

using UnityEngine;

public sealed class UiSfxPlayer : MonoBehaviour
{
    private const string ConfigResource = "UIAudioConfig";
    private const float HoverCooldownSeconds = 0.06f;

    public static UiSfxPlayer Instance { get; private set; }

    private AudioSource _source;
    private AudioEventSO _hoverEvent;
    private AudioEventSO _clickEvent;
    private float _nextHoverAllowedUnscaledTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap() => EnsureExists();

    public static UiSfxPlayer EnsureExists()
    {
        if (Instance != null)
            return Instance;

        UiSfxPlayer existing = FindFirstObjectByType<UiSfxPlayer>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameAudioSettings.EnsureExists();
        var go = new GameObject(nameof(UiSfxPlayer));
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<UiSfxPlayer>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSource();
        ResolveEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayHover()
    {
        if (Time.unscaledTime < _nextHoverAllowedUnscaledTime)
            return;

        _nextHoverAllowedUnscaledTime = Time.unscaledTime + HoverCooldownSeconds;
        Play(_hoverEvent);
    }

    public void PlayClick()
    {
        Play(_clickEvent);
    }

    private void Play(AudioEventSO audioEvent)
    {
        EnsureSource();
        ResolveEvents();
        if (_source == null || audioEvent == null || !audioEvent.HasClip)
            return;

        GameAudioSettings.BindSfxOutput(_source);
        PlayerSfxUtility.PlayOneShot(_source, audioEvent);
    }

    private void EnsureSource()
    {
        if (_source != null)
            return;

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
        GameAudioSettings.BindSfxOutput(_source);
    }

    private void ResolveEvents()
    {
        if (_hoverEvent != null && _clickEvent != null)
            return;

        UIAudioConfigSO config = Resources.Load<UIAudioConfigSO>(ConfigResource);
        if (config == null)
            return;

        if (_hoverEvent == null)
            _hoverEvent = config.buttonHover;
        if (_clickEvent == null)
            _clickEvent = config.buttonClick;
    }
}
