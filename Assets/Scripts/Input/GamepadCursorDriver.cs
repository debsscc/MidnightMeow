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
    [SerializeField] private float deadzone = 0.25f;

    private void Update()
    {
        // Só gamepad real. HID genérico + Mouse.current=(0,0) teleportava o cursor
        // pro canto e quebrava ataque/flip (mesmo sintoma pós-refactor de joystick).
        if (!TryReadGamepadAimStick(out Vector2 aim))
            return;

        if (aim.sqrMagnitude < deadzone * deadzone)
            return;

        // Mouse físico ativo: não interferir.
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
            return;

        if (Mouse.current == null)
            return;

        if (!PlayerPointerInput.TryGetScreenPosition(out Vector2 current))
            return; // Nunca warp a partir de (0,0) — prende cursor e quebra ataque/flip.

        Vector2 delta = aim * (cursorSpeed * Time.unscaledDeltaTime);
        Vector2 next = new Vector2(
            Mathf.Clamp(current.x + delta.x, 0f, Screen.width - 1f),
            Mathf.Clamp(current.y + delta.y, 0f, Screen.height - 1f));

        Mouse.current.WarpCursorPosition(next);
    }

    private static bool TryReadGamepadAimStick(out Vector2 aim)
    {
        aim = Vector2.zero;
        Gamepad gamepad = GamepadInputUtility.Current;
        if (gamepad == null)
            return false;

        aim = gamepad.rightStick.ReadValue();
        return true;
    }
}
