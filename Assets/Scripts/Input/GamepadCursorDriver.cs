// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: Move o cursor do mouse com o analógico direito do controle.
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class GamepadCursorDriver : MonoBehaviour
{
    [SerializeField] private float cursorSpeed = 960f;
    [SerializeField] private float deadzone = 0.15f;

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (!TryReadAimStick(out Vector2 aim))
            return;

        if (aim.sqrMagnitude < deadzone * deadzone)
            return;

        Vector2 delta = aim * (cursorSpeed * Time.unscaledDeltaTime);
        Vector2 current = mouse.position.ReadValue();
        Vector2 next = new Vector2(
            Mathf.Clamp(current.x + delta.x, 0f, Screen.width - 1f),
            Mathf.Clamp(current.y + delta.y, 0f, Screen.height - 1f));

        mouse.WarpCursorPosition(next);
    }

    private static bool TryReadAimStick(out Vector2 aim)
    {
        aim = Vector2.zero;

        if (GenericControllerInput.TryReadAim(out aim))
            return true;

        Gamepad gamepad = GamepadInputUtility.Current;
        if (gamepad == null)
            return false;

        aim = gamepad.rightStick.ReadValue();
        return true;
    }
}
