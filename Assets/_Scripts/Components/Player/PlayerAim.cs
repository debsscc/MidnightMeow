///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Controla a mira do jogador com o mouse, posicionando e rotacionando o ponto de disparo (firePoint).
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    public readonly struct AimDebugSnapshot
    {
        public readonly Vector2 MouseScreenPosition;
        public readonly Vector3 MouseWorldPosition;
        public readonly Vector2 LookDirection;
        public readonly bool UsedRayPlane;
        public readonly bool RayHitPlane;
        public readonly bool CameraIsOrthographic;

        public AimDebugSnapshot(
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            Vector2 lookDirection,
            bool usedRayPlane,
            bool rayHitPlane,
            bool cameraIsOrthographic)
        {
            MouseScreenPosition = mouseScreenPosition;
            MouseWorldPosition = mouseWorldPosition;
            LookDirection = lookDirection;
            UsedRayPlane = usedRayPlane;
            RayHitPlane = rayHitPlane;
            CameraIsOrthographic = cameraIsOrthographic;
        }
    }

    // Refer�ncias do Inspector
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerStats stats; 

    private Camera _mainCamera;
    private Vector2 _mousePosition;
    private Vector2 _currentAimDirection = Vector2.up;
    private AimDebugSnapshot _lastDebugSnapshot;

    private void Awake()
    {
        /// Pega a refer�ncia para a c�mera principal
        _mainCamera = Camera.main; 
    }

    private void Update()
    {
        // Tenta obter a câmera principal caso não esteja disponível ainda
        // (pode ser nula nos primeiros frames após o spawn em multiplayer)
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        if (Mouse.current == null)
        {
            _currentAimDirection = firePoint != null ? (Vector2)firePoint.up : Vector2.up;
            return;
        }

        // Lê a posição do mouse na tela e converte para coordenadas do mundo.
        // Usar ray + plano evita erro em câmeras perspective (z fixo em ScreenToWorldPoint).
        _mousePosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos;
        bool rayHitPlane;
        bool usedRayPlane = !_mainCamera.orthographic;
        if (usedRayPlane)
        {
            Plane aimPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
            Ray ray = _mainCamera.ScreenPointToRay(_mousePosition);
            rayHitPlane = aimPlane.Raycast(ray, out float enterDistance);
            mouseWorldPos = rayHitPlane
                ? ray.GetPoint(enterDistance)
                : _mainCamera.ScreenToWorldPoint(new Vector3(_mousePosition.x, _mousePosition.y, Mathf.Abs(_mainCamera.transform.position.z - transform.position.z)));
        }
        else
        {
            rayHitPlane = false;
            mouseWorldPos = _mainCamera.ScreenToWorldPoint(_mousePosition);
        }

        Vector2 lookDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;
        _lastDebugSnapshot = new AimDebugSnapshot(
            _mousePosition,
            mouseWorldPos,
            lookDirection,
            usedRayPlane,
            rayHitPlane,
            _mainCamera.orthographic);

        if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            _currentAimDirection = firePoint != null ? (Vector2)firePoint.up : Vector2.up;
            return;
        }

        _currentAimDirection = lookDirection.normalized;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        // Rotaciona e posiciona o firePoint para olhar na direção do mouse
        firePoint.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Vector2 localOffset = lookDirection.normalized * stats.firePointRadius;
        firePoint.localPosition = localOffset;
    }

    public bool TryGetAimDirection(out Vector2 direction, out bool usedFallback)
    {
        direction = _currentAimDirection;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            usedFallback = true;
            direction = firePoint != null ? (Vector2)firePoint.up : Vector2.up;
            return false;
        }

        usedFallback = false;
        return true;
    }

    public bool TryGetDebugSnapshot(out AimDebugSnapshot snapshot)
    {
        snapshot = _lastDebugSnapshot;
        return _mainCamera != null;
    }
}