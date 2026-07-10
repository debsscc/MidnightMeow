/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Vinheta URP em runtime — morte, pouca vida e flash de dano (solo e MP).
---------------------------------------------------------------- */

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameplayVignetteController : MonoBehaviour
{
    private const float LowHealthStartRatio = 0.42f;
    private const float CriticalHealthRatio = 0.18f;
    private const float MaxLowHealthVignette = 0.52f;
    private const float MaxCriticalVignetteBoost = 0.12f;
    private const float MaxChromaticAberration = 0.06f;
    // Usados só na sequência de morte (tela cheia, dramática).
    private const float MaxSaturationDrop = -26f;
    private const float MaxContrastBoost = 12f;
    private const float IntensitySmoothSpeed = 3.2f;
    private const float ColorSmoothSpeed = 2.1f;

    private static readonly Color NeutralVignetteColor = new Color(0.04f, 0f, 0.02f, 1f);
    private static readonly Color CriticalVignetteColor = new Color(0.58f, 0.04f, 0.05f, 1f);
    private static readonly Color LowHealthColorFilter = new Color(1f, 0.74f, 0.7f, 1f);
    private static readonly Color DeathVignetteColor = new Color(0.42f, 0.03f, 0.04f, 1f);

    private static GameplayVignetteController _instance;

    private Volume _volume;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;

    private float _currentIntensity;
    private float _healthRatio = 1f;
    private float _smoothedIntensity;
    private float _smoothedStress;
    private float _smoothedVignetteSmoothness = 0.46f;
    private Color _smoothedVignetteColor = NeutralVignetteColor;
    private Color _smoothedColorFilter = Color.white;
    private float _smoothedSaturation;
    private float _smoothedContrast;
    private float _smoothedChromatic;

    private float _damagePulse;
    private float _damagePulsePeak;
    private float _damagePulseDuration;
    private float _damagePulseEndTime;

    private Coroutine _deathSequence;
    private bool _postDeathVisualHold;

    private bool _downedRevivePulseActive;
    private float _downedReviveStress;
    private float _downedReviveUrgency;

    public static GameplayVignetteController Instance => _instance;

    public float CurrentIntensity => _currentIntensity;

    public static void EnsureExists()
    {
        if (!IsActiveGameplayScene())
            return;

        if (_instance != null)
        {
            _instance.BindToGameplayCamera();
            _instance.EnsureVolume();
            return;
        }

        Camera cam = ResolveGameplayCamera();
        if (cam == null)
            return;

        var go = new GameObject(nameof(GameplayVignetteController));
        go.transform.SetParent(cam.transform, false);
        _instance = go.AddComponent<GameplayVignetteController>();
        _instance.BindToGameplayCamera();
        _instance.EnsureVolume();
    }

    public static void PlayDeathSequence(float totalDuration, float peakIntensity = 0.58f)
    {
        EnsureExists();
        if (_instance == null)
            return;

        _instance._postDeathVisualHold = true;
        _instance._healthRatio = 0f;
        _instance.StartDeathSequence(totalDuration, peakIntensity);
    }

    public static void SetHealthRatio(float ratio)
    {
        EnsureExists();
        if (_instance == null)
            return;

        _instance._healthRatio = Mathf.Clamp01(ratio);
        if (_instance._healthRatio <= 0.001f)
            _instance._postDeathVisualHold = true;
    }

    public static void TriggerDamagePulse(float peakIntensity = 0.1f, float duration = 0.32f)
    {
        EnsureExists();
        if (_instance == null)
            return;

        _instance._damagePulsePeak = Mathf.Clamp01(peakIntensity);
        _instance._damagePulseDuration = Mathf.Max(0.05f, duration);
        _instance._damagePulseEndTime = Time.unscaledTime + _instance._damagePulseDuration;
        _instance._damagePulse = _instance._damagePulsePeak;
    }

    public static void SetDownedRevivePulse(bool active, float stress, float urgency)
    {
        EnsureExists();
        if (_instance == null)
            return;

        _instance._downedRevivePulseActive = active;
        _instance._downedReviveStress = active ? Mathf.Clamp01(stress) : 0f;
        _instance._downedReviveUrgency = active ? Mathf.Clamp01(urgency) : 0f;
    }

    public static void ClearDeathVisualHold()
    {
        if (_instance == null)
            return;

        _instance._postDeathVisualHold = false;
    }

    public static float SampleDownedHeartbeatPulse(float urgency)
    {
        float speed = Mathf.Lerp(2.6f, 4f, Mathf.Clamp01(urgency));
        float sin = Mathf.Sin(Time.unscaledTime * speed);
        return Mathf.Pow(Mathf.Max(0f, sin), 2.2f);
    }

    public static void ClearIfActive()
    {
        if (_instance == null)
            return;

        _instance.StopDeathSequence();
        _instance._postDeathVisualHold = false;
        _instance._downedRevivePulseActive = false;
        _instance._downedReviveStress = 0f;
        _instance._downedReviveUrgency = 0f;
        _instance._healthRatio = 1f;
        _instance._smoothedStress = 0f;
        _instance._smoothedIntensity = 0f;
        _instance._smoothedVignetteSmoothness = 0.46f;
        _instance._smoothedVignetteColor = NeutralVignetteColor;
        _instance._smoothedColorFilter = Color.white;
        _instance._smoothedSaturation = 0f;
        _instance._smoothedContrast = 0f;
        _instance._smoothedChromatic = 0f;
        _instance._damagePulse = 0f;
        _instance._damagePulsePeak = 0f;
        _instance._damagePulseDuration = 0f;
        _instance._damagePulseEndTime = 0f;
        _instance.ApplyVisuals(0f, NeutralVignetteColor, Color.white, 0f, 0f, 0f, 0.46f);
    }

    private static bool IsActiveGameplayScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && GameplaySceneBootstrap.IsGameplayScene(scene.name);
    }

    private static Camera ResolveGameplayCamera()
    {
        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        if (controller != null && controller.MainCamera != null)
            return controller.MainCamera;

        if (Camera.main != null)
            return Camera.main;

        return Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
    }

    private void BindToGameplayCamera()
    {
        Camera cam = ResolveGameplayCamera();
        if (cam == null)
            return;

        if (transform.parent != cam.transform)
            transform.SetParent(cam.transform, false);

        UniversalAdditionalCameraData urpData = cam.GetUniversalAdditionalCameraData();
        if (urpData != null)
            urpData.renderPostProcessing = true;
    }

    private void EnsureVolume()
    {
        if (_volume != null)
            return;

        _volume = gameObject.GetComponent<Volume>();
        if (_volume == null)
            _volume = gameObject.AddComponent<Volume>();

        _volume.isGlobal = true;
        _volume.priority = 100;
        _volume.weight = 1f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        _vignette = profile.Add<Vignette>(true);
        _vignette.active = true;
        _vignette.intensity.Override(0f);
        _vignette.smoothness.Override(0.46f);
        _vignette.color.Override(NeutralVignetteColor);

        _colorAdjustments = profile.Add<ColorAdjustments>(true);
        _colorAdjustments.active = true;
        _colorAdjustments.saturation.Override(0f);
        _colorAdjustments.contrast.Override(0f);
        _colorAdjustments.colorFilter.Override(Color.white);

        _chromaticAberration = profile.Add<ChromaticAberration>(true);
        _chromaticAberration.active = true;
        _chromaticAberration.intensity.Override(0f);

        _volume.profile = profile;
    }

    private void Update()
    {
        if (_deathSequence != null)
            return;

        UpdateDamagePulseDecay();
        ApplyLowHealthFrame();
    }

    private void UpdateDamagePulseDecay()
    {
        if (_damagePulseEndTime <= 0f)
        {
            _damagePulse = 0f;
            return;
        }

        float remaining = _damagePulseEndTime - Time.unscaledTime;
        if (remaining <= 0f)
        {
            _damagePulse = 0f;
            _damagePulseEndTime = 0f;
            return;
        }

        float t = remaining / _damagePulseDuration;
        _damagePulse = _damagePulsePeak * t * t;
    }

    private void ApplyLowHealthFrame()
    {
        float targetStress;
        float heartbeat = 0f;

        if (_downedRevivePulseActive)
        {
            targetStress = _downedReviveStress;
            heartbeat = SampleDownedHeartbeatPulse(_downedReviveUrgency) * _downedReviveStress * 0.22f;
        }
        else
        {
            targetStress = ComputeStress(_healthRatio);
            if (_postDeathVisualHold)
                targetStress = Mathf.Max(targetStress, 0.92f);

            if (_smoothedStress > 0.35f && !_postDeathVisualHold)
            {
                float speed = Mathf.Lerp(3.2f, 5f, _smoothedStress);
                heartbeat = Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * speed)) * _smoothedStress * MaxCriticalVignetteBoost;
            }
        }

        float intensityDelta = Time.unscaledDeltaTime * IntensitySmoothSpeed;
        float colorDelta = Time.unscaledDeltaTime * ColorSmoothSpeed;

        _smoothedStress = Mathf.Lerp(_smoothedStress, targetStress, intensityDelta);

        float targetIntensity = _smoothedStress * MaxLowHealthVignette + _damagePulse + heartbeat;
        _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, targetIntensity, intensityDelta);

        Color targetVignetteColor = Color.Lerp(NeutralVignetteColor, CriticalVignetteColor, _smoothedStress);
        // Vermelho só nas bordas (vinheta). Sem tinta/saturação de tela cheia.
        Color targetColorFilter = Color.white;
        float targetSaturation = 0f;
        float targetContrast = 0f;
        float targetChromatic = _smoothedStress * MaxChromaticAberration;
        float targetSmoothness = Mathf.Lerp(0.34f, 0.42f, _smoothedStress);

        _smoothedVignetteColor = Color.Lerp(_smoothedVignetteColor, targetVignetteColor, colorDelta);
        _smoothedColorFilter = Color.Lerp(_smoothedColorFilter, targetColorFilter, colorDelta);
        _smoothedSaturation = Mathf.Lerp(_smoothedSaturation, targetSaturation, colorDelta);
        _smoothedContrast = Mathf.Lerp(_smoothedContrast, targetContrast, colorDelta);
        _smoothedChromatic = Mathf.Lerp(_smoothedChromatic, targetChromatic, colorDelta);
        _smoothedVignetteSmoothness = Mathf.Lerp(_smoothedVignetteSmoothness, targetSmoothness, colorDelta);

        ApplyVisuals(
            _smoothedIntensity,
            _smoothedVignetteColor,
            _smoothedColorFilter,
            _smoothedSaturation,
            _smoothedContrast,
            _smoothedChromatic,
            _smoothedVignetteSmoothness);
    }

    private static float ComputeStress(float healthRatio)
    {
        if (healthRatio >= LowHealthStartRatio)
            return 0f;

        return 1f - Mathf.InverseLerp(CriticalHealthRatio, LowHealthStartRatio, healthRatio);
    }

    private void ApplyVisuals(
        float intensity,
        Color vignetteColor,
        Color colorFilter,
        float saturation,
        float contrast,
        float chromatic,
        float smoothness)
    {
        EnsureVolume();
        _currentIntensity = Mathf.Clamp01(intensity);

        if (_vignette != null)
        {
            _vignette.intensity.Override(_currentIntensity);
            _vignette.color.Override(vignetteColor);
            _vignette.smoothness.Override(smoothness);
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.colorFilter.Override(colorFilter);
            _colorAdjustments.saturation.Override(saturation);
            _colorAdjustments.contrast.Override(contrast);
        }

        if (_chromaticAberration != null)
            _chromaticAberration.intensity.Override(chromatic);
    }

    public void SetIntensity(float intensity)
    {
        EnsureExists();
        EnsureVolume();
        _currentIntensity = Mathf.Clamp01(intensity);
        if (_vignette != null)
            _vignette.intensity.Override(_currentIntensity);
    }

    private void StartDeathSequence(float totalDuration, float peakIntensity)
    {
        StopDeathSequence();
        _deathSequence = StartCoroutine(DeathSequenceRoutine(totalDuration, peakIntensity));
    }

    private void StopDeathSequence()
    {
        if (_deathSequence == null)
            return;

        StopCoroutine(_deathSequence);
        _deathSequence = null;
    }

    private IEnumerator DeathSequenceRoutine(float totalDuration, float peakIntensity)
    {
        float duration = Mathf.Max(1f, totalDuration);
        float rampIn = Mathf.Clamp(duration * 0.32f, 1.2f, 3f);
        float elapsed = 0f;

        Color deathFilter = Color.Lerp(LowHealthColorFilter, new Color(1f, 0.68f, 0.64f, 1f), 0.35f);

        while (elapsed < rampIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / rampIn));
            float intensity = Mathf.Lerp(_smoothedIntensity, peakIntensity, t);
            Color vignette = Color.Lerp(_smoothedVignetteColor, DeathVignetteColor, t);
            Color filter = Color.Lerp(_smoothedColorFilter, deathFilter, t);
            ApplyVisuals(intensity, vignette, filter, MaxSaturationDrop * 0.45f, MaxContrastBoost * 0.35f, 0f, 0.52f);
            yield return null;
        }

        ApplyVisuals(peakIntensity, DeathVignetteColor, deathFilter, MaxSaturationDrop * 0.45f, MaxContrastBoost * 0.35f, 0f, 0.52f);
        _smoothedIntensity = peakIntensity;
        _smoothedVignetteColor = DeathVignetteColor;
        _smoothedColorFilter = deathFilter;
        _smoothedSaturation = MaxSaturationDrop * 0.45f;
        _smoothedContrast = MaxContrastBoost * 0.35f;
        _smoothedStress = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _deathSequence = null;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}

