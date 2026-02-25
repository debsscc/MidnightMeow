///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do jogador. 
// Ouve eventos de movimento e combate para emitir feedback sonoro.
// ---------------------------------------------------------------- */

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Referência ao componente de movimento para escutar OnMovement e OnStop")]
    [SerializeField] private PlayerMovement playerMovement;
    
    [Tooltip("Referência ao componente de tiro para escutar OnShoot")]
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private PlayerDash playerDash;

    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicado a sons contínuos (em loop)")]
    [SerializeField] private AudioSource loopSource;
    
    [Tooltip("AudioSource dedicado a sons instantâneos (one-shot)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource dashSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip dashClip;

    private void Awake()
    {
        // Fallback de segurança caso os componentes estejam no mesmo GameObject e não tenham sido arrastados
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();

        // Configura a fonte de loop automaticamente para evitar erros de design
        if (loopSource != null)
        {
            loopSource.loop = true;
            loopSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovement += HandleMovementStarted;
            playerMovement.OnStop += HandleMovementStopped;
        }

        if (playerShooting != null)
        {
            playerShooting.OnShoot += HandleShoot;
        }

        if (playerDash != null)
        {
            playerDash.OnDashStarted += HandleDashStarted;
            playerDash.OnDashEnded += HandleDashEnded;
        }
        
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovement -= HandleMovementStarted;
            playerMovement.OnStop -= HandleMovementStopped;
        }

        if (playerShooting != null)
        {
            playerShooting.OnShoot -= HandleShoot;
        }
        if (playerDash != null)
        {
            playerDash.OnDashStarted -= HandleDashStarted;
            playerDash.OnDashEnded -= HandleDashEnded;
        }
    }

    private void HandleMovementStarted()
    {
        if (loopSource == null || movementClip == null) return;

        // Evita reiniciar o áudio se ele já estiver tocando (evita som engasgado)
        if (!loopSource.isPlaying || loopSource.clip != movementClip)
        {
            loopSource.clip = movementClip;
            loopSource.Play();
        }
    }

    private void HandleMovementStopped()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    private void HandleShoot()
    {
        if (sfxSource != null && shootClip != null)
        {
            sfxSource.PlayOneShot(shootClip);
        }
    }

    private void HandleDashStarted()
    {
        if (dashSource != null && dashClip != null)
        {
            dashSource.PlayOneShot(dashClip);
        }
    }

    private void HandleDashEnded()
    {
        if (dashSource != null && dashSource.isPlaying)
        {
            dashSource.Stop();
        }
    }
}