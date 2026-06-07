using UnityEngine;

/// <summary>
/// Exibe zonas de habilidade em Play Mode (shader) e no editor (Gizmos).
/// </summary>
[DisallowMultipleComponent]
public class AbilityDebugVisualHost : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool showInPlayMode = true;
    [SerializeField] private float displayDuration = 0.45f;
    [SerializeField] private int sortingOrder = 45;

    [Header("Colors")]
    [SerializeField] private Color nixPushFill = new Color(0.2f, 0.55f, 1f, 0.35f);
    [SerializeField] private Color nixPushOutline = new Color(0.4f, 0.8f, 1f, 0.95f);
    [SerializeField] private Color nixChargeFill = new Color(1f, 0.45f, 0.1f, 0.35f);
    [SerializeField] private Color nixChargeOutline = new Color(1f, 0.75f, 0.2f, 0.95f);
    [SerializeField] private Color coraBarrierFill = new Color(0.2f, 0.95f, 0.45f, 0.35f);
    [SerializeField] private Color coraBarrierOutline = new Color(0.5f, 1f, 0.65f, 0.95f);
    [SerializeField] private Color coraPoolFill = new Color(0.75f, 0.2f, 0.95f, 0.35f);
    [SerializeField] private Color coraPoolOutline = new Color(0.9f, 0.5f, 1f, 0.95f);
    [SerializeField] private Color dashFill = new Color(0.2f, 0.95f, 0.95f, 0.3f);
    [SerializeField] private Color dashOutline = new Color(0.6f, 1f, 1f, 0.95f);

    private SpriteRenderer _zoneRenderer;
    private Material _materialInstance;
    private float _displayTimer;
    private AbilityDebugSnapshot _activeSnapshot;
    private AbilityDebugSnapshot _gizmoSnapshot;

    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int PulseId = Shader.PropertyToID("_Pulse");

    public bool DrawDebugGizmos => drawDebugGizmos;

    public void ShowAbility(
        CharacterAbilityType abilityType,
        Vector2 origin,
        Vector2 aimDirection,
        Vector2 placement,
        AbilityTierData tierData)
    {
        if (!showInPlayMode && Application.isPlaying) return;

        var snapshot = BuildSnapshot(abilityType, origin, aimDirection, placement, tierData);
        _activeSnapshot = snapshot;
        _gizmoSnapshot = snapshot;
        _displayTimer = displayDuration;
        ApplySnapshotToRenderer(snapshot, 1f);
    }

    public void ShowDash(Vector2 origin, Vector2 direction, float distance, float width)
    {
        if (!showInPlayMode && Application.isPlaying) return;

        var snapshot = new AbilityDebugSnapshot
        {
            abilityType = CharacterAbilityType.Dash,
            origin = origin,
            aimDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up,
            placement = origin,
            range = distance,
            areaWidth = width,
            isCircle = false
        };

        _activeSnapshot = snapshot;
        _gizmoSnapshot = snapshot;
        _displayTimer = displayDuration;
        ApplySnapshotToRenderer(snapshot, 1f);
    }

    private void LateUpdate()
    {
        if (!showInPlayMode || _displayTimer <= 0f)
        {
            HideZoneRenderer();
            return;
        }

        _displayTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(_displayTimer / displayDuration);
        ApplySnapshotToRenderer(_activeSnapshot, alpha);
    }

    private void ApplySnapshotToRenderer(AbilityDebugSnapshot snapshot, float alpha)
    {
        EnsureRenderer();
        if (_zoneRenderer == null || _materialInstance == null) return;

        GetColors(snapshot.abilityType, out Color fill, out Color outline);
        _materialInstance.SetColor(FillColorId, fill);
        _materialInstance.SetColor(OutlineColorId, outline);
        _materialInstance.SetFloat(ShapeId, snapshot.isCircle ? 0f : 1f);
        _materialInstance.SetFloat(AlphaId, alpha);
        _materialInstance.SetFloat(PulseId, (float)snapshot.abilityType * 0.17f);

        PoseZone(snapshot);
        _zoneRenderer.enabled = true;
    }

    private void PoseZone(AbilityDebugSnapshot snapshot)
    {
        if (_zoneRenderer == null) return;

        Transform zoneTransform = _zoneRenderer.transform;
        Vector2 forward = snapshot.aimDirection.sqrMagnitude > 0.0001f
            ? snapshot.aimDirection.normalized
            : Vector2.up;

        if (snapshot.isCircle)
        {
            float diameter = snapshot.range * 2f;
            Vector2 center = snapshot.abilityType is CharacterAbilityType.CoraBarrier or CharacterAbilityType.CoraPool
                ? snapshot.placement
                : snapshot.origin;

            zoneTransform.position = new Vector3(center.x, center.y, zoneTransform.position.z);
            zoneTransform.rotation = Quaternion.identity;
            zoneTransform.localScale = new Vector3(diameter, diameter, 1f);
            return;
        }

        float depth = snapshot.range;
        float width = Mathf.Max(0.2f, snapshot.areaWidth);
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;

        zoneTransform.position = new Vector3(snapshot.origin.x, snapshot.origin.y, zoneTransform.position.z);
        zoneTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        zoneTransform.localScale = new Vector3(width, depth, 1f);
    }

    private void HideZoneRenderer()
    {
        if (_zoneRenderer == null) return;

        _zoneRenderer.enabled = false;
        _zoneRenderer.transform.localPosition = Vector3.zero;
        _zoneRenderer.transform.localRotation = Quaternion.identity;
        _zoneRenderer.transform.localScale = Vector3.one;
    }

    private AbilityDebugSnapshot BuildSnapshot(
        CharacterAbilityType abilityType,
        Vector2 origin,
        Vector2 aimDirection,
        Vector2 placement,
        AbilityTierData tierData)
    {
        return abilityType switch
        {
            CharacterAbilityType.NixPush => new AbilityDebugSnapshot
            {
                abilityType = abilityType,
                origin = origin,
                aimDirection = aimDirection,
                placement = placement,
                range = tierData.range,
                areaWidth = tierData.areaWidth,
                isCircle = true
            },
            CharacterAbilityType.NixCharge => new AbilityDebugSnapshot
            {
                abilityType = abilityType,
                origin = origin,
                aimDirection = aimDirection,
                placement = placement,
                range = tierData.range,
                areaWidth = tierData.areaWidth,
                isCircle = false
            },
            CharacterAbilityType.CoraBarrier or CharacterAbilityType.CoraPool => new AbilityDebugSnapshot
            {
                abilityType = abilityType,
                origin = origin,
                aimDirection = aimDirection,
                placement = placement,
                range = tierData.range,
                areaWidth = tierData.areaWidth,
                isCircle = true
            },
            _ => new AbilityDebugSnapshot
            {
                abilityType = abilityType,
                origin = origin,
                aimDirection = aimDirection,
                placement = placement,
                range = tierData.range,
                areaWidth = tierData.areaWidth,
                isCircle = true
            }
        };
    }

    private void GetColors(CharacterAbilityType abilityType, out Color fill, out Color outline)
    {
        switch (abilityType)
        {
            case CharacterAbilityType.NixPush:
                fill = nixPushFill;
                outline = nixPushOutline;
                break;
            case CharacterAbilityType.NixCharge:
                fill = nixChargeFill;
                outline = nixChargeOutline;
                break;
            case CharacterAbilityType.CoraBarrier:
                fill = coraBarrierFill;
                outline = coraBarrierOutline;
                break;
            case CharacterAbilityType.CoraPool:
                fill = coraPoolFill;
                outline = coraPoolOutline;
                break;
            case CharacterAbilityType.Dash:
                fill = dashFill;
                outline = dashOutline;
                break;
            default:
                fill = new Color(1f, 1f, 1f, 0.25f);
                outline = Color.white;
                break;
        }
    }

    private void EnsureRenderer()
    {
        if (_zoneRenderer != null) return;

        var child = new GameObject("AbilityDebugZone");
        child.transform.SetParent(transform, false);

        _zoneRenderer = child.AddComponent<SpriteRenderer>();
        _zoneRenderer.sprite = CreateUnitSprite();
        _zoneRenderer.sortingOrder = sortingOrder;

        var shader = Shader.Find("MidnightMeow/AbilityZoneFill");
        if (shader == null)
            shader = Shader.Find("MidnightMeow/TelegraphFill");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        _materialInstance = new Material(shader);
        _zoneRenderer.material = _materialInstance;
        _zoneRenderer.enabled = false;
    }

    private static Sprite CreateUnitSprite()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || _gizmoSnapshot.abilityType == CharacterAbilityType.None)
            return;

        DrawSnapshotGizmo(_gizmoSnapshot);
    }

    public void DrawPreviewGizmo(
        CharacterAbilityType abilityType,
        Vector2 origin,
        Vector2 aimDirection,
        AbilityTierData tierData)
    {
        if (!drawDebugGizmos) return;
        DrawSnapshotGizmo(BuildSnapshot(abilityType, origin, aimDirection, origin, tierData));
    }

    private void DrawSnapshotGizmo(AbilityDebugSnapshot snapshot)
    {
        GetColors(snapshot.abilityType, out Color fill, out Color outline);
        Vector2 forward = snapshot.aimDirection.sqrMagnitude > 0.0001f
            ? snapshot.aimDirection.normalized
            : Vector2.up;

        if (snapshot.isCircle)
        {
            Vector2 center = snapshot.abilityType is CharacterAbilityType.CoraBarrier or CharacterAbilityType.CoraPool
                ? snapshot.placement
                : snapshot.origin;
            AbilityDebugGizmoUtility.DrawCircle(center, snapshot.range, fill, outline);
            return;
        }

        if (snapshot.abilityType == CharacterAbilityType.Dash)
        {
            AbilityDebugGizmoUtility.DrawDash(snapshot.origin, forward, snapshot.range, snapshot.areaWidth, fill, outline);
            return;
        }

        AbilityDebugGizmoUtility.DrawOrientedRect(
            snapshot.origin,
            forward,
            snapshot.range,
            snapshot.areaWidth * 0.5f,
            fill,
            outline);
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

    private struct AbilityDebugSnapshot
    {
        public CharacterAbilityType abilityType;
        public Vector2 origin;
        public Vector2 aimDirection;
        public Vector2 placement;
        public float range;
        public float areaWidth;
        public bool isCircle;
    }
}
