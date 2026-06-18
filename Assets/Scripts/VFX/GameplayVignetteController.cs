using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Vinheta runtime via URP Volume. Usada na morte do jogador (solo e MP).
/// </summary>
public class GameplayVignetteController : MonoBehaviour
{
    private static GameplayVignetteController _instance;

    private Volume _volume;
    private Vignette _vignette;
    private float _currentIntensity;
    private Coroutine _deathSequence;

    public static GameplayVignetteController Instance => _instance;

    public float CurrentIntensity => _currentIntensity;

    public static void EnsureExists()
    {
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

    /// <summary>Vinheta dramática de morte — rampa, pico e hold até ClearIfActive.</summary>
    public static void PlayDeathSequence(float totalDuration, float peakIntensity = 0.68f)
    {
        EnsureExists();
        if (_instance == null)
            return;

        _instance.StartDeathSequence(totalDuration, peakIntensity);
    }

    public static void ClearIfActive()
    {
        if (_instance == null)
            return;

        _instance.StopDeathSequence();
        _instance.SetIntensity(0f);
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
        _vignette.smoothness.Override(0.42f);
        _vignette.color.Override(new Color(0.04f, 0f, 0.02f, 1f));
        _volume.profile = profile;
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

        while (elapsed < rampIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / rampIn));
            SetIntensity(Mathf.Lerp(0f, peakIntensity, t));
            yield return null;
        }

        SetIntensity(peakIntensity);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
