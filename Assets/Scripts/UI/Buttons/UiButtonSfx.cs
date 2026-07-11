// ----------------------------------------------------------------
// DESCRIÇÃO: Hover (mouse + seleção gamepad) e click em Selectables de UI.
// Toca direto no UiSfxPlayer — sem UnityEvent / MenuAudioManager.
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public sealed class UiButtonSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private bool playSfx = true;

    private Selectable _selectable;
    private Button _button;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleButtonClick);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleButtonClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TryPlayHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Cooldown no UiSfxPlayer evita hover duplicado (PointerEnter + Select no mesmo frame).
        TryPlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Button já cobre click via onClick; aqui cobre Toggle e outros Selectables.
        if (_button != null)
            return;

        TryPlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_button != null)
            return;

        TryPlayClick();
    }

    private void HandleButtonClick()
    {
        TryPlayClick();
    }

    private void TryPlayHover()
    {
        if (!CanPlay())
            return;

        UiSfxPlayer.EnsureExists().PlayHover();
    }

    private void TryPlayClick()
    {
        if (!CanPlay())
            return;

        UiSfxPlayer.EnsureExists().PlayClick();
    }

    private bool CanPlay()
    {
        if (!playSfx || !isActiveAndEnabled)
            return false;
        if (GetComponent<UiSfxIgnore>() != null)
            return false;
        if (_selectable != null && !_selectable.interactable)
            return false;
        return true;
    }
}
