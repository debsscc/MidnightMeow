using UnityEngine;

/// <summary>
/// Desenha o trapézio do ataque melee em Play Mode (LineRenderer) e no editor (Gizmos).
/// </summary>
[RequireComponent(typeof(PlayerMeleeCombat))]
public class MeleeAttackDebugVisual : MonoBehaviour
{
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool showInPlayMode = false;
    [SerializeField] private Color fillColor = new Color(1f, 0.35f, 0.1f, 0.25f);
    [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] private float lineWidth = 0.04f;

    private PlayerMeleeCombat _melee;
    private PlayerPassiveHandler _passiveHandler;
    private LineRenderer _lineRenderer;
    private float _displayTimer;
    private Vector2 _lastOrigin;
    private Vector2 _lastForward;

    private void Awake()
    {
        _melee = GetComponent<PlayerMeleeCombat>();
        _passiveHandler = GetComponent<PlayerPassiveHandler>();
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
        if (_lineRenderer == null) return;
        if (!TryGetSwingShape(out Vector2 origin, out Vector2 forward, out float attackRange, out float nearHalfWidth, out float farHalfWidth))
        {
            _lineRenderer.enabled = false;
            return;
        }

        EnsureLineRenderer();
        _lineRenderer.enabled = true;

        float z = transform.position.z;
        var corners = MeleeHitUtility.GetTrapezoidWorldCorners(
            origin,
            forward,
            attackRange,
            nearHalfWidth,
            farHalfWidth,
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
        _lineRenderer.material = CombatVisualMaterials.CreateAbilityZoneFillInstance();
        _lineRenderer.startColor = outlineColor;
        _lineRenderer.endColor = outlineColor;
        _lineRenderer.sortingOrder = 50;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!drawDebugGizmos) return;
        DrawGizmoShape();
#endif
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!drawDebugGizmos) return;
        DrawGizmoShape();
#endif
    }

    private void DrawGizmoShape()
    {
        if (_melee == null)
            _melee = GetComponent<PlayerMeleeCombat>();

        if (!TryGetSwingShape(out Vector2 origin, out Vector2 forward, out float attackRange, out float nearHalfWidth, out float farHalfWidth))
            return;

        float z = transform.position.z;
        var corners = MeleeHitUtility.GetTrapezoidWorldCorners(
            origin, forward, attackRange, nearHalfWidth, farHalfWidth, z);

        Gizmos.color = fillColor;
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        Gizmos.color = outlineColor;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }

    private bool TryGetSwingShape(
        out Vector2 origin,
        out Vector2 forward,
        out float attackRange,
        out float nearHalfWidth,
        out float farHalfWidth)
    {
        origin = Vector2.zero;
        forward = Vector2.up;
        attackRange = 0f;
        nearHalfWidth = 0f;
        farHalfWidth = 0f;

        if (_melee == null)
            return false;

        var stats = _melee.CombatStats;
        if (stats == null)
            return false;

        forward = Application.isPlaying && _lastForward.sqrMagnitude > 0.0001f
            ? _lastForward
            : Vector2.up;

        origin = Application.isPlaying && _displayTimer > 0f
            ? _lastOrigin
            : _melee.AttackOriginPosition;

        float offset = stats.attackOriginForwardOffset;
        if (offset > 0f)
            origin += forward.normalized * offset;

        float areaMultiplier = _passiveHandler != null ? _passiveHandler.CleaveAreaMultiplier : 1f;
        attackRange = stats.attackRange * areaMultiplier;
        nearHalfWidth = stats.nearHalfWidth * areaMultiplier;
        farHalfWidth = stats.farHalfWidth * areaMultiplier;
        return attackRange > 0f;
    }
}
