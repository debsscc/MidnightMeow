using TMPro;
using UnityEngine;

/// <summary>
/// Prompt world-space "Aperte F para selar" perto de buracos não selados.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRatHoleSealInteraction))]
public class RatHoleSealPromptUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

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
        bool show = hole != null;
        SetVisible(show);
        if (!show || _canvas == null)
            return;

        Vector3 anchor = hole.AnchorPosition;
        _canvas.transform.position = anchor + offset;
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

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.6f, 0.5f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(2.6f, 0.5f);
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.text = "Aperte F para selar";
        _label.fontSize = 2.2f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(0.85f, 0.95f, 1f, 1f);
    }
}
