using UnityEngine;

/// <summary>
/// Evento de áudio configurável pelo Game Designer (clip aleatório, volume e variação de pitch).
/// </summary>
[CreateAssetMenu(fileName = "AudioEvent", menuName = "MidnightMeow/Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    [SerializeField] private AudioClip[] clips = System.Array.Empty<AudioClip>();

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Range(0.1f, 2f)]
    [SerializeField] private float pitchMin = 1f;

    [Range(0.1f, 2f)]
    [SerializeField] private float pitchMax = 1f;

    public bool HasClip => clips != null && clips.Length > 0;

    public bool TryResolvePlayback(out AudioClip clip, out float resolvedVolume, out float pitch)
    {
        clip = null;
        resolvedVolume = volume;
        pitch = 1f;

        if (clips == null || clips.Length == 0)
            return false;

        clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return false;

        float minPitch = Mathf.Min(pitchMin, pitchMax);
        float maxPitch = Mathf.Max(pitchMin, pitchMax);
        pitch = Random.Range(minPitch, maxPitch);
        return true;
    }
}
