///* ----------------------------------------------------------------
// DESCRIÇÃO: Gerenciador de áudio local para a cena de Menu.
// Reproduz clipes de som (SFX) solicitados por eventos externos.
// ---------------------------------------------------------------- */

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MenuAudioManager : MonoBehaviour
{
    [Header("Audio Clips - UI")]
    [Tooltip("Som reproduzido ao passar o mouse sobre elementos interativos.")]
    [SerializeField] private AudioClip hoverClip;
    
    [Tooltip("Som reproduzido ao clicar em elementos interativos.")]
    [SerializeField] private AudioClip clickClip;

    [Header("Settings")]
    [Tooltip("Volume global para os efeitos sonoros da UI.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private AudioSource _sfxSource;

    private void Awake()
    {
        _sfxSource = GetComponent<AudioSource>();
        
        // Configurações de segurança para garantir o comportamento correto do AudioSource
        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
        _sfxSource.spatialBlend = 0f; // 2D sound
    }

    /// <summary>
    /// Reproduz o som de Hover. Deve ser chamado via UnityEvent (ex: UIButtonInteractionEvents).
    /// </summary>
    public void PlayHoverSound()
    {
        if (hoverClip != null)
        {
            _sfxSource.PlayOneShot(hoverClip, sfxVolume);
        }
        else
        {
            Debug.LogWarning("MenuAudioManager: Hover Clip não atribuído.");
        }
    }

    /// <summary>
    /// Reproduz o som de Click. Deve ser chamado via UnityEvent.
    /// </summary>
    public void PlayClickSound()
    {
        if (clickClip != null)
        {
            _sfxSource.PlayOneShot(clickClip, sfxVolume);
            Debug.Log("MenuAudioManager: Click sound played.");
        }
        else
        {
            Debug.LogWarning("MenuAudioManager: Click Clip não atribuído.");
        }
    }
}