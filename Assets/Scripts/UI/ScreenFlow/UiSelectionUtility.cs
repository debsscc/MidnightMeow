//--------------------------------------------------
// FUNÇÃO: Seleção de UI para navegação por teclado/gamepad (New Input System).
//--------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiSelectionUtility
{
    /// <summary>Define o objeto selecionado no EventSystem global (necessário para seta/D-pad/Submit).</summary>
    public static void Select(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
            return;

        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        if (es.alreadySelecting)
            return;

        if (es.currentSelectedGameObject == target)
            return;

        es.SetSelectedGameObject(target);
    }

    public static void Select(Selectable selectable)
    {
        if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsInteractable())
            return;

        Select(selectable.gameObject);
    }

    /// <summary>Primeiro Selectable interagível sob o root (maior sortingOrder do canvas).</summary>
    public static void SelectFirstUnder(Transform root)
    {
        Selectable best = FindFirstSelectableUnder(root);
        if (best != null)
            Select(best);
    }

    public static Selectable FindFirstSelectableUnder(Transform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
            return null;

        Selectable[] all = root.GetComponentsInChildren<Selectable>(includeInactive: false);
        Selectable best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            Selectable s = all[i];
            if (s == null || !s.isActiveAndEnabled || !s.IsInteractable())
                continue;

            if (s.navigation.mode == Navigation.Mode.None)
                continue;

            Canvas canvas = s.GetComponentInParent<Canvas>();
            int order = 0;
            if (canvas != null)
            {
                Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                order = rootCanvas.sortingOrder;
            }

            // Prefere ordem de hierarquia estável quando sortingOrder empatar.
            if (best == null || order > bestOrder)
            {
                best = s;
                bestOrder = order;
            }
        }

        return best;
    }

    /// <summary>Limpa seleção se o objeto atual estiver inativo ou fora da hierarquia.</summary>
    public static void ClearIfInvalid()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        GameObject current = es.currentSelectedGameObject;
        if (current == null)
            return;

        if (!current.activeInHierarchy)
            es.SetSelectedGameObject(null);
    }

    public static void Clear()
    {
        EventSystem es = EventSystem.current;
        if (es == null || es.alreadySelecting)
            return;

        es.SetSelectedGameObject(null);
    }
}
