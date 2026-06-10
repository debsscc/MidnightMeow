using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Converte posição do mouse em ponto no chão com clamp de alcance máximo.
/// </summary>
public static class AbilityPlacementUtility
{
    public readonly struct PlacementResult
    {
        public readonly bool Success;
        public readonly Vector2 WorldPosition;
        public readonly Vector2 Direction;

        public PlacementResult(bool success, Vector2 worldPosition, Vector2 direction)
        {
            Success = success;
            WorldPosition = worldPosition;
            Direction = direction;
        }
    }

    public static PlacementResult TryGetPlacement(
        Transform user,
        Camera camera,
        float maxRange,
        Vector2 fallbackDirection)
    {
        if (user == null)
            return new PlacementResult(false, Vector2.zero, fallbackDirection);

        if (camera == null)
            camera = Camera.main;

        Vector2 origin = user.position;
        Vector2 target = origin;

        if (camera != null && Mouse.current != null)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, user.position.z));
            Ray ray = camera.ScreenPointToRay(mouseScreen);

            if (plane.Raycast(ray, out float distance))
                target = ray.GetPoint(distance);
            else
            {
                Vector3 screen = new Vector3(mouseScreen.x, mouseScreen.y,
                    camera.WorldToScreenPoint(user.position).z);
                target = camera.ScreenToWorldPoint(screen);
            }
        }

        Vector2 offset = target - origin;
        if (offset.sqrMagnitude > maxRange * maxRange && maxRange > 0f)
            offset = offset.normalized * maxRange;

        Vector2 worldPos = origin + offset;
        Vector2 direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : fallbackDirection.normalized;

        return new PlacementResult(true, worldPos, direction);
    }

    public static Quaternion RotationFromDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return Quaternion.identity;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, angle);
    }
}
