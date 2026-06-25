using UnityEngine;

/// <summary>
/// Trajeto configurável da carruagem (waypoints na cena).
/// </summary>
[DisallowMultipleComponent]
public class CarriagePath : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    public int WaypointCount => waypoints != null ? waypoints.Length : 0;

    public Vector3 EvaluatePosition(float normalizedProgress)
    {
        if (waypoints == null || waypoints.Length == 0)
            return transform.position;

        if (waypoints.Length == 1)
            return waypoints[0].position;

        float scaled = Mathf.Clamp01(normalizedProgress) * (waypoints.Length - 1);
        int index = Mathf.FloorToInt(scaled);
        int next = Mathf.Min(index + 1, waypoints.Length - 1);
        float t = scaled - index;
        return Vector3.Lerp(waypoints[index].position, waypoints[next].position, t);
    }

    public Vector3 ArrivalPosition =>
        waypoints != null && waypoints.Length > 0
            ? waypoints[waypoints.Length - 1].position
            : transform.position;

    public void ConfigureWaypoints(Transform[] points)
    {
        waypoints = points;
    }
}
