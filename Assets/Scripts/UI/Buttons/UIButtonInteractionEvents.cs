///* ----------------------------------------------------------------
// DESCRIÇÃO: Emissor genérico de eventos de interação de UI (legado).
// SFX de botão: UiButtonSfx + UiSfxPlayer. UnityEvents podem permanecer
// ligados a MenuAudioManager (no-op) sem duplicar áudio.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
[DisallowMultipleComponent]
public class UIButtonInteractionEvents : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Interaction Events")]
    [Tooltip("Disparado quando o cursor entra na área do elemento.")]
    public UnityEvent onHover;

    [Tooltip("Disparado quando o elemento é clicado.")]
    public UnityEvent onClick;

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_selectable != null && !_selectable.interactable) return;
        onHover?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_selectable != null && !_selectable.interactable) return;
        onClick?.Invoke();
    }
}
