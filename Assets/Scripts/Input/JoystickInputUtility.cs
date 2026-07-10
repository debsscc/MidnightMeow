//--------------------------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 09/07/2026
// FUNÇÃO: UTILITÁRIO PARA INPUT DE JOYSTICK QUE RESOLVE O JOYSTICK ATIVO MESMO QUANDO Joystick.current AINDA NÃO FOI ATUALIZADO.
//--------------------------------------------------

using UnityEngine.InputSystem;

public static class JoystickInputUtility
{
    public static Joystick Current
    {
        get
        {
            Joystick current = Joystick.current;
            if (current != null)
                return current;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Joystick joystick)
                    return joystick;
            }

            return null;
        }
    }
}
