using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Vinheta runtime via URP Volume (sem depender de perfil na cena).
/// </summary>
public class GameplayVignetteController : MonoBehaviour
{
    private static GameplayVignetteController _instance;

    private Volume _volume;
    private Vignette _vignette;
    private float _currentIntensity;

    public static GameplayVignetteController Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            Camera cam = Camera.main;
            if (cam == null)
                cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            if (cam == null)
                return null;

            var go = new GameObject(nameof(GameplayVignetteController));
            go.transform.SetParent(cam.transform, false);
            _instance = go.AddComponent<GameplayVignetteController>();
            _instance.EnsureVolume();
            return _instance;
        }
    }

    public float CurrentIntensity => _currentIntensity;

    private void EnsureVolume()
    {
        if (_volume != null)
            return;

        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 50;
        _volume.weight = 1f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _vignette = profile.Add<Vignette>(true);
        _vignette.active = true;
        _vignette.intensity.Override(0f);
        _vignette.smoothness.Override(0.35f);
        _vignette.color.Override(Color.black);
        _volume.profile = profile;
    }

    public void SetIntensity(float intensity)
    {
        EnsureVolume();
        _currentIntensity = Mathf.Clamp01(intensity);
        if (_vignette != null)
            _vignette.intensity.Override(_currentIntensity);
    }

    public static void ClearIfActive()
    {
        if (_instance == null)
            return;

        _instance.SetIntensity(0f);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
