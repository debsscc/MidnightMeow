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

    public float GetTotalLength()
    {
        if (waypoints == null || waypoints.Length == 0)
            return 1f;

        if (waypoints.Length == 1)
            return 1f;

        float total = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null)
                continue;

            total += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }

        return Mathf.Max(0.1f, total);
    }

    public float GetNormalizedProgress(Vector3 worldPosition)
    {
        if (waypoints == null || waypoints.Length < 2)
            return 0f;

        Vector3 start = waypoints[0].position;
        Vector3 end = waypoints[waypoints.Length - 1].position;
        Vector3 segment = end - start;
        float lengthSqr = segment.sqrMagnitude;
        if (lengthSqr < 0.0001f)
            return 1f;

        float projected = Vector3.Dot(worldPosition - start, segment) / lengthSqr;
        return Mathf.Clamp01(projected);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = new Color(0.95f, 0.7f, 0.25f, 0.85f);
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.35f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
#endif
}
