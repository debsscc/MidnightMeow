///* ----------------------------------------------------------------
// DESCRIÇÃO: Emissor genérico de eventos de interação de UI. 
// Atua como um canal desacoplado para disparar feedback (ex: Áudio).
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
        // Usa Selectable ao invés de Button para garantir escalabilidade (funciona com Toggles, Sliders, etc.)
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Evita disparar som de hover se o botão estiver desabilitado
        if (_selectable != null && !_selectable.interactable) return;

        onHover?.Invoke();
        Debug.Log($"{gameObject.name} was hovered.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Evita disparar som de clique se o botão estiver desabilitado
        if (_selectable != null && !_selectable.interactable) return;

        onClick?.Invoke();
        Debug.Log($"{gameObject.name} was clicked.");
    }
}