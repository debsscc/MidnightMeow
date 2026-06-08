using UnityEngine;

/// <summary>Cria instâncias de zona de telegraph (prefab opcional ou runtime).</summary>
public class EnemyTelegraphZoneFactory : MonoBehaviour
{
    [SerializeField] private GameObject zonePrefab;
    [SerializeField] private Sprite defaultSprite;

    private static Sprite _cachedWhiteSprite;

    public EnemyTelegraphZoneInstance Spawn(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        GameObject instigator,
        Transform attackOrigin,
        bool visualOnly,
        System.Func<GameObject, Vector3, Quaternion, GameObject> projectileSpawnDelegate = null)
    {
        GameObject go = zonePrefab != null
            ? Instantiate(zonePrefab)
            : CreateRuntimeZoneObject();

        return InitializeZone(go, strike, style, worldPosition, rotationDegrees, instigator, attackOrigin, visualOnly, projectileSpawnDelegate);
    }

    /// <summary>
    /// Spawn visual-only sem depender de componente no inimigo (ClientRpc / clientes MP).
    /// </summary>
    public static EnemyTelegraphZoneInstance SpawnClientVisual(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        GameObject instigator,
        Transform attackOrigin)
    {
        GameObject go = new GameObject("EnemyTelegraphZone_Client");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.drawMode = SpriteDrawMode.Simple;
        go.AddComponent<EnemyTelegraphZoneView>();

        return InitializeZone(go, strike, style, worldPosition, rotationDegrees, instigator, attackOrigin, visualOnly: true);
    }

    private static EnemyTelegraphZoneInstance InitializeZone(
        GameObject go,
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        GameObject instigator,
        Transform attackOrigin,
        bool visualOnly,
        System.Func<GameObject, Vector3, Quaternion, GameObject> projectileSpawnDelegate = null)
    {
        var instance = go.GetComponent<EnemyTelegraphZoneInstance>();
        if (instance == null)
            instance = go.AddComponent<EnemyTelegraphZoneInstance>();

        instance.Initialize(strike, style, worldPosition, rotationDegrees, instigator, attackOrigin, visualOnly, projectileSpawnDelegate);
        return instance;
    }

    private GameObject CreateRuntimeZoneObject()
    {
        var go = new GameObject("EnemyTelegraphZone");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = defaultSprite != null ? defaultSprite : GetWhiteSprite();
        sr.drawMode = SpriteDrawMode.Simple;
        go.AddComponent<EnemyTelegraphZoneView>();
        return go;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_cachedWhiteSprite != null) return _cachedWhiteSprite;

        var tex = Texture2D.whiteTexture;
        _cachedWhiteSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            tex.width);
        return _cachedWhiteSprite;
    }
}
