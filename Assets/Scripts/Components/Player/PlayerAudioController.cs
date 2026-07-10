///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do jogador.
// Ouve eventos de movimento e combate para emitir feedback sonoro via mixer SFX.
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerAudioConfigSO audioConfig;

    [Header("Mixer (legado — BindSfxOutput usa GameAudioSettings)")]
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

    [Header("Legado (Cora / fallback sem PlayerAudioConfigSO)")]
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
    private PlayerMeleeCombat _meleeCombat;
    private PlayerAbilityHandler _abilityHandler;
    private HealthComponent _healthComponent;
    private NetworkPlayerHealth _networkHealth;
    private float _stepTimer;

    public void ApplyConfig(PlayerAudioConfigSO config) => audioConfig = config;

    private void Awake()
    {
        if (playerShooting == null)
            playerShooting = GetComponent<PlayerShooting>();
        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        _rb = GetComponent<Rigidbody2D>();
        _meleeCombat = GetComponent<PlayerMeleeCombat>();
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        _healthComponent = GetComponent<HealthComponent>();
        _networkHealth = GetComponent<NetworkPlayerHealth>();

        ConfigureSources();
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

        if (_meleeCombat != null)
            _meleeCombat.OnMeleeAttackStarted += HandleMeleeAttackStarted;

        if (_abilityHandler != null)
            _abilityHandler.OnAbilityActivated += HandleAbilityActivated;

        if (_healthComponent != null)
            _healthComponent.OnTakeDamage.AddListener(HandleTakeDamage);
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

        if (_meleeCombat != null)
            _meleeCombat.OnMeleeAttackStarted -= HandleMeleeAttackStarted;

        if (_abilityHandler != null)
            _abilityHandler.OnAbilityActivated -= HandleAbilityActivated;

        if (_healthComponent != null)
            _healthComponent.OnTakeDamage.RemoveListener(HandleTakeDamage);

        _stepTimer = 0f;
    }

    private void Update() => UpdateFootsteps();

    public void PlayAttackSfx() => PlayConfiguredEvent(audioConfig != null ? audioConfig.attack : null);

    public void PlayDamageSfx() => PlayConfiguredEvent(audioConfig != null ? audioConfig.damage : null);

    public void PlayAbilitySfx(CharacterAbilityType abilityType)
    {
        if (audioConfig == null)
            return;

        AudioEventSO audioEvent = abilityType switch
        {
            CharacterAbilityType.NixPush => audioConfig.abilityQ,
            CharacterAbilityType.NixCharge => audioConfig.abilityR,
            CharacterAbilityType.CoraBarrier => audioConfig.abilityQ,
            CharacterAbilityType.CoraPool => audioConfig.abilityR,
            _ => null
        };

        PlayConfiguredEvent(audioEvent);
    }

    private void ConfigureSources()
    {
        GameAudioSettings.EnsureExists();

        if (loopSource != null)
        {
            loopSource.loop = false;
            loopSource.playOnAwake = false;
            BindSourceOutput(loopSource);
        }

        if (sfxSource != null)
            BindSourceOutput(sfxSource);

        if (dashSource != null)
            BindSourceOutput(dashSource);
    }

    private void BindSourceOutput(AudioSource source)
    {
        if (!GameAudioSettings.BindSfxOutput(source) && sfxMixerGroup != null)
            source.outputAudioMixerGroup = sfxMixerGroup;
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
        if (audioConfig != null && audioConfig.attack != null)
        {
            PlayAttackSfx();
            return;
        }

        if (sfxSource == null || shootClip == null)
            return;

        sfxSource.PlayOneShot(shootClip);
    }

    private void HandleMeleeAttackStarted() => PlayAttackSfx();

    private void HandleAbilityActivated(CharacterAbilityType abilityType)
    {
        if (abilityType == CharacterAbilityType.Dash)
            return;

        PlayAbilitySfx(abilityType);
    }

    private void HandleDashStarted()
    {
        if (audioConfig != null && audioConfig.dash != null)
        {
            PlayConfiguredEvent(audioConfig.dash, dashSource);
            return;
        }

        if (dashSource != null && dashClip != null)
            dashSource.PlayOneShot(dashClip);
    }

    private void HandleDashEnded()
    {
        if (dashSource != null && dashSource.isPlaying)
            dashSource.Stop();
    }

    private void HandleTakeDamage()
    {
        if (UsesNetworkDamageRelay())
            return;

        PlayDamageSfx();
    }

    private bool UsesNetworkDamageRelay() =>
        _networkHealth != null && _networkHealth.IsSpawned;

    private void PlayConfiguredEvent(AudioEventSO audioEvent, AudioSource sourceOverride = null)
    {
        AudioSource source = sourceOverride != null ? sourceOverride : sfxSource;
        PlayerSfxUtility.PlayOneShot(source, audioEvent);
    }
}
