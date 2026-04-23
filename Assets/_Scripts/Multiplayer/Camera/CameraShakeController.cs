/// <summary>
/// CameraShakeController.cs
/// Componente dedicado ao controle de shake da câmera, independente do sistema Cinemachine.
/// Aplica um offset de posição à câmera após o CinemachineBrain processar seu LateUpdate,
/// garantindo que o shake não seja sobrescrito pelo Cinemachine.
/// Aceita shakes por preset (definido em CameraConfig) ou por valores customizados.
/// Múltiplos shakes simultâneos são compostos (maior valor prevalece).
/// SRP: exclusivamente responsável pelo efeito de shake na câmera.
/// </summary>

using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1001)] // Roda DEPOIS do CinemachineBrain (1000) e do FollowCamera
[RequireComponent(typeof(Camera))]
public class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private CameraConfig config;

    private float _currentIntensity = 0f;
    private float _currentDuration = 0f;
    private float _shakeTimer = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void LateUpdate()
    {
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector3.zero;
            return;
        }

        _shakeTimer -= Time.deltaTime;

        // Interpolação de intensidade: maior no início, zero no final
        float progress = Mathf.Clamp01(_shakeTimer / _currentDuration);
        float magnitude = _currentIntensity * progress;

        _shakeOffset = new Vector3(
            Random.Range(-1f, 1f) * magnitude,
            Random.Range(-1f, 1f) * magnitude,
            0f
        );

        transform.position += _shakeOffset;

        if (_shakeTimer <= 0f)
            _shakeOffset = Vector3.zero;
    }

    // ── API Pública — Presets ──────────────────────────────────────────────────

    /// <summary>
    /// Aciona shake usando um dos presets configurados em CameraConfig.
    /// Seguro chamar durante gameplay; mescla com shake ativo se for mais intenso.
    /// </summary>
    public void Shake(CameraShakePreset preset)
    {
        if (config == null) { ShakeCustom(0.2f, 0.25f); return; }

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

    /// <summary>
    /// Atalho direto para shake médio — compatível com chamadas legadas (ex: HealthComponent).
    /// </summary>
    public void Shake() => Shake(CameraShakePreset.Medium);

    /// <summary>
    /// Aciona shake com intensidade e duração personalizadas.
    /// Se já houver um shake ativo e este for mais forte, o novo prevalece.
    /// </summary>
    public void ShakeCustom(float intensity, float duration)
    {
        if (intensity > _currentIntensity || _shakeTimer <= 0f)
        {
            _currentIntensity = intensity;
            _currentDuration = duration;
            _shakeTimer = duration;
        }
    }

    /// <summary>Cancela qualquer shake em andamento imediatamente.</summary>
    public void StopShake()
    {
        _shakeTimer = 0f;
        _currentIntensity = 0f;
        _shakeOffset = Vector3.zero;
    }

    /// <summary>Verdadeiro se um shake estiver ativo no momento.</summary>
    public bool IsShaking => _shakeTimer > 0f;
}
