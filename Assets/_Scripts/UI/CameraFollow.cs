using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

[DefaultExecutionOrder(1000)] // roda LateUpdate DEPOIS do CinemachineBrain
public class FollowCamera : MonoBehaviour
{
    public static FollowCamera Instance { get; private set; }

    public Transform target;
    public float yOffset = 1f;
    public float xOffset = 1f;
    public float smoothTime = 0.25f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.25f;

    [Header("Zoom Inicial")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [Tooltip("O quanto vai dar zoom in (subtraído do valor do inspector). Ex: 2 = zoom in de 2 unidades")]
    [SerializeField] private float zoomInAmount = 2f;
    [SerializeField] private float zoomInDuration = 0.6f;

    private float _baseSize;
    private float _zoomTimer = 0f;
    private bool _isZooming = false;

    private Vector3 _shakeOffset = Vector3.zero;
    private float _shakeTimer = 0f;
    private float _currentShakeIntensity = 0f;
    private float _currentShakeDuration = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
        {
            _baseSize = cinemachineCamera.Lens.OrthographicSize;
            _isZooming = true;
        }
    }

    void Update()
    {
        if (_isZooming && cinemachineCamera != null)
        {
            _zoomTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_zoomTimer / zoomInDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            var lens = cinemachineCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(_baseSize, _baseSize - zoomInAmount, smoothT);
            cinemachineCamera.Lens = lens;

            if (t >= 1f)
                _isZooming = false;
        }

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float progress = _shakeTimer / _currentShakeDuration;
            float magnitude = _currentShakeIntensity * progress;
            _shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * magnitude,
                Random.Range(-1f, 1f) * magnitude,
                0f);
            if (_shakeTimer <= 0f)
                _shakeOffset = Vector3.zero;
        }
    }

    // Roda DEPOIS do CinemachineBrain (que também usa LateUpdate)
    // Por isso aplicamos o shake aqui: o Brain já definiu transform.position,
    // nós apenas somamos o offset por cima.
    void LateUpdate()
    {
        if (_shakeOffset != Vector3.zero)
            transform.position += _shakeOffset;
    }

    public void Shake(float intensity = -1f, float duration = -1f)
    {
        _currentShakeIntensity = intensity < 0f ? shakeIntensity : intensity;
        _currentShakeDuration = duration < 0f ? shakeDuration : duration;
        _shakeTimer = _currentShakeDuration;
    }
}
