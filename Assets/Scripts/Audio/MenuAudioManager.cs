///* ----------------------------------------------------------------
// DESCRIÇÃO: Legado — hover/click de UI migraram para UiSfxPlayer + UiButtonSfx.
// Métodos mantidos como no-op para UnityEvents antigos em cenas/prefabs.
// ---------------------------------------------------------------- */

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MenuAudioManager : MonoBehaviour
{
#pragma warning disable CS0414 // Serializados só para compatibilidade com YAML de cenas antigas.
    [Header("Audio Clips - UI (legado, ignorados)")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
#pragma warning restore CS0414

    private void Awake()
    {
        UiSfxPlayer.EnsureExists();
    }

    /// <summary>Delega para UiSfxPlayer (UnityEvents legados em Menu2/Lobby).</summary>
    public void PlayHoverSound()
    {
        UiSfxPlayer.EnsureExists().PlayHover();
    }

    /// <summary>Delega para UiSfxPlayer (UnityEvents legados em Menu2/Lobby).</summary>
    public void PlayClickSound()
    {
        UiSfxPlayer.EnsureExists().PlayClick();
    }
}
