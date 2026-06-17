using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicadores na borda da tela apontando para inimigos fora do viewport.
/// </summary>
[DisallowMultipleComponent]
public class OffscreenEnemyIndicator : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private int maxIndicators = 6;
    [SerializeField] private float edgePadding = 28f;
    [SerializeField] private Color indicatorColor = new Color(0.95f, 0.25f, 0.2f, 0.9f);
    [SerializeField] private float indicatorSize = 18f;

    private RectTransform _root;
    private readonly List<RectTransform> _indicators = new List<RectTransform>();

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _root = GetComponent<RectTransform>();
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        EnsureIndicators();
    }

    public static void EnsureOnCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.GetComponentInChildren<OffscreenEnemyIndicator>(true) != null)
            return;

        GameObject go = new GameObject("OffscreenEnemyIndicator", typeof(RectTransform), typeof(OffscreenEnemyIndicator));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        EnsureIndicators();
        HideAll();

        NetworkEnemyController[] enemies = FindObjectsByType<NetworkEnemyController>(FindObjectsSortMode.None);
        int shown = 0;

        for (int i = 0; i < enemies.Length && shown < maxIndicators; i++)
        {
            NetworkEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDeadOnNetwork)
                continue;

            Vector3 viewport = targetCamera.WorldToViewportPoint(enemy.transform.position);
            if (viewport.z < 0f)
                viewport = new Vector3(1f - viewport.x, 1f - viewport.y, viewport.z);

            bool onScreen = viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.05f && viewport.y < 0.95f;
            if (onScreen)
                continue;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, enemy.transform.position);
            if (!TryGetEdgePoint(screenPoint, out Vector2 edgePoint))
                continue;

            RectTransform indicator = _indicators[shown];
            indicator.gameObject.SetActive(true);
            indicator.anchoredPosition = edgePoint;

            Vector2 direction = ((Vector2)screenPoint - edgePoint).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            indicator.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            shown++;
        }
    }

    private bool TryGetEdgePoint(Vector2 screenPoint, out Vector2 edgePoint)
    {
        edgePoint = Vector2.zero;
        if (_root == null)
            return false;

        Rect rect = _root.rect;
        Vector2 center = rect.center;
        Vector2 dir = screenPoint - center;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.up;

        float maxX = rect.width * 0.5f - edgePadding;
        float maxY = rect.height * 0.5f - edgePadding;
        float scale = Mathf.Min(Mathf.Abs(maxX / dir.x), Mathf.Abs(maxY / dir.y));
        edgePoint = center + dir.normalized * Mathf.Min(dir.magnitude, Mathf.Min(maxX, maxY) * scale);
        return true;
    }

    private void EnsureIndicators()
    {
        while (_indicators.Count < maxIndicators)
        {
            GameObject go = new GameObject($"Indicator_{_indicators.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(indicatorSize, indicatorSize);
            Image image = go.GetComponent<Image>();
            image.color = indicatorColor;
            LoadingProgressUtility.ApplySolidSprite(image);
            go.SetActive(false);
            _indicators.Add(rt);
        }
    }

    private void HideAll()
    {
        for (int i = 0; i < _indicators.Count; i++)
            _indicators[i].gameObject.SetActive(false);
    }
}
