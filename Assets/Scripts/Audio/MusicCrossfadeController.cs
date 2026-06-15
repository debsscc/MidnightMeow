using System.Collections;
using UnityEngine;
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

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _activeSource;
    private Coroutine _fadeRoutine;
    private AudioClip _currentClip;
    private AudioClip _pendingClip;
    private bool _pendingLoop = true;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        MusicCrossfadeController existing = FindFirstObjectByType<MusicCrossfadeController>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var go = new GameObject(nameof(MusicCrossfadeController));
        go.AddComponent<MusicCrossfadeController>();
    }

    protected override void Awake()
    {
        base.Awake();
        _sourceA = CreateMusicSource("MusicA");
        _sourceB = CreateMusicSource("MusicB");
        _activeSource = null;
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
        return source;
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
        SceneMusicResolver.SuppressSceneMusicSources(scene);

        if (!SceneMusicResolver.TryResolve(scene, out AudioClip clip, out bool loop))
        {
            _pendingClip = null;
            if (SceneMusicResolver.IsSilentHubScene(scene.name))
                HandleTransitionFadeOut(defaultFadeSeconds);
            return;
        }

        _pendingClip = clip;
        _pendingLoop = loop;
    }

    public void HandleTransitionFadeOut(float duration)
    {
        float fade = ResolveDuration(duration);
        if (_activeSource == null || !_activeSource.isPlaying)
            return;

        StartVolumeFade(_activeSource, _activeSource.volume, 0f, fade, stopAtEnd: true);
    }

    public void FadeInPending(float duration)
    {
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
        incoming.volume = 0f;
        if (!incoming.isPlaying)
            incoming.Play();

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
