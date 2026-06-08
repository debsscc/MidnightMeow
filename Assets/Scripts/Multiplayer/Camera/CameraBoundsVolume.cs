using UnityEngine;

/// <summary>
/// Delimitador de câmera para Fase-1 (e outras fases). Use um <see cref="PolygonCollider2D"/>
/// para desenhar o polígono no Editor; o <see cref="MultiplayerCameraController"/> liga
/// automaticamente ao Cinemachine Confiner.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public class CameraBoundsVolume : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D boundsCollider;

    public Collider2D BoundsCollider => boundsCollider;

    private void Awake()
    {
        if (boundsCollider == null)
            boundsCollider = GetComponent<PolygonCollider2D>();

        boundsCollider.isTrigger = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (boundsCollider == null)
            boundsCollider = GetComponent<PolygonCollider2D>();
    }

    private void OnDrawGizmosSelected()
    {
        if (boundsCollider == null)
            return;

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
        for (int path = 0; path < boundsCollider.pathCount; path++)
        {
            Vector2[] points = boundsCollider.GetPath(path);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = transform.TransformPoint(points[i]);
                Vector2 b = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(a, b);
            }
        }
    }
#endif
}
