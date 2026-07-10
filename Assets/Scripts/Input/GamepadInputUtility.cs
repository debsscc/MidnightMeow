//--------------------------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 09/07/2026
// FUNÇÃO: UTILITÁRIO PARA INPUT DE GAMEPAD QUE RESOLVE O GAMEPAD ATIVO MESMO QUANDO Gamepad.current AINDA NÃO FOI ATUALIZADO.

using UnityEngine.InputSystem;

public static class GamepadInputUtility
{
    public static Gamepad Current
    {
        get
        {
            Gamepad current = Gamepad.current;
            if (current != null)
                return current;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Gamepad gamepad)
                    return gamepad;
            }

            return null;
        }
    }
}
