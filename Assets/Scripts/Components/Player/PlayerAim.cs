///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla a mira do jogador com o cursor (mouse). O analógico direito move o cursor via GamepadCursorDriver.
// Mira e flip seguem a posição do cursor no mundo. Twin-stick só como fallback sem mouse.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.InputSystem;
using System;

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

    public readonly struct AimPipelineSnapshot
    {
        public readonly string Context;
        public readonly bool Success;
        public readonly string Reason;
        public readonly string CameraName;
        public readonly Vector3 CameraPosition;
        public readonly bool CameraIsOrthographic;
        public readonly bool HasMouse;
        public readonly Vector2 MouseScreenPosition;
        public readonly Vector3 MouseWorldPosition;
        public readonly bool RayHitPlane;
        public readonly Vector3 PlayerPosition;
        public readonly Vector3 FirePointBeforePosition;
        public readonly Vector3 FirePointAfterPosition;
        public readonly Vector3 FirePointBeforeEuler;
        public readonly Vector3 FirePointAfterEuler;
        public readonly Vector2 AimDirection;
        public readonly float Radius;

        public AimPipelineSnapshot(
            string context,
            bool success,
            string reason,
            Camera camera,
            bool hasMouse,
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            bool rayHitPlane,
            Vector3 playerPosition,
            Vector3 firePointBeforePosition,
            Vector3 firePointAfterPosition,
            Vector3 firePointBeforeEuler,
            Vector3 firePointAfterEuler,
            Vector2 aimDirection,
            float radius)
        {
            Context = context;
            Success = success;
            Reason = reason;
            CameraName = camera != null ? camera.name : "null";
            CameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            CameraIsOrthographic = camera != null && camera.orthographic;
            HasMouse = hasMouse;
            MouseScreenPosition = mouseScreenPosition;
            MouseWorldPosition = mouseWorldPosition;
            RayHitPlane = rayHitPlane;
            PlayerPosition = playerPosition;
            FirePointBeforePosition = firePointBeforePosition;
            FirePointAfterPosition = firePointAfterPosition;
            FirePointBeforeEuler = firePointBeforeEuler;
            FirePointAfterEuler = firePointAfterEuler;
            AimDirection = aimDirection;
            Radius = radius;
        }
    }

    // Refer�ncias do Inspector
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerStats stats;
    [Tooltip("Magnitude mínima do analógico direito para assumir a mira (twin-stick).")]
    [SerializeField] private float gamepadAimDeadzone = 0.15f;
    private float _attackRangeOverride = -1f; 

    private PlayerInputHandler _input;
    private Camera _mainCamera;
    private Vector2 _mousePosition;
    private Vector2 _currentAimDirection = Vector2.up;
    private Vector3 _currentFirePointPosition;
    private Quaternion _currentFirePointRotation = Quaternion.identity;
    private AimDebugSnapshot _lastDebugSnapshot;

    public event Action<AimPipelineSnapshot> OnAimPipelineSampled;

    private void Awake()
    {
        /// Pega a refer�ncia para a c�mera principal
        _mainCamera = Camera.main;
        _input = GetComponent<PlayerInputHandler>();
        if (firePoint != null)
        {
            _currentFirePointPosition = firePoint.position;
            _currentFirePointRotation = firePoint.rotation;
        }
    }

    private void Update()
    {
        RefreshAim("Update", false);
    }

    /// <summary>
    /// Atualiza imediatamente a direção da mira e o firePoint. Chamado pelo Update e
    /// também pelo PlayerShooting antes de disparar, garantindo que o RPC use a mira atual.
    /// </summary>
    public bool RefreshAim()
    {
        return RefreshAim("Manual", true);
    }

    private bool RefreshAim(string context, bool emitDebug)
    {
        if (firePoint == null)
        {
            if (emitDebug)
                EmitAimPipeline(context, false, "firePoint ausente", null, false, default, default, false, default, default, default, default, default, Vector2.up, 0f);
            return false;
        }

        Vector3 beforePosition = firePoint.position;
        Vector3 beforeEuler = firePoint.eulerAngles;
        float radius = ResolveFirePointRadius();

        _mainCamera = ResolveAimCamera();

        // Cursor/pointer primeiro: flip, firePoint e mira seguem a posição do cursor
        // (inclui warp do analógico direito via GamepadCursorDriver).
        // Pointer cobre Mouse e Pen — no Windows o clique às vezes não atualiza Mouse.current.
        if (_mainCamera != null && TryReadPointerScreenPosition(out _mousePosition))
        {
            Vector3 mouseWorldPos = GetMouseWorldPositionOnPlayerPlane(_mousePosition, out bool rayHitPlane);

            Vector2 lookDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;
            _lastDebugSnapshot = new AimDebugSnapshot(
                _mousePosition,
                mouseWorldPos,
                lookDirection,
                true,
                rayHitPlane,
                _mainCamera.orthographic);

            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                _currentAimDirection = lookDirection.normalized;
                ApplyFirePointPose(_currentAimDirection);
                if (emitDebug)
                    EmitAimPipeline(context, true, "mouse", _mainCamera, true, _mousePosition, mouseWorldPos, rayHitPlane, transform.position, beforePosition, firePoint.position, beforeEuler, firePoint.eulerAngles, _currentAimDirection, radius);
                return true;
            }

            ApplyFirePointPose(_currentAimDirection);
            if (emitDebug)
                EmitAimPipeline(context, true, "mouse-zero-keep-last", _mainCamera, true, _mousePosition, mouseWorldPos, rayHitPlane, transform.position, beforePosition, firePoint.position, beforeEuler, firePoint.eulerAngles, _currentAimDirection, radius);
            return true;
        }

        // Fallback twin-stick: só quando não há mouse (ex.: build sem cursor).
        Vector2 stickAim = _input != null ? _input.AimInput : Vector2.zero;
        if (stickAim.sqrMagnitude >= gamepadAimDeadzone * gamepadAimDeadzone)
        {
            _currentAimDirection = stickAim.normalized;
            ApplyFirePointPose(_currentAimDirection);
            if (emitDebug)
                EmitAimPipeline(context, true, "gamepad", _mainCamera, false, default, default, false, transform.position, beforePosition, firePoint.position, beforeEuler, firePoint.eulerAngles, _currentAimDirection, radius);
            return true;
        }

        if (GenericControllerInput.TryReadAim(out Vector2 genericAim)
            && genericAim.sqrMagnitude >= gamepadAimDeadzone * gamepadAimDeadzone)
        {
            _currentAimDirection = genericAim.normalized;
            ApplyFirePointPose(_currentAimDirection);
            if (emitDebug)
                EmitAimPipeline(context, true, "generic-hid-aim", _mainCamera, false, default, default, false, transform.position, beforePosition, firePoint.position, beforeEuler, firePoint.eulerAngles, _currentAimDirection, radius);
            return true;
        }

        if (_mainCamera == null)
        {
            if (emitDebug)
                EmitAimPipeline(context, false, "camera ausente", null, Pointer.current != null || Mouse.current != null, default, default, false, transform.position, beforePosition, beforePosition, beforeEuler, beforeEuler, _currentAimDirection, radius);
            return false;
        }

        // Sem mouse e sem stick: mantém a última direção mirada.
        ApplyFirePointPose(_currentAimDirection);
        if (emitDebug)
            EmitAimPipeline(context, true, "keep-last (sem mouse/stick)", _mainCamera, Pointer.current != null || Mouse.current != null, default, default, false, transform.position, beforePosition, firePoint.position, beforeEuler, firePoint.eulerAngles, _currentAimDirection, radius);
        return true;
    }

    private static bool TryReadPointerScreenPosition(out Vector2 screenPosition)
    {
        if (Pointer.current != null)
        {
            screenPosition = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Pen.current != null)
        {
            screenPosition = Pen.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    public void SetAimCamera(Camera camera)
    {
        if (camera != null)
            _mainCamera = camera;
    }

    private Camera ResolveAimCamera()
    {
        Camera multiplayerCamera = MultiplayerCameraController.Instance != null
            ? MultiplayerCameraController.Instance.MainCamera
            : null;

        if (multiplayerCamera != null && multiplayerCamera.isActiveAndEnabled)
            return multiplayerCamera;

        return Camera.main;
    }

    private Vector3 GetMouseWorldPositionOnPlayerPlane(Vector2 mouseScreenPosition, out bool rayHitPlane)
    {
        Plane aimPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPosition);
        rayHitPlane = aimPlane.Raycast(ray, out float enterDistance);
        if (rayHitPlane)
            return ray.GetPoint(enterDistance);

        Vector3 fallbackScreenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            _mainCamera.WorldToScreenPoint(transform.position).z
        );
        return _mainCamera.ScreenToWorldPoint(fallbackScreenPosition);
    }

    private void ApplyFirePointPose(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float radius = ResolveFirePointRadius();

        // Rotaciona e posiciona o firePoint para olhar na direção do mouse
        _currentFirePointRotation = Quaternion.Euler(0, 0, angle - 90f);
        _currentFirePointPosition = transform.position + (Vector3)(direction.normalized * radius);
        firePoint.SetPositionAndRotation(_currentFirePointPosition, _currentFirePointRotation);
    }

    private float ResolveFirePointRadius()
    {
        if (_attackRangeOverride > 0f)
            return _attackRangeOverride;

        return stats != null ? stats.firePointRadius : Mathf.Max(0.01f, firePoint.localPosition.magnitude);
    }

    public void ApplyRuntimeStats(PlayerStats runtimeStats) => stats = runtimeStats;

    public void ApplyRangedCombatStats(RangedCombatStats rangedStats)
    {
        if (rangedStats != null && rangedStats.attackRange > 0f)
            _attackRangeOverride = rangedStats.attackRange;
    }

    private void EmitAimPipeline(
        string context,
        bool success,
        string reason,
        Camera camera,
        bool hasMouse,
        Vector2 mouseScreenPosition,
        Vector3 mouseWorldPosition,
        bool rayHitPlane,
        Vector3 playerPosition,
        Vector3 firePointBeforePosition,
        Vector3 firePointAfterPosition,
        Vector3 firePointBeforeEuler,
        Vector3 firePointAfterEuler,
        Vector2 aimDirection,
        float radius)
    {
        OnAimPipelineSampled?.Invoke(new AimPipelineSnapshot(
            context,
            success,
            reason,
            camera,
            hasMouse,
            mouseScreenPosition,
            mouseWorldPosition,
            rayHitPlane,
            playerPosition,
            firePointBeforePosition,
            firePointAfterPosition,
            firePointBeforeEuler,
            firePointAfterEuler,
            aimDirection,
            radius
        ));
    }

    public bool TryGetFirePose(out Vector3 position, out Quaternion rotation, out Vector2 direction)
    {
        bool refreshed = RefreshAim("TryGetFirePose", true);
        direction = _currentAimDirection;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = firePoint != null ? (Vector2)firePoint.up : Vector2.up;

        direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.up;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rotation = refreshed ? _currentFirePointRotation : Quaternion.Euler(0f, 0f, angle);
        position = refreshed
            ? _currentFirePointPosition
            : (firePoint != null ? firePoint.position : transform.position);

        return refreshed;
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