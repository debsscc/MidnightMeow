using UnityEngine;

/// <summary>
/// Círculo de zona de reviver ao redor do jogador inconsciente (shader TelegraphFill).
/// </summary>
[RequireComponent(typeof(NetworkPlayerHealth))]
public class DownedReviveZoneVisual : MonoBehaviour
{
    [SerializeField] private DownedPlayerConfig downedConfig;

    private NetworkPlayerHealth _health;
    private GameObject _zoneRoot;
    private EnemyTelegraphZoneView _zoneView;

    private void Awake()
    {
        _health = GetComponent<NetworkPlayerHealth>();
        if (downedConfig == null)
            downedConfig = _health.DownedConfig;
    }

    private void LateUpdate()
    {
        if (_health == null || downedConfig == null) return;

        bool show = _health.IsSpawned && _health.IsUnconscious && !_health.IsBleedingOut;
        if (!show)
        {
            HideZone();
            return;
        }

        EnsureZone();
        _zoneRoot.SetActive(true);
        float diameter = downedConfig.reviveZoneRadius * 2f;
        _zoneRoot.transform.position = transform.position;
        _zoneRoot.transform.localScale = new Vector3(diameter, diameter, 1f);
        _zoneView.SetFill(_health.ReviveProgress);
    }

    private void EnsureZone()
    {
        if (_zoneRoot != null) return;

        _zoneRoot = new GameObject("DownedReviveZone");
        _zoneRoot.transform.SetParent(transform, false);

        var sr = _zoneRoot.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        sr.sortingOrder = 40;

        _zoneView = _zoneRoot.AddComponent<EnemyTelegraphZoneView>();

        var style = ScriptableObject.CreateInstance<EnemyTelegraphVisualStyle>();
        style.backgroundColor = downedConfig.reviveZoneBackgroundColor;
        style.fillColor = downedConfig.reviveZoneFillColor;
        style.outlineColor = downedConfig.reviveZoneOutlineColor;
        style.outlineWidth = 0.05f;
        style.sortingOrder = 40;

        _zoneView.ApplyStyle(style, TelegraphShapeType.Circle, TelegraphFillMode.ExpandFromOrigin);
        _zoneView.SetFill(0f);
    }

    private void HideZone()
    {
        if (_zoneRoot != null)
            _zoneRoot.SetActive(false);
    }

    private static Sprite _whiteSprite;

    private static Sprite CreateWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        return _whiteSprite;
    }

    private void OnDisable()
    {
        if (_zoneRoot != null)
            Destroy(_zoneRoot);
        _zoneRoot = null;
        _zoneView = null;
    }
}
