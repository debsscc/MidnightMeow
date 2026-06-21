///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do jogador. 
// Ouve eventos de movimento e combate para emitir feedback sonoro.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Tooltip("Referência ao componente de tiro para escutar OnShoot")]
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private PlayerDash playerDash;

    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicado a passos (one-shot)")]
    [SerializeField] private AudioSource loopSource;

    [Tooltip("AudioSource dedicado a sons instantâneos (one-shot)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource dashSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip dashClip;

    [Header("Footsteps")]
    [SerializeField] private float footstepMinSpeed = 0.35f;
    [SerializeField] private float footstepBaseInterval = 0.38f;
    [SerializeField] private float footstepReferenceSpeed = 5f;
    [SerializeField] private float footstepMinInterval = 0.22f;
    [SerializeField] private float footstepMaxInterval = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.85f;

    private Rigidbody2D _rb;
    private float _stepTimer;

    private void Awake()
    {
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        _rb = GetComponent<Rigidbody2D>();

        if (loopSource != null)
        {
            loopSource.loop = false;
            loopSource.playOnAwake = false;
            loopSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        if (sfxSource != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

        if (dashSource != null)
            dashSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnEnable()
    {
        if (playerShooting != null)
            playerShooting.OnProjectileInstantiated += HandleProjectileInstantiated;

        if (playerDash != null)
        {
            playerDash.OnDashStarted += HandleDashStarted;
            playerDash.OnDashEnded += HandleDashEnded;
        }
    }

    private void OnDisable()
    {
        if (playerShooting != null)
            playerShooting.OnProjectileInstantiated -= HandleProjectileInstantiated;

        if (playerDash != null)
        {
            playerDash.OnDashStarted -= HandleDashStarted;
            playerDash.OnDashEnded -= HandleDashEnded;
        }

        _stepTimer = 0f;
    }

    private void Update()
    {
        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (loopSource == null || movementClip == null || _rb == null)
            return;

        if (playerDash != null && playerDash.IsDashing)
        {
            _stepTimer = 0f;
            return;
        }

        float speed = _rb.linearVelocity.magnitude;
        if (speed < footstepMinSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        float interval = footstepBaseInterval * (footstepReferenceSpeed / speed);
        interval = Mathf.Clamp(interval, footstepMinInterval, footstepMaxInterval);

        _stepTimer += Time.deltaTime;
        if (_stepTimer < interval)
            return;

        _stepTimer = 0f;
        loopSource.PlayOneShot(movementClip, footstepVolume);
    }

    private void HandleProjectileInstantiated(GameObject _, Vector3 __, Quaternion ___, Vector2 ____)
    {
        if (sfxSource != null && shootClip != null)
            sfxSource.PlayOneShot(shootClip);
    }

    private void HandleDashStarted()
    {
        if (dashSource != null && dashClip != null)
            dashSource.PlayOneShot(dashClip);
    }

    private void HandleDashEnded()
    {
        if (dashSource != null && dashSource.isPlaying)
            dashSource.Stop();
    }
}
