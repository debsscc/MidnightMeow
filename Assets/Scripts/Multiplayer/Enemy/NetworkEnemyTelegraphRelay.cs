using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Encaminha telegraphs visuais para <see cref="NetworkEnemyController"/> (RPC registrado no prefab).
/// </summary>
public class NetworkEnemyTelegraphRelay : MonoBehaviour
{
    private NetworkEnemyController _controller;

    private void Awake()
    {
        _controller = GetComponent<NetworkEnemyController>();
    }

    public void BroadcastTelegraph(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        Vector2 travelSpawnPosition)
    {
        if (_controller == null)
            _controller = GetComponent<NetworkEnemyController>();

        _controller?.BroadcastTelegraphToClients(
            strike, style, worldPosition, rotationDegrees, travelSpawnPosition);
    }
}

public struct TelegraphClientSnapshot : INetworkSerializable
{
    public byte Shape;
    public byte FillMode;
    public byte Resolution;
    public Vector2 WorldPosition;
    public float RotationDegrees;
    public Vector2 Size;
    public float FillDuration;
    public Vector2 TravelSpawnPosition;
    public float TravelSpeed;
    public byte HasTravelVisual;
    public Color BackgroundColor;
    public Color FillColor;
    public Color OutlineColor;
    public float OutlineWidth;
    public int SortingOrder;

    public static TelegraphClientSnapshot From(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees,
        Vector2 travelSpawnPosition)
    {
        bool hasTravel = strike.resolution == EnemyTelegraphResolution.ProjectileToZone
                         && (strike.travelVisualPrefab != null || strike.projectilePrefab != null
                             || GameplayPrefabCatalog.LoadCached()?.enemyTelegraphTravelPrefab != null);

        float travelSpeed = strike.travelSpeed > 0f
            ? strike.travelSpeed
            : (strike.projectileSpeedOverride > 0f ? strike.projectileSpeedOverride : 12f);

        var snap = new TelegraphClientSnapshot
        {
            Shape = (byte)strike.shape,
            FillMode = (byte)strike.fillMode,
            Resolution = (byte)strike.resolution,
            WorldPosition = worldPosition,
            RotationDegrees = rotationDegrees,
            Size = strike.size,
            FillDuration = strike.fillDuration,
            TravelSpawnPosition = travelSpawnPosition,
            TravelSpeed = travelSpeed,
            HasTravelVisual = hasTravel ? (byte)1 : (byte)0
        };

        if (style != null)
        {
            snap.BackgroundColor = style.backgroundColor;
            snap.FillColor = style.fillColor;
            snap.OutlineColor = style.outlineColor;
            snap.OutlineWidth = style.outlineWidth;
            snap.SortingOrder = style.sortingOrder;
        }
        else
        {
            snap.BackgroundColor = new Color(1f, 0.92f, 0.22f, 0.55f);
            snap.FillColor = new Color(0.9f, 0.12f, 0.08f, 0.85f);
            snap.OutlineColor = new Color(0.95f, 0.15f, 0.1f, 1f);
            snap.OutlineWidth = 0.06f;
            snap.SortingOrder = 50;
        }

        return snap;
    }

    public TelegraphStrikeDefinition ToStrikeDefinition()
    {
        return new TelegraphStrikeDefinition
        {
            shape = (TelegraphShapeType)Shape,
            fillMode = (TelegraphFillMode)FillMode,
            resolution = (EnemyTelegraphResolution)Resolution,
            size = Size,
            fillDuration = FillDuration,
            damage = 0
        };
    }

    public EnemyTelegraphVisualStyle ToVisualStyle()
    {
        var style = ScriptableObject.CreateInstance<EnemyTelegraphVisualStyle>();
        style.backgroundColor = BackgroundColor;
        style.fillColor = FillColor;
        style.outlineColor = OutlineColor;
        style.outlineWidth = OutlineWidth;
        style.sortingOrder = SortingOrder;
        return style;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Shape);
        serializer.SerializeValue(ref FillMode);
        serializer.SerializeValue(ref Resolution);
        serializer.SerializeValue(ref WorldPosition);
        serializer.SerializeValue(ref RotationDegrees);
        serializer.SerializeValue(ref Size);
        serializer.SerializeValue(ref FillDuration);
        serializer.SerializeValue(ref TravelSpawnPosition);
        serializer.SerializeValue(ref TravelSpeed);
        serializer.SerializeValue(ref HasTravelVisual);
        serializer.SerializeValue(ref BackgroundColor);
        serializer.SerializeValue(ref FillColor);
        serializer.SerializeValue(ref OutlineColor);
        serializer.SerializeValue(ref OutlineWidth);
        serializer.SerializeValue(ref SortingOrder);
    }
}
