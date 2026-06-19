using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desenha áreas circulares de selamento ativas (cliente).
/// </summary>
[DisallowMultipleComponent]
public class RatHoleSealZoneVisual : MonoBehaviour
{
    [SerializeField] private RatHoleSealConfig config;

    private readonly Dictionary<ushort, List<GameObject>> _zoneObjects = new Dictionary<ushort, List<GameObject>>();

    private void Awake()
    {
        if (config == null)
            config = Resources.Load<RatHoleSealConfig>("RatHoleSealConfig");
    }

    private void LateUpdate()
    {
        NetworkRatHoleSealManager manager = NetworkRatHoleSealManager.Instance;
        if (manager == null || config == null)
        {
            HideAll();
            return;
        }

        var active = new HashSet<ushort>();
        foreach (RatHoleSealSession session in manager.Sessions)
        {
            if (!session.IsActive)
                continue;

            active.Add(session.HoleId);
            RenderSession(session);
        }

        foreach (var pair in _zoneObjects)
        {
            if (active.Contains(pair.Key))
                continue;

            for (int i = 0; i < pair.Value.Count; i++)
            {
                if (pair.Value[i] != null)
                    pair.Value[i].SetActive(false);
            }
        }
    }

    private void RenderSession(RatHoleSealSession session)
    {
        List<GameObject> zones = GetOrCreateZones(session.HoleId, session.ZoneCount);
        float diameter = config.zoneRadius * 2f;

        zones[0].SetActive(true);
        zones[0].transform.position = session.ZoneA;
        zones[0].transform.localScale = new Vector3(diameter, diameter, 1f);
        zones[0].GetComponent<EnemyTelegraphZoneView>()?.SetFill(session.Progress);

        if (session.ZoneCount > 1 && zones.Count > 1)
        {
            zones[1].SetActive(true);
            zones[1].transform.position = session.ZoneB;
            zones[1].transform.localScale = new Vector3(diameter, diameter, 1f);
            zones[1].GetComponent<EnemyTelegraphZoneView>()?.SetFill(session.Progress);
        }
        else if (zones.Count > 1)
        {
            zones[1].SetActive(false);
        }
    }

    private List<GameObject> GetOrCreateZones(ushort holeId, int zoneCount)
    {
        if (_zoneObjects.TryGetValue(holeId, out List<GameObject> existing))
            return existing;

        var created = new List<GameObject>(2);
        int count = Mathf.Clamp(zoneCount, 1, 2);
        for (int i = 0; i < count; i++)
            created.Add(CreateZone($"SealZone_{holeId}_{i}"));

        _zoneObjects[holeId] = created;
        return created;
    }

    private GameObject CreateZone(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 35;

        var view = go.AddComponent<EnemyTelegraphZoneView>();
        var style = ScriptableObject.CreateInstance<EnemyTelegraphVisualStyle>();
        style.backgroundColor = config.zoneBackgroundColor;
        style.fillColor = config.zoneFillColor;
        style.outlineColor = config.zoneOutlineColor;
        style.sortingOrder = 35;
        view.ApplyStyle(style, TelegraphShapeType.Circle, TelegraphFillMode.ExpandFromOrigin);
        return go;
    }

    private void HideAll()
    {
        foreach (var pair in _zoneObjects)
        {
            for (int i = 0; i < pair.Value.Count; i++)
            {
                if (pair.Value[i] != null)
                    pair.Value[i].SetActive(false);
            }
        }
    }
}
