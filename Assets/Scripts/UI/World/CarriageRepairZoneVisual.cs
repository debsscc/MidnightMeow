using UnityEngine;

/// <summary>
/// Áreas circulares de conserto da carruagem quebrada.
/// </summary>
[DisallowMultipleComponent]
public class CarriageRepairZoneVisual : MonoBehaviour
{
    private GameObject _zoneRootA;
    private GameObject _zoneRootB;
    private EnemyTelegraphZoneView _viewA;
    private EnemyTelegraphZoneView _viewB;

    private void LateUpdate()
    {
        NetworkCarriage carriage = NetworkCarriage.Instance;
        if (carriage == null || carriage.Config == null || !carriage.RepairActive)
        {
            Hide();
            return;
        }

        EnsureZones(carriage.Config);
        float diameter = carriage.Config.repairZoneRadius * 2f;

        _zoneRootA.SetActive(true);
        _zoneRootA.transform.position = carriage.RepairZoneA;
        _zoneRootA.transform.localScale = new Vector3(diameter, diameter, 1f);
        _viewA.SetFill(carriage.RepairProgress);

        if (carriage.RepairZoneCount > 1)
        {
            _zoneRootB.SetActive(true);
            _zoneRootB.transform.position = carriage.RepairZoneB;
            _zoneRootB.transform.localScale = new Vector3(diameter, diameter, 1f);
            _viewB.SetFill(carriage.RepairProgress);
        }
        else if (_zoneRootB != null)
        {
            _zoneRootB.SetActive(false);
        }
    }

    private void EnsureZones(CarriageConfig config)
    {
        if (_zoneRootA != null)
            return;

        _zoneRootA = CreateZone("RepairZoneA", config);
        _zoneRootB = CreateZone("RepairZoneB", config);
        _viewA = _zoneRootA.GetComponent<EnemyTelegraphZoneView>();
        _viewB = _zoneRootB.GetComponent<EnemyTelegraphZoneView>();
    }

    private static GameObject CreateZone(string name, CarriageConfig config)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 36;

        var view = go.AddComponent<EnemyTelegraphZoneView>();
        var style = ScriptableObject.CreateInstance<EnemyTelegraphVisualStyle>();
        style.backgroundColor = config.repairZoneBackgroundColor;
        style.fillColor = config.repairZoneFillColor;
        style.outlineColor = config.repairZoneOutlineColor;
        style.sortingOrder = 36;
        view.ApplyStyle(style, TelegraphShapeType.Circle, TelegraphFillMode.ExpandFromOrigin);
        return go;
    }

    private void Hide()
    {
        if (_zoneRootA != null)
            _zoneRootA.SetActive(false);
        if (_zoneRootB != null)
            _zoneRootB.SetActive(false);
    }
}
