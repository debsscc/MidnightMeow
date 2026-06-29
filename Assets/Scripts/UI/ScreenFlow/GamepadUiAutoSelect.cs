// ----------------------------------------------------------------
// CRIADO POR: Debs Carvalho
// DATA: 2026-06-28
// DESCRIÇÃO: Torna as telas navegáveis por gamepad/teclado: quando nada está selecionado e o jogador usa 
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class GamepadUiAutoSelect : MonoBehaviour
{
    private void Update()
    {
        // Verifica se o EventSystem está ativo e se a tela deve ser navegada automaticamente.
        EventSystem es = EventSystem.current;
        //Se o EventSystem não está ativo ou a tela não deve ser navegada automaticamente, retorna.
        if (es == null || !ShouldAutoSelect())
            return;

        GameObject current = es.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy)
        {
            //Se o objeto atual é interagível, retorna.
            Selectable sel = current.GetComponent<Selectable>();
            if (sel != null && sel.IsInteractable())
                return;
        }

        if (!NavigationRequested())
            return;

        GameObject target = FindFirstSelectable();
        if (target != null)
            //Seleciona o primeiro botão interagível da tela ativa.
            es.SetSelectedGameObject(target);
    }

    private static bool ShouldAutoSelect()
    {
        // Se a fase atual não é Gameplay, retorna true para navegar automaticamente.
        if (ScreenFlowStateMachine.CurrentPhase != ScreenFlowPhase.Gameplay)
            return true;

        return GameFlowOrchestrator.Instance != null && GameFlowOrchestrator.Instance.IsPauseActive;
    }

    private static bool NavigationRequested()
    {
        // Verifica se o jogador está usando o gamepad.
        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            // Verifica se o jogador está usando o analógico esquerdo.
            Vector2 stick = gp.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.5f || Mathf.Abs(stick.y) > 0.5f)
                return true;
            // Verifica se o jogador está usando o d-pad.
            if (gp.dpad.ReadValue().sqrMagnitude > 0.1f)
                return true;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            // Verifica se o jogador está usando as setas do teclado.
            if (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                kb.tabKey.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private static GameObject FindFirstSelectable()
    {
        Selectable[] all = Selectable.allSelectablesArray;
        Selectable best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            // Verifica se o objeto é interagível.
            Selectable s = all[i];
            if (s == null || !s.isActiveAndEnabled || !s.IsInteractable())
                continue;
            // Verifica se o objeto tem navegação.
            if (s.navigation.mode == Navigation.Mode.None)
                continue;
            // Verifica se o objeto tem canvas.

            Canvas canvas = s.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled)
                continue;
            // Verifica se o objeto tem canvas raiz.

            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            int order = root.sortingOrder;
            if (best == null || order > bestOrder)
            {
                // Se o objeto tem canvas raiz, atualiza o melhor objeto.
                bestOrder = order;
                best = s;
            }
        }

        return best != null ? best.gameObject : null;
    }
}
