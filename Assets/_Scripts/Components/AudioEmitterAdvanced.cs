// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-09 19:00
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class AudioEmitterAdvanced : MonoBehaviour
{
    private AudioSource source;

    [Header("Configurações")]
    public bool playOnStart = false;
    public bool destroyAfterPlay = false;

    [Header("Randomização")]
    public float volumeMin = 0.9f;
    public float volumeMax = 1.0f;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Start()
    {
        if (playOnStart && source.clip != null)
            Play(source.clip);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioEmitterAdvanced: Clip nulo fornecido para reprodução.");
            return;
        }

        source.volume = Random.Range(volumeMin, volumeMax);
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.PlayOneShot(clip);

        if (destroyAfterPlay)
            Destroy(gameObject, clip.length / source.pitch);
    }
    public void Stop()
    {
        source.Stop();
    }
}
