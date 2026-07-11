///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do jogador.
// Ouve eventos de movimento e combate para emitir feedback sonoro via mixer SFX.
// Inclui batida cardíaca local quando a vida cai abaixo do limiar (solo e MP).
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    private const float DefaultHeartbeatHealthThreshold = 0.5f;
    private const float CriticalHeartbeatHealthRatio = 0.18f;

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

    [Header("Low health heartbeat")]
    [Tooltip("Toca heartbeat do PlayerAudioConfigSO quando current/max fica abaixo deste valor.")]
    [SerializeField] [Range(0.05f, 1f)] private float heartbeatHealthThreshold = DefaultHeartbeatHealthThreshold;

    [Tooltip("Ganho extra do heartbeat (AudioSource.volume maxa em 1; isto amplifica o sinal).")]
    [SerializeField] [Range(1f, 5f)] private float heartbeatVolumeGain = 3f;

    private Rigidbody2D _rb;
    private PlayerMeleeCombat _meleeCombat;
    private PlayerAbilityHandler _abilityHandler;
    private HealthComponent _healthComponent;
    private NetworkPlayerHealth _networkHealth;
    private float _stepTimer;
    private float _healthRatio = 1f;
    private AudioSource _heartbeatSource;
    private HeartbeatAudioGain _heartbeatGain;
    private bool _heartbeatLoopActive;
    private AudioClip _heartbeatClip;
    private float _heartbeatBaseVolume = 1f;
    private float _heartbeatBasePitch = 1f;

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
        SyncHealthRatio();
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
        {
            _healthComponent.OnTakeDamage.AddListener(HandleTakeDamage);
            _healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
        }

        NetworkPlayerHealth.OnNetworkHealthChanged += HandleNetworkHealthChanged;

        SyncHealthRatio();
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
        {
            _healthComponent.OnTakeDamage.RemoveListener(HandleTakeDamage);
            _healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }

        NetworkPlayerHealth.OnNetworkHealthChanged -= HandleNetworkHealthChanged;

        _stepTimer = 0f;
        StopLowHealthHeartbeat();
    }

    private void Update()
    {
        SyncHealthRatio();
        UpdateFootsteps();
        UpdateLowHealthHeartbeat();
    }

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

    private void HandleHealthChanged(float current, float max)
    {
        ApplyHealthRatio(current, max);
    }

    private void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (!IsLocalPlayerClientId(clientId))
            return;

        ApplyHealthRatio(current, max);
    }

    private void SyncHealthRatio()
    {
        // Mesma fonte da barra de vida do HUD: NetworkPlayerHealth quando spawnado.
        if (_networkHealth != null && _networkHealth.IsSpawned)
        {
            ApplyHealthRatio(_networkHealth.CurrentHealth, _networkHealth.MaxHealth);
            return;
        }

        if (_healthComponent == null || _healthComponent.MaxHealth <= 0f)
        {
            _healthRatio = 1f;
            return;
        }

        ApplyHealthRatio(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
    }

    private void ApplyHealthRatio(float current, float max)
    {
        _healthRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void UpdateLowHealthHeartbeat()
    {
        if (!ShouldPlayLowHealthHeartbeat())
        {
            StopLowHealthHeartbeat();
            return;
        }

        EnsureHeartbeatSource();
        if (_heartbeatSource == null)
            return;

        if (!_heartbeatLoopActive || _heartbeatClip == null)
        {
            if (!audioConfig.heartbeat.TryResolvePlayback(out AudioClip clip, out float volume, out float pitch))
            {
                StopLowHealthHeartbeat();
                return;
            }

            _heartbeatClip = clip;
            _heartbeatBaseVolume = volume;
            _heartbeatBasePitch = pitch;
        }

        float urgency = ComputeHeartbeatUrgency(_healthRatio);
        BindSourceOutput(_heartbeatSource);
        _heartbeatSource.loop = true;
        _heartbeatSource.playOnAwake = false;
        _heartbeatSource.spatialBlend = 0f;
        _heartbeatSource.mute = false;
        // Volume do source fica no teto; o ganho real vem do HeartbeatAudioGain.
        _heartbeatSource.volume = 1f;
        _heartbeatSource.pitch = _heartbeatBasePitch;
        if (_heartbeatGain != null)
            _heartbeatGain.Gain = heartbeatVolumeGain * _heartbeatBaseVolume * Mathf.Lerp(0.9f, 1f, urgency);

        if (_heartbeatSource.clip != _heartbeatClip)
            _heartbeatSource.clip = _heartbeatClip;

        if (!_heartbeatSource.isPlaying)
            _heartbeatSource.Play();

        _heartbeatLoopActive = true;
    }

    private void StopLowHealthHeartbeat()
    {
        if (_heartbeatSource != null && _heartbeatSource.isPlaying)
            _heartbeatSource.Stop();

        _heartbeatLoopActive = false;
        _heartbeatClip = null;
    }

    private void EnsureHeartbeatSource()
    {
        if (_heartbeatSource != null)
            return;

        var go = new GameObject("HeartbeatAudio");
        go.transform.SetParent(transform, false);
        _heartbeatSource = go.AddComponent<AudioSource>();
        _heartbeatSource.playOnAwake = false;
        _heartbeatSource.loop = true;
        _heartbeatSource.spatialBlend = 0f;
        _heartbeatSource.bypassListenerEffects = false;
        _heartbeatGain = go.AddComponent<HeartbeatAudioGain>();
        _heartbeatGain.Gain = heartbeatVolumeGain;
        BindSourceOutput(_heartbeatSource);
    }

    private bool ShouldPlayLowHealthHeartbeat()
    {
        if (audioConfig == null || audioConfig.heartbeat == null || !audioConfig.heartbeat.HasClip)
            return false;

        if (!IsLocalPlayerForAudio())
            return false;

        if (_networkHealth != null && _networkHealth.IsSpawned && _networkHealth.IsUnconscious)
            return false;

        if (_healthComponent != null && _healthComponent.IsDead)
            return false;

        // <= 50%: inclui exatamente metade da vida.
        return _healthRatio > 0f && _healthRatio <= heartbeatHealthThreshold;
    }

    private static bool IsLocalPlayerClientId(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null && clientId == networkManager.LocalClientId;
    }

    private bool IsLocalPlayerForAudio()
    {
        if (_networkHealth != null && _networkHealth.IsSpawned)
            return _networkHealth.IsOwner;

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
            return networkObject.IsOwner;

        return true;
    }

    private float ComputeHeartbeatUrgency(float healthRatio)
    {
        float threshold = Mathf.Max(0.05f, heartbeatHealthThreshold);
        float critical = Mathf.Clamp(CriticalHeartbeatHealthRatio, 0.01f, threshold - 0.01f);
        if (healthRatio <= critical)
            return 1f;

        return 1f - Mathf.InverseLerp(critical, threshold, healthRatio);
    }

    private bool UsesNetworkDamageRelay() =>
        _networkHealth != null && _networkHealth.IsSpawned;

    private void PlayConfiguredEvent(AudioEventSO audioEvent, AudioSource sourceOverride = null)
    {
        AudioSource source = sourceOverride != null ? sourceOverride : sfxSource;
        PlayerSfxUtility.PlayOneShot(source, audioEvent);
    }

    /// <summary>
    /// Amplifica o heartbeat além do teto 0–1 do AudioSource.volume.
    /// </summary>
    private sealed class HeartbeatAudioGain : MonoBehaviour
    {
        public float Gain = 1f;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float gain = Gain;
            if (gain <= 1.0001f)
                return;

            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }
    }
}
