using UnityEngine;

/// <summary>
/// Desenha o trapézio do ataque melee em Play Mode (LineRenderer) e no editor (Gizmos).
/// </summary>
[RequireComponent(typeof(PlayerMeleeCombat))]
public class MeleeAttackDebugVisual : MonoBehaviour
{
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool showInPlayMode = true;
    [SerializeField] private Color fillColor = new Color(1f, 0.35f, 0.1f, 0.25f);
    [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] private float lineWidth = 0.04f;

    private PlayerMeleeCombat _melee;
    private LineRenderer _lineRenderer;
    private float _displayTimer;
    private Vector2 _lastOrigin;
    private Vector2 _lastForward;
    private MeleeCombatStats _lastStats;

    private void Awake()
    {
        _melee = GetComponent<PlayerMeleeCombat>();
        EnsureLineRenderer();
    }

    private void OnEnable()
    {
        if (_melee != null)
            _melee.OnAttackPerformed += HandleAttackPerformed;
    }

    private void OnDisable()
    {
        if (_melee != null)
            _melee.OnAttackPerformed -= HandleAttackPerformed;
    }

    private void HandleAttackPerformed(Vector2 origin, Vector2 forward, MeleeCombatStats stats)
    {
        _lastOrigin = origin;
        _lastForward = forward;
        _lastStats = stats;
        _displayTimer = 0.35f;
        RefreshLineRenderer();
    }

    private void LateUpdate()
    {
        if (!showInPlayMode || _displayTimer <= 0f)
        {
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
            return;
        }

        _displayTimer -= Time.deltaTime;
        RefreshLineRenderer();
    }

    private void RefreshLineRenderer()
    {
        if (_lastStats == null || _lineRenderer == null) return;

        EnsureLineRenderer();
        _lineRenderer.enabled = true;

        float z = transform.position.z;
        var corners = MeleeHitUtility.GetTrapezoidWorldCorners(
            _lastOrigin,
            _lastForward,
            _lastStats.attackRange,
            _lastStats.nearHalfWidth,
            _lastStats.farHalfWidth,
            z);

        _lineRenderer.positionCount = 5;
        for (int i = 0; i < 4; i++)
            _lineRenderer.SetPosition(i, corners[i]);
        _lineRenderer.SetPosition(4, corners[0]);
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null) return;

        var go = new GameObject("MeleeDebugOutline");
        go.transform.SetParent(transform, false);
        _lineRenderer = go.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = false;
        _lineRenderer.widthMultiplier = lineWidth;
        var shader = Shader.Find("MidnightMeow/AbilityZoneFill");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        _lineRenderer.material = new Material(shader);
        _lineRenderer.startColor = outlineColor;
        _lineRenderer.endColor = outlineColor;
        _lineRenderer.sortingOrder = 50;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;
        DrawGizmoShape();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;
        DrawGizmoShape();
    }

    private void DrawGizmoShape()
    {
        if (_melee == null)
            _melee = GetComponent<PlayerMeleeCombat>();

        var stats = _melee != null ? _melee.CombatStats : null;
        if (stats == null) return;

        Vector2 origin = _melee.AttackOriginPosition;
        Vector2 forward = Application.isPlaying ? _lastForward : Vector2.up;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector2.up;

        float offset = stats.attackOriginForwardOffset;
        if (offset > 0f)
            origin += forward.normalized * offset;

        float z = transform.position.z;
        var corners = MeleeHitUtility.GetTrapezoidWorldCorners(
            origin, forward, stats.attackRange, stats.nearHalfWidth, stats.farHalfWidth, z);

        Gizmos.color = fillColor;
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        Gizmos.color = outlineColor;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }
}
