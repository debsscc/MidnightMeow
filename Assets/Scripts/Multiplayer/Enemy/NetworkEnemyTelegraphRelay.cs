using Unity.Netcode;
using UnityEngine;

/// <summary>Replica telegraphs visuais nos clientes (servidor mantém zona autoritativa).</summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkEnemyTelegraphRelay : NetworkBehaviour
{
    [SerializeField] private EnemyTelegraphZoneFactory clientVisualFactory;

    private void Awake()
    {
        if (clientVisualFactory == null)
            clientVisualFactory = GetComponent<EnemyTelegraphZoneFactory>();
    }

    public void BroadcastTelegraph(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees)
    {
        if (!IsServer) return;

        var snapshot = TelegraphClientSnapshot.From(strike, style, worldPosition, rotationDegrees);
        BroadcastTelegraphClientRpc(snapshot);
    }

    [ClientRpc]
    private void BroadcastTelegraphClientRpc(TelegraphClientSnapshot snapshot)
    {
        if (IsServer) return;

        if (clientVisualFactory == null) return;

        var strike = snapshot.ToStrikeDefinition();
        var style = snapshot.ToVisualStyle();

        clientVisualFactory.Spawn(
            strike,
            style,
            snapshot.WorldPosition,
            snapshot.RotationDegrees,
            gameObject,
            transform,
            visualOnly: true);
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
    public Color BackgroundColor;
    public Color FillColor;
    public Color OutlineColor;
    public float OutlineWidth;
    public int SortingOrder;

    public static TelegraphClientSnapshot From(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPosition,
        float rotationDegrees)
    {
        var snap = new TelegraphClientSnapshot
        {
            Shape = (byte)strike.shape,
            FillMode = (byte)strike.fillMode,
            Resolution = (byte)strike.resolution,
            WorldPosition = worldPosition,
            RotationDegrees = rotationDegrees,
            Size = strike.size,
            FillDuration = strike.fillDuration
        };

        if (style != null)
        {
            snap.BackgroundColor = style.backgroundColor;
            snap.FillColor = style.fillColor;
            snap.OutlineColor = style.outlineColor;
            snap.OutlineWidth = style.outlineWidth;
            snap.SortingOrder = style.sortingOrder;
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
        serializer.SerializeValue(ref BackgroundColor);
        serializer.SerializeValue(ref FillColor);
        serializer.SerializeValue(ref OutlineColor);
        serializer.SerializeValue(ref OutlineWidth);
        serializer.SerializeValue(ref SortingOrder);
    }
}
