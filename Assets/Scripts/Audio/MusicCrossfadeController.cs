using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Trilha persistente com crossfade, sincronizada com o fade visual do <see cref="ScreenFlowController"/>.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-195)]
public class MusicCrossfadeController : Singleton<MusicCrossfadeController>
{
    [SerializeField] private float defaultFadeSeconds = 1f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _activeSource;
    private Coroutine _fadeRoutine;
    private AudioClip _currentClip;
    private AudioClip _pendingClip;
    private bool _pendingLoop = true;
    private bool _externalOverride;

    public static void EnsureExists()
    {
        GameAudioSettings.EnsureExists();

        if (Instance != null)
            return;

        MusicCrossfadeController existing =
            FindFirstObjectByType<MusicCrossfadeController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf)
                existing.gameObject.SetActive(true);

            // Awake do Singleton define Instance; se o objeto já estava ativo sem Awake, força.
            if (Instance == null)
                Instance = existing;
            return;
        }

        var go = new GameObject(nameof(MusicCrossfadeController));
        go.AddComponent<MusicCrossfadeController>();
    }

    protected override void Awake()
    {
        base.Awake();
        ResolveMusicMixerGroup();
        _sourceA = CreateMusicSource("MusicA");
        _sourceB = CreateMusicSource("MusicB");
        _activeSource = null;
    }

    private void ResolveMusicMixerGroup()
    {
        GameAudioSettings.EnsureExists();
        GameAudioSettings settings = GameAudioSettings.Instance;
        if (settings != null && settings.MusicGroup != null)
            musicMixerGroup = settings.MusicGroup;
    }

    private AudioSource CreateMusicSource(string childName)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        AssignMusicOutput(source);
        return source;
    }

    private void AssignMusicOutput(AudioSource source)
    {
        if (source == null)
            return;

        if (!GameAudioSettings.BindMusicOutput(source))
        {
            ResolveMusicMixerGroup();
            if (musicMixerGroup != null)
                source.outputAudioMixerGroup = musicMixerGroup;
        }
        else
        {
            musicMixerGroup = source.outputAudioMixerGroup;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ScreenFlowController.Instance != null && ScreenFlowController.Instance.IsTransitioning)
            return;

        PrepareSceneMusic(scene);
        FadeInPending(defaultFadeSeconds);
    }

    public void PrepareSceneMusic(Scene scene)
    {
        // Lê o clip ANTES de silenciar o AudioSource da cena (Unity 6 / resource).
        bool resolved = SceneMusicResolver.TryResolve(scene, out AudioClip clip, out bool _);
        SceneMusicResolver.SuppressSceneMusicSources(scene);

        if (_externalOverride)
            return;

        if (!resolved)
        {
            _pendingClip = null;
            if (SceneMusicResolver.IsSilentHubScene(scene.name))
                HandleTransitionFadeOut(defaultFadeSeconds);
            return;
        }

        _pendingClip = clip;
        // Trilha de cena (menu/lobby/fases) sempre em loop — não confiar só no flag do AudioSource da cena.
        _pendingLoop = true;
    }

    public void HandleTransitionFadeOut(float duration)
    {
        if (_externalOverride)
            return;

        float fade = ResolveDuration(duration);
        if (_activeSource == null || !_activeSource.isPlaying)
            return;

        StartVolumeFade(_activeSource, _activeSource.volume, 0f, fade, stopAtEnd: true);
    }

    public void FadeInPending(float duration)
    {
        if (_externalOverride)
            return;

        float fade = ResolveDuration(duration);

        if (_pendingClip == null)
            return;

        if (_currentClip == _pendingClip && _activeSource != null && _activeSource.isPlaying)
        {
            _pendingClip = null;
            return;
        }

        CrossfadeTo(_pendingClip, _pendingLoop, fade);
        _pendingClip = null;
    }

    /// <summary>
    /// Trilha forçada (ex.: créditos). Bloqueia Prepare/FadeIn da cena até <see cref="EndExternalOverride"/>.
    /// </summary>
    public void BeginExternalOverride(AudioClip clip, bool loop, float duration)
    {
        _externalOverride = true;
        _pendingClip = null;
        SceneMusicResolver.SuppressAllLoadedScenes();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        // Para imediatamente a trilha atual (ex.: vitória) antes de entrar nos créditos.
        HardStopSource(_sourceA);
        HardStopSource(_sourceB);
        _activeSource = null;
        _currentClip = null;

        if (clip == null)
            return;

        CrossfadeTo(clip, loop, duration);
    }

    public void EndExternalOverride()
    {
        _externalOverride = false;
    }

    private static void HardStopSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    public void CrossfadeTo(AudioClip clip, bool loop, float duration)
    {
        if (clip == null)
            return;

        if (_currentClip == clip && _activeSource != null && _activeSource.isPlaying)
            return;

        float fade = ResolveDuration(duration);
        AudioSource incoming = _activeSource == _sourceA ? _sourceB : _sourceA;
        AudioSource outgoing = _activeSource;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        incoming.clip = clip;
        incoming.loop = loop;
        incoming.playOnAwake = false;
        incoming.volume = 0f;
        AssignMusicOutput(incoming);
        if (!incoming.isPlaying)
            incoming.Play();

        // Garante loop mesmo se algum Suppress/HardStop tiver alterado o AudioSource.
        if (loop && !incoming.loop)
            incoming.loop = true;

        _fadeRoutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming, fade));
        _activeSource = incoming;
        _currentClip = clip;
    }

    private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, float duration)
    {
        float outgoingStart = outgoing != null && outgoing.isPlaying ? outgoing.volume : 0f;
        float incomingTarget = musicVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            if (incoming != null)
                incoming.volume = Mathf.Lerp(0f, incomingTarget, t);

            if (outgoing != null && outgoing.isPlaying)
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, t);

            yield return null;
        }

        if (incoming != null)
            incoming.volume = incomingTarget;

        if (outgoing != null)
        {
            outgoing.volume = 0f;
            outgoing.Stop();
        }

        _fadeRoutine = null;
    }

    private void StartVolumeFade(AudioSource source, float from, float to, float duration, bool stopAtEnd)
    {
        if (source == null)
            return;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(VolumeFadeRoutine(source, from, to, duration, stopAtEnd));
    }

    private IEnumerator VolumeFadeRoutine(AudioSource source, float from, float to, float duration, bool stopAtEnd)
    {
        float elapsed = 0f;
        source.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        source.volume = to;

        if (stopAtEnd && to <= 0.001f)
            source.Stop();

        _fadeRoutine = null;
    }

    private float ResolveDuration(float duration) =>
        duration > 0f ? duration : defaultFadeSeconds;
}
