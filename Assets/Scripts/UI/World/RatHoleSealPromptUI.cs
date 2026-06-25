using TMPro;
using UnityEngine;

/// <summary>
/// Prompt world-space "Aperte E para selar" perto de buracos não selados.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRatHoleSealInteraction))]
public class RatHoleSealPromptUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.1f, 0f);

    private PlayerRatHoleSealInteraction _interaction;
    private Canvas _canvas;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _interaction = GetComponent<PlayerRatHoleSealInteraction>();
        BuildUI();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        RatHoleSpawnPoint hole = _interaction != null ? _interaction.CurrentTargetHole : null;
        bool show = hole != null && !hole.IsSealed;
        if (show && NetworkRatHoleSealManager.Instance != null &&
            NetworkRatHoleSealManager.Instance.TryGetSession(hole.HoleId, out RatHoleSealSession session) &&
            (session.IsActive || session.IsSealed))
        {
            show = false;
        }

        SetVisible(show);
        if (!show || _canvas == null)
            return;

        _canvas.transform.position = (Vector3)hole.AnchorPosition + offset;
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        var root = new GameObject("RatHoleSealPrompt");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 115;

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4.8f, 0.22f);
        rect.localScale = Vector3.one;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.text = "Aperte E para selar";
        _label.fontSize = 1.65f;
        _label.enableAutoSizing = false;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.overflowMode = TextOverflowModes.Overflow;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(0.85f, 0.95f, 1f, 1f);
    }
}
