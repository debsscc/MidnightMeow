// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Prompt world-space "Aperte E para selar" perto de buracos não selados. 
// ---------------------------------------------------------------- 

using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRatHoleSealInteraction))]
public class RatHoleSealPromptUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.85f, 0f);

    private PlayerRatHoleSealInteraction _interaction;
    private Canvas _canvas;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _interaction = GetComponent<PlayerRatHoleSealInteraction>();
        BuildUI();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas.gameObject);
    }

    private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

    private void HandleLocaleChanged(Locale _) => RefreshLabel();

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

        _canvas.transform.SetPositionAndRotation((Vector3)hole.AnchorPosition + offset, Quaternion.identity);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (_label != null)
            _label.text = UiLocalization.GetSealPrompt();
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        // Sem pai: evita herdar scale do jogador e garante sorting estável na frente do buraco.
        _canvas = GameplayUiFonts.CreateWorldInteractionCanvas("RatHoleSealPrompt", out RectTransform rect);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rect, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        GameplayUiFonts.ApplyWorldInteraction(_label);
        RefreshLabel();
    }
}
