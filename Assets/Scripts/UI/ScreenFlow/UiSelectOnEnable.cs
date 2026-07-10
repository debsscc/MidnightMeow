//--------------------------------------------------
// FUNÇÃO: Ao ativar o painel, seleciona um botão padrão para teclado/gamepad.
//--------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiSelectOnEnable : MonoBehaviour
{
    [Tooltip("Selectable preferido. Se vazio, usa o primeiro interagível sob este objeto.")]
    [SerializeField] private Selectable preferredSelectable;

    [Tooltip("Atraso em frames para garantir que o painel já está ativo na hierarquia.")]
    [SerializeField] private bool selectNextFrame = true;

    private void OnEnable()
    {
        if (selectNextFrame)
            StartCoroutine(SelectEndOfFrame());
        else
            ApplySelection();
    }

    private System.Collections.IEnumerator SelectEndOfFrame()
    {
        yield return null;
        ApplySelection();
    }

    private void ApplySelection()
    {
        if (preferredSelectable != null && preferredSelectable.isActiveAndEnabled && preferredSelectable.IsInteractable())
        {
            UiSelectionUtility.Select(preferredSelectable);
            return;
        }

        UiSelectionUtility.SelectFirstUnder(transform);
    }
}
