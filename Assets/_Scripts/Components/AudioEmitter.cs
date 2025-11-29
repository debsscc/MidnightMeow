// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-09 19:05
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEmitter : MonoBehaviour
{
    [Header("Debug Autoplay")]
    public bool playOnStart = false;

    private AudioSource audioSource;
    public  bool destroyAfterPlay = false;

    // --------------------------
    //  Lifecycle
    // --------------------------

    void Awake()
    {
        // Garante que o AudioSource está no GameObject
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (playOnStart)
            PlayAudio();
    }

    void Update()
    {
        if (destroyAfterPlay && !audioSource.isPlaying)
            Destroy(gameObject);
    }

    private void PlayAudio()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }

    // --------------------------
    // Public Controls
    // --------------------------

    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopSound()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}

// Para tocar em outro script de colisão:
// GetComponent<AudioEmitter>().PlaySound(meuAudioClip);