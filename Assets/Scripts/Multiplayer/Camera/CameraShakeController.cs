/// <summary>
/// CameraShakeController.cs
/// Shake da câmera com trauma (envelope²) + Perlin — menos “vibração seca” que Random por frame.
/// Aplica offset após o follow (execution order alto).
/// </summary>

using UnityEngine;

[DefaultExecutionOrder(1001)] // Depois do CinemachineBrain (1000) e do follow direto
[RequireComponent(typeof(Camera))]
public class CameraShakeController : MonoBehaviour
{
    private const float DefaultPerlinFrequency = 18f;

    public static CameraShakeController Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private CameraConfig config;

    private float _currentIntensity;
    private float _currentDuration = 0.01f;
    private float _shakeTimer;
    private float _seedX;
    private float _seedY;
    private Vector3 _shakeOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ReseedNoise();
    }

    private void LateUpdate()
    {
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector3.zero;
            return;
        }

        _shakeTimer -= Time.unscaledDeltaTime;

        float linear = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.01f, _currentDuration));
        // Trauma: forte no início, decay suave (quadrado).
        float envelope = linear * linear;
        float magnitude = _currentIntensity * envelope;

        float frequency = config != null ? config.shakePerlinFrequency : DefaultPerlinFrequency;
        float t = Time.unscaledTime * Mathf.Max(1f, frequency);

        float nx = Mathf.PerlinNoise(_seedX, t) * 2f - 1f;
        float ny = Mathf.PerlinNoise(_seedY, t + 17.13f) * 2f - 1f;

        _shakeOffset = new Vector3(nx * magnitude, ny * magnitude, 0f);
        transform.position += _shakeOffset;

        if (_shakeTimer <= 0f)
        {
            _shakeTimer = 0f;
            _currentIntensity = 0f;
            _shakeOffset = Vector3.zero;
        }
    }

    public void Shake(CameraShakePreset preset)
    {
        if (config == null)
        {
            ShakeCustom(0.2f, 0.25f);
            return;
        }

        switch (preset)
        {
            case CameraShakePreset.Light:
                ShakeCustom(config.shakeLightIntensity, config.shakeLightDuration);
                break;
            case CameraShakePreset.Medium:
                ShakeCustom(config.shakeMediumIntensity, config.shakeMediumDuration);
                break;
            case CameraShakePreset.Heavy:
                ShakeCustom(config.shakeHeavyIntensity, config.shakeHeavyDuration);
                break;
        }
    }

    public void Shake() => Shake(CameraShakePreset.Medium);

    public void ShakeCustom(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f)
            return;

        if (_shakeTimer > 0f)
        {
            _currentIntensity = Mathf.Max(_currentIntensity, intensity);
            _currentDuration = Mathf.Max(_currentDuration, duration);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }
        else
        {
            _currentIntensity = intensity;
            _currentDuration = duration;
            _shakeTimer = duration;
            ReseedNoise();
        }
    }

    public void StopShake()
    {
        _shakeTimer = 0f;
        _currentIntensity = 0f;
        _shakeOffset = Vector3.zero;
    }

    public bool IsShaking => _shakeTimer > 0f;

    private void ReseedNoise()
    {
        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
    }
}
