using UnityEngine;

/// <summary>
/// Efeito de onda do ataque melee (Nixie) em Play Mode via shader. Gizmos desativados por padrão.
/// </summary>
[RequireComponent(typeof(PlayerMeleeCombat))]
public class MeleeAttackVisual : MonoBehaviour
{
    [SerializeField] private MeleeHitVisualConfig visualConfig;
    [SerializeField] private bool showInPlayMode = true;

    private PlayerMeleeCombat _melee;
    private PlayerPassiveHandler _passiveHandler;
    private SpriteRenderer _zoneRenderer;
    private Material _materialInstance;
    private float _displayTimer;
    private float _waveTimer;
    private Vector2 _lastOrigin;
    private Vector2 _lastForward;
    private bool _passiveActive;

    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int WaveEdgeColorId = Shader.PropertyToID("_WaveEdgeColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int WaveProgressId = Shader.PropertyToID("_WaveProgress");
    private static readonly int WaveEdgeWidthId = Shader.PropertyToID("_WaveEdgeWidth");
    private static readonly int NearHalfWidthId = Shader.PropertyToID("_NearHalfWidth");
    private static readonly int FarHalfWidthId = Shader.PropertyToID("_FarHalfWidth");

    private void Awake()
    {
        _melee = GetComponent<PlayerMeleeCombat>();
        _passiveHandler = GetComponent<PlayerPassiveHandler>();

        if (_passiveHandler != null)
            _passiveHandler.OnPassiveStateChanged += HandlePassiveStateChanged;
    }

    private void OnDestroy()
    {
        if (_passiveHandler != null)
            _passiveHandler.OnPassiveStateChanged -= HandlePassiveStateChanged;

        if (_materialInstance != null)
            Destroy(_materialInstance);
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

        HideZoneRenderer();
    }

    public void Configure(MeleeHitVisualConfig config)
    {
        if (config != null)
            visualConfig = config;
    }

    private void HandlePassiveStateChanged(bool active) => _passiveActive = active;

    private void HandleAttackPerformed(Vector2 origin, Vector2 forward, MeleeCombatStats stats)
    {
        if (!showInPlayMode || visualConfig == null)
            return;

        _lastOrigin = origin;
        _lastForward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        _displayTimer = visualConfig.displayDuration;
        _waveTimer = 0f;
        RefreshRenderer(1f, 0f);
    }

    private void LateUpdate()
    {
        if (!showInPlayMode || visualConfig == null || _displayTimer <= 0f)
        {
            HideZoneRenderer();
            return;
        }

        float duration = Mathf.Max(0.05f, visualConfig.displayDuration);
        _displayTimer -= Time.deltaTime;
        _waveTimer += Time.deltaTime * visualConfig.waveSpeed;

        float alpha = Mathf.Clamp01(_displayTimer / duration);
        float wave = Mathf.Clamp01(_waveTimer / duration);
        RefreshRenderer(alpha, wave);
    }

    private void RefreshRenderer(float alpha, float waveProgress)
    {
        if (!TryGetSwingShape(out Vector2 origin, out Vector2 forward, out float attackRange, out float nearHalfWidth, out float farHalfWidth))
        {
            HideZoneRenderer();
            return;
        }

        EnsureRenderer();
        ApplyColors(_passiveActive || (_passiveHandler != null && _passiveHandler.IsPassiveActive));

        float rangeMultiplier = visualConfig.rangeVisualMultiplier;
        _materialInstance.SetFloat(NearHalfWidthId, nearHalfWidth / Mathf.Max(attackRange * rangeMultiplier, 0.01f));
        _materialInstance.SetFloat(FarHalfWidthId, farHalfWidth / Mathf.Max(attackRange * rangeMultiplier, 0.01f));
        _materialInstance.SetFloat(WaveEdgeWidthId, visualConfig.waveEdgeWidth);
        _materialInstance.SetFloat(WaveProgressId, waveProgress);
        _materialInstance.SetFloat(AlphaId, alpha);

        PoseZone(origin, forward, attackRange * rangeMultiplier, nearHalfWidth, farHalfWidth);
        _zoneRenderer.enabled = true;
    }

    private void ApplyColors(bool passive)
    {
        if (_materialInstance == null)
            return;

        _materialInstance.SetColor(FillColorId, passive ? visualConfig.passiveFillColor : visualConfig.fillColor);
        _materialInstance.SetColor(WaveEdgeColorId, passive ? visualConfig.passiveWaveEdgeColor : visualConfig.waveEdgeColor);
        _materialInstance.SetColor(OutlineColorId, passive ? visualConfig.passiveOutlineColor : visualConfig.outlineColor);
    }

    private void PoseZone(Vector2 origin, Vector2 forward, float depth, float nearHalfWidth, float farHalfWidth)
    {
        if (_zoneRenderer == null)
            return;

        Transform zoneTransform = _zoneRenderer.transform;
        if (zoneTransform.parent != null)
            zoneTransform.SetParent(null, true);

        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
        Vector2 center = origin + forward * (depth * 0.5f);
        float maxHalfWidth = Mathf.Max(nearHalfWidth, farHalfWidth) * 2f;

        zoneTransform.position = new Vector3(center.x, center.y, transform.position.z);
        zoneTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        zoneTransform.localScale = new Vector3(maxHalfWidth, depth, 1f);
    }

    private void HideZoneRenderer()
    {
        if (_zoneRenderer == null)
            return;

        _zoneRenderer.enabled = false;
        _zoneRenderer.transform.SetParent(transform, false);
        _zoneRenderer.transform.localPosition = Vector3.zero;
        _zoneRenderer.transform.localRotation = Quaternion.identity;
        _zoneRenderer.transform.localScale = Vector3.one;
    }

    private void EnsureRenderer()
    {
        if (_zoneRenderer != null)
            return;

        var child = new GameObject("MeleeHitWave");
        child.transform.SetParent(transform, false);

        _zoneRenderer = child.AddComponent<SpriteRenderer>();
        _zoneRenderer.sprite = CreateUnitSprite();
        _zoneRenderer.sortingOrder = visualConfig != null ? visualConfig.sortingOrder : 48;

        _materialInstance = CombatVisualMaterials.CreateMeleeHitWaveInstance();
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

        MeleeCombatStats stats = _melee.CombatStats;
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

    private void OnDrawGizmosSelected()
    {
        if (visualConfig == null || !visualConfig.drawDebugGizmos)
            return;

        if (_melee == null)
            _melee = GetComponent<PlayerMeleeCombat>();

        if (!TryGetSwingShape(out Vector2 origin, out Vector2 forward, out float attackRange, out float nearHalfWidth, out float farHalfWidth))
            return;

        float z = transform.position.z;
        var corners = MeleeHitUtility.GetTrapezoidWorldCorners(
            origin, forward, attackRange, nearHalfWidth, farHalfWidth, z);

        Gizmos.color = visualConfig.fillColor;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }
}
