using UnityEngine;

/// <summary>
/// Gira as rodas localmente (todos os peers) a partir do progresso/distância da carruagem.
/// Não sincroniza ângulo na rede — deriva do movimento já replicado.
/// </summary>
[DisallowMultipleComponent]
public sealed class CarriageWheelSpinner : MonoBehaviour
{
    [SerializeField] private Transform frontWheel;
    [SerializeField] private Transform backWheel;
    [SerializeField] private float frontWheelRadius = 0.28f;
    [SerializeField] private float backWheelRadius = 0.36f;

    private CarriageController _carriage;
    private NetworkCarriageHealth _health;
    private float _lastProgress = -1f;
    private Vector3 _lastWorldPos;

    public void Configure(Transform front, Transform back, float frontRadius, float backRadius)
    {
        frontWheel = front;
        backWheel = back;
        frontWheelRadius = Mathf.Max(0.05f, frontRadius);
        backWheelRadius = Mathf.Max(0.05f, backRadius);
    }

    private void Awake()
    {
        _carriage = GetComponentInParent<CarriageController>();
        _health = GetComponentInParent<NetworkCarriageHealth>();
    }

    private void OnEnable()
    {
        CaptureBaseline();
    }

    private void LateUpdate()
    {
        if (_carriage == null || frontWheel == null && backWheel == null)
            return;

        if (GameEvents.IsPaused || _carriage.HasArrived)
            return;

        if (_health != null && _health.IsBroken)
            return;

        float distance = ResolveTravelledDistance();
        if (distance <= 0.00005f)
            return;

        // Movimento +X (direita): rotação horária em 2D = Z negativo.
        const float directionSign = -1f;
        ApplyRotation(frontWheel, frontWheelRadius, distance * directionSign);
        ApplyRotation(backWheel, backWheelRadius, distance * directionSign);
    }

    private float ResolveTravelledDistance()
    {
        CarriagePath path = _carriage.Path;
        float progress = _carriage.PathProgress;

        if (path != null && path.WaypointCount >= 2 && _lastProgress >= 0f)
        {
            float deltaProgress = Mathf.Abs(progress - _lastProgress);
            _lastProgress = progress;
            return deltaProgress * Mathf.Max(0.1f, path.GetTotalLength());
        }

        _lastProgress = progress;

        Vector3 worldPos = _carriage.transform.position;
        float delta = Vector2.Distance(worldPos, _lastWorldPos);
        _lastWorldPos = worldPos;
        return delta;
    }

    private void CaptureBaseline()
    {
        if (_carriage == null)
            return;

        _lastProgress = _carriage.PathProgress;
        _lastWorldPos = _carriage.transform.position;
    }

    private static void ApplyRotation(Transform wheel, float radius, float signedDistance)
    {
        if (wheel == null || radius < 0.01f)
            return;

        float degrees = (signedDistance / radius) * Mathf.Rad2Deg;
        wheel.Rotate(0f, 0f, degrees, Space.Self);
    }
}
