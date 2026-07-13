//--------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 12/07/2026
// FUNÇÃO: Leitura estável de cursor/clique (Input System + Input Manager legado).
//--------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Com Active Input Handling = Both, o Editor costuma reportar posição/clique
/// só no Input Manager legado enquanto Mouse.current fica em (0,0) / solto.
/// </summary>
public static class PlayerPointerInput
{
    public static bool TryGetScreenPosition(out Vector2 screenPosition)
    {
        Vector2 legacy = (Vector2)Input.mousePosition;
        Vector2 system = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        if (system.sqrMagnitude < 1f && legacy.sqrMagnitude >= 1f)
        {
            screenPosition = legacy;
            return true;
        }

        if (system.sqrMagnitude >= 1f)
        {
            screenPosition = system;
            return true;
        }

        if (Pen.current != null)
        {
            screenPosition = Pen.current.position.ReadValue();
            if (screenPosition.sqrMagnitude >= 1f)
                return true;
        }

        if (Pointer.current != null)
        {
            screenPosition = Pointer.current.position.ReadValue();
            if (screenPosition.sqrMagnitude >= 1f)
                return true;
        }

        // (0,0) não é cursor válido — evita mira/flip presos no canto inferior esquerdo.
        screenPosition = default;
        return false;
    }

    public static bool IsFireHeld()
    {
        // Legado primeiro: com Input System Only isso é no-op; com Both funciona no Editor.
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            return true;

        if (Mouse.current != null
            && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
            return true;

        if (Pointer.current != null && Pointer.current.press.isPressed)
            return true;

        if (Pen.current != null && Pen.current.tip.isPressed)
            return true;

        return false;
    }
}
