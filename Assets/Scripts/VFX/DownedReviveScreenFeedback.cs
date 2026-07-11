// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: Pulso de tela + SFX cardíaco + timer durante janela de reviver no multiplayer.
// Ao concluir o revive: SFX Reviver.wav + pulso breve de vinheta (só MP).
// ----------------------------------------------------------------
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(120)]
public class DownedReviveScreenFeedback : MonoBehaviour
{
    private const float ReviveSuccessPulsePeak = 0.18f;
    private const float ReviveSuccessPulseDuration = 0.55f;
    private const float ReviveCompleteSfxVolume = 0.95f;

    private static DownedReviveScreenFeedback _instance;
    private AudioSource _audioSource;
    private DownedReviveTimerHud _timerHud;
    private bool _heartbeatBeatArmed = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Bootstrap do feedback, registra handlers dos eventos de revive/bleedout/scene.
    private static void Bootstrap()
    {
        EnsureExists();
        NetworkPlayerHealth.OnNetworkPlayerDowned -= HandlePlayerDowned;
        NetworkPlayerHealth.OnNetworkPlayerDowned += HandlePlayerDowned;
        NetworkPlayerHealth.OnNetworkPlayerRevived -= HandlePlayerRevived;
        NetworkPlayerHealth.OnNetworkPlayerRevived += HandlePlayerRevived;
        NetworkPlayerHealth.OnNetworkPlayerBleedOut -= HandlePlayerBleedOut;
        NetworkPlayerHealth.OnNetworkPlayerBleedOut += HandlePlayerBleedOut;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Garante existência do singleton do feedback.
    public static void EnsureExists()
    {
        if (_instance != null)
            return;
        var go = new GameObject(nameof(DownedReviveScreenFeedback));
        _instance = go.AddComponent<DownedReviveScreenFeedback>();
        DontDestroyOnLoad(go);
    }

    // Inicializa audio source e singleton no Awake.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSfxAudioSource();
    }

    private void Start()
    {
        EnsureSfxMixerRouting();
    }

    private void EnsureSfxAudioSource()
    {
        if (_audioSource == null)
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.loop = false;
        EnsureSfxMixerRouting();
    }

    /// Batida cardíaca e revive tocam pelo grupo SFX do AudioMixer (slider de SFX do menu).
    private void EnsureSfxMixerRouting()
    {
        if (_audioSource == null)
            return;

        GameAudioSettings.BindSfxOutput(_audioSource);
    }

    // Limpa singleton ao destruir.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // Limpa feedback se a cena não for gameplay.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_instance == null)
            return;
        if (!GameplaySceneBootstrap.IsGameplayScene(scene.name))
            _instance.ClearFeedback();
    }

    // Handler para evento de jogador downed (garante feedback).
    private static void HandlePlayerDowned(ulong clientId)
    {
        if (!IsMultiplayerReviveContext())
            return;
        EnsureExists();
    }

    // Handler para evento de revive (SFX + pulso de sucesso — só MP).
    private static void HandlePlayerRevived(ulong clientId)
    {
        if (!IsMultiplayerReviveContext())
            return;

        EnsureExists();
        if (_instance == null)
            return;

        _instance.ClearFeedback(stopAudio: false);
        DownedPlayerConfig config = DownedPlayerConfigUtility.Resolve();
        AudioClip reviveClip = config != null ? config.reviveCompleteClip : null;
        if (reviveClip != null)
            _instance.PlayClip(reviveClip, ReviveCompleteSfxVolume);

        GameplayVignetteController.TriggerReviveSuccessPulse(ReviveSuccessPulsePeak, ReviveSuccessPulseDuration);
    }

    // Handler para evento de bleed out (limpa feedback).
    private static void HandlePlayerBleedOut(ulong clientId)
    {
        if (!IsMultiplayerReviveContext())
            return;
        _instance?.ClearFeedback();
    }

    // Atualiza HUD e efeitos na tela a cada frame.
    private void LateUpdate()
    {
        if (!IsMultiplayerReviveContext() || !ShouldRunInCurrentScene())
        {
            ClearFeedback();
            return;
        }
        NetworkPlayerHealth downed = FindActiveRevivableDowned(out bool anyFightingAlly);
        if (downed == null)
        {
            ClearFeedback();
            return;
        }
        DownedPlayerConfig config = downed.DownedConfig ?? DownedPlayerConfigUtility.Resolve();
        float duration = Mathf.Max(1f, downed.UnconsciousDuration);
        float remaining = Mathf.Max(0f, downed.UnconsciousTimeRemaining);
        if (!DownedReviveFeedbackUtility.ShouldShowFeedback(anyFightingAlly, downed.CanBeRevived, remaining))
        {
            ClearFeedback();
            return;
        }
        float urgency = DownedReviveFeedbackUtility.ComputeUrgency(remaining, duration);
        float baseIntensity = config != null ? config.downedScreenPulseIntensity : 0.38f;
        float stress = DownedReviveFeedbackUtility.ComputePulseStress(baseIntensity, urgency);
        GameplayVignetteController.SetDownedRevivePulse(true, stress, urgency);
        EnsureTimerHud(config);
        bool isLocalDowned = IsLocalOwner(downed);
        float pulse = GameplayVignetteController.SampleDownedHeartbeatPulse(urgency);
        _timerHud?.Refresh(
            config: config,
            visible: true,
            secondsRemaining: Mathf.CeilToInt(remaining),
            paused: downed.IsReviveTimerPaused,
            isLocalDowned: isLocalDowned,
            pulse01: pulse);
        UpdateHeartbeatAudio(config, urgency, pulse);
    }

    // Gera efeito e áudio do batimento cardíaco (janela de downed — só MP).
    private void UpdateHeartbeatAudio(DownedPlayerConfig config, float urgency, float pulse)
    {
        AudioClip clip = config != null ? config.downedHeartbeatClip : null;
        if (clip == null || _audioSource == null)
            return;
        bool beatPeak = pulse > 0.88f;
        if (beatPeak && _heartbeatBeatArmed)
        {
            float volume = Mathf.Lerp(0.45f, 0.85f, urgency);
            PlayClip(clip, volume);
            _heartbeatBeatArmed = false;
        }
        else if (!beatPeak)
        {
            _heartbeatBeatArmed = true;
        }
    }

    // Toca SFX no audio source (grupo SFX do AudioMixer).
    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        EnsureSfxAudioSource();
        EnsureSfxMixerRouting();
        if (_audioSource == null)
            return;

        _audioSource.PlayOneShot(clip, volume);
    }

    // Garante HUD de timer do revive está presente.
    private void EnsureTimerHud(DownedPlayerConfig config)
    {
        if (_timerHud != null)
            return;
        GameplayHudController hudController = FindFirstObjectByType<GameplayHudController>();
        if (hudController == null)
            return;
        Transform feedbackLayer = hudController.transform.Find($"{GameplayHudController.LayersRootName}/{GameplayHudController.FeedbackLayerName}");
        if (feedbackLayer == null)
        {
            hudController.EnsureWidgets();
            feedbackLayer = hudController.transform.Find($"{GameplayHudController.LayersRootName}/{GameplayHudController.FeedbackLayerName}");
        }
        if (feedbackLayer == null)
            return;
        _timerHud = DownedReviveTimerHud.EnsureOnLayer(feedbackLayer, config);
        _timerHud.transform.SetAsFirstSibling();
    }

    // Limpa feedback visual e sonoro da janela de downed.
    private void ClearFeedback(bool stopAudio = true)
    {
        _heartbeatBeatArmed = true;
        if (stopAudio)
            StopHeartbeatAudio();
        if (GameplayVignetteController.Instance != null)
            GameplayVignetteController.SetDownedRevivePulse(false, 0f, 0f);
        _timerHud?.SetVisible(false);
    }

    // Para o áudio do batimento cardíaco.
    private void StopHeartbeatAudio()
    {
        if (_audioSource == null)
            return;
        _audioSource.Stop();
    }

    // True se deve rodar feedback nesta cena.
    private static bool ShouldRunInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && GameplaySceneBootstrap.IsGameplayScene(scene.name);
    }

    /// Revive cooperativo / feedback de downed só existem em sessão multiplayer.
    private static bool IsMultiplayerReviveContext()
    {
        if (GameSessionContext.IsSinglePlayer)
            return false;

        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null && networkManager.IsListening;
    }

    // Procura jogador downed revivível, retorna bool se há aliado lutando.
    private static NetworkPlayerHealth FindActiveRevivableDowned(out bool anyFightingAlly)
    {
        anyFightingAlly = false;
        NetworkPlayerHealth best = null;
        float bestRemaining = float.MaxValue;
        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned)
                continue;
            if (health.CanFight)
                anyFightingAlly = true;
            if (!health.CanBeRevived)
                continue;
            float remaining = health.UnconsciousTimeRemaining;
            if (remaining < bestRemaining)
            {
                bestRemaining = remaining;
                best = health;
            }
        }
        return best;
    }

    // Retorna true se health é do jogador local.
    private static bool IsLocalOwner(NetworkPlayerHealth health)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return health != null
               && networkManager != null
               && health.IsOwner
               && health.OwnerClientId == networkManager.LocalClientId;
    }
}
