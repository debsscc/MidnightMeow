using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de progresso e texto de selamento em world-space sobre cada buraco.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RatHoleSpawnPoint))]
public class RatHoleSealStatusUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private Vector2 panelSize = new Vector2(3.4f, 0.55f);

    private RatHoleSpawnPoint _hole;
    private Canvas _canvas;
    private Image _fill;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _hole = GetComponent<RatHoleSpawnPoint>();
        BuildUI();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (_hole == null)
            return;

        NetworkRatHoleSealManager manager = NetworkRatHoleSealManager.Instance;
        if (manager == null || !manager.IsSpawned)
        {
            if (_hole.IsSealed)
                ShowSealed();
            else
                SetVisible(false);
            return;
        }

        if (!manager.TryGetSession(_hole.HoleId, out RatHoleSealSession session))
        {
            if (_hole.IsSealed)
                ShowSealed();
            else
                SetVisible(false);
            return;
        }

        if (session.IsSealed || _hole.IsSealed)
        {
            ShowSealed();
            return;
        }

        if (!session.IsActive)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        _canvas.transform.position = (Vector3)_hole.AnchorPosition + offset;
        if (_fill != null)
            _fill.fillAmount = session.Progress;
        if (_label != null)
            _label.text = $"Fique na Área para selar — {Mathf.RoundToInt(session.Progress * 100f)}%";
    }

    private void ShowSealed()
    {
        SetVisible(true);
        _canvas.transform.position = (Vector3)_hole.AnchorPosition + offset;
        if (_fill != null)
            _fill.fillAmount = 1f;
        if (_label != null)
            _label.text = "Área selada";
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        var root = new GameObject("RatHoleSealStatus");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 120;

        RectTransform panelRect = root.GetComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        Stretch(bgRect);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        Stretch(fillRect, 2f);
        _fill = fillGo.AddComponent<Image>();
        _fill.color = new Color(0.25f, 0.75f, 0.45f, 0.95f);
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        Stretch(labelRect);
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 1.25f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = Color.white;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
