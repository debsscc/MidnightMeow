//--------------------------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 09/07/2026
// FUNÇÃO: UTILITÁRIO PARA INPUT DE CONTROLES GENÉRICOS QUE RESOLVE O CONTROLE ATIVO MESMO QUANDO Gamepad.current AINDA NÃO FOI ATUALIZADO.
//--------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class GenericControllerInput
{
    private const float MoveDeadzone = 0.15f;
    private const float AimDeadzone = 0.2f;

    private static readonly List<InputDevice> HidCandidates = new List<InputDevice>(4);

    public static bool HasHidFallbackDevice => HidCandidates.Count > 0;

    public static void RegisterHidCandidate(InputDevice device)
    {
        if (device == null || device is Gamepad || device is Joystick)
            return;

        if (HidCandidates.Contains(device))
            return;

        HidCandidates.Add(device);
        Debug.Log($"[GenericControllerInput] HID candidato registrado: {device.name} ({device.layout})");
    }

    public static void UnregisterDevice(InputDevice device)
    {
        if (device == null)
            return;

        HidCandidates.Remove(device);
    }

    public static InputDevice ResolveActiveDevice()
    {
        Gamepad gamepad = GamepadInputUtility.Current;
        if (gamepad != null)
            return gamepad;

        Joystick joystick = JoystickInputUtility.Current;
        if (joystick != null)
            return joystick;

        for (int i = 0; i < HidCandidates.Count; i++)
        {
            InputDevice candidate = HidCandidates[i];
            if (candidate != null && candidate.enabled)
                return candidate;
        }

        return null;
    }

    public static bool TryReadMove(out Vector2 move)
    {
        move = Vector2.zero;
        InputDevice device = ResolveActiveDevice();
        if (device == null)
            return false;

        if (!TryReadStick(device, preferPrimary: true, out move))
            return false;

        if (move.magnitude < MoveDeadzone)
        {
            move = Vector2.zero;
            return true;
        }

        return true;
    }

    public static bool TryReadAim(out Vector2 aim)
    {
        aim = Vector2.zero;
        InputDevice device = ResolveActiveDevice();
        if (device == null)
            return false;

        if (!TryReadSecondaryStick(device, out aim))
            return false;

        if (aim.magnitude < AimDeadzone)
            aim = Vector2.zero;

        return true;
    }

    public static bool ReadFireHeld()
    {
        InputDevice device = ResolveActiveDevice();
        return ReadFireHeldForDevice(device);
    }

    private static bool _prevFireHeld;
    private static bool _prevInteractHeld;

    public static bool WasFirePressedThisFrame()
    {
        bool now = ReadFireHeld();
        bool pressed = now && !_prevFireHeld;
        _prevFireHeld = now;
        return pressed;
    }

    public static bool WasFireReleasedThisFrame()
    {
        bool now = ReadFireHeld();
        bool released = !now && _prevFireHeld;
        _prevFireHeld = now;
        return released;
    }

    public static bool WasDashPressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.rightShoulder.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 4) || WasButtonPressedThisFrame(device, 5);
    }

    public static bool WasAbility1PressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.buttonWest.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 2) || WasButtonPressedThisFrame(device, 3);
    }

    public static bool WasAbility2PressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.buttonEast.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 1);
    }

    public static bool WasFrenzyPressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.buttonNorth.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 6);
    }

    public static bool WasInteractPressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        bool pressed = false;
        if (device is Gamepad gamepad)
            pressed = gamepad.buttonSouth.wasPressedThisFrame;
        else if (device != null)
            pressed = WasButtonPressedThisFrame(device, 0);

        if (device != null)
        {
            bool now = device is Gamepad gp ? gp.buttonSouth.isPressed : ReadButton(device, 0);
            _prevInteractHeld = now;
        }

        return pressed;
    }

    public static bool WasInteractReleasedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        bool now = false;
        if (device is Gamepad gamepad)
            now = gamepad.buttonSouth.isPressed;
        else if (device != null)
            now = ReadButton(device, 0);

        bool released = !now && _prevInteractHeld;
        _prevInteractHeld = now;
        return released;
    }

    public static bool WasSubmitPressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.buttonSouth.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 0);
    }

    public static bool WasCancelPressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.buttonEast.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 1);
    }

    public static bool WasPausePressedThisFrame()
    {
        InputDevice device = ResolveActiveDevice();
        if (device is Gamepad gamepad)
            return gamepad.startButton.wasPressedThisFrame;

        return WasButtonPressedThisFrame(device, 7) || WasButtonPressedThisFrame(device, 9);
    }

    public static bool TryReadNavigateStick(out Vector2 navigate)
    {
        navigate = Vector2.zero;
        if (!TryReadMove(out navigate))
            return false;

        if (navigate.sqrMagnitude < MoveDeadzone * MoveDeadzone)
        {
            navigate = Vector2.zero;
            return false;
        }

        return true;
    }

    private static bool ReadFireHeldForDevice(InputDevice device)
    {
        if (device == null)
            return false;

        if (device is Gamepad gamepad)
            return gamepad.rightTrigger.ReadValue() > 0.35f || gamepad.buttonSouth.isPressed;

        if (ReadAxis(device, "trigger") > 0.35f)
            return true;

        return ReadButton(device, 0) || ReadButton(device, 1);
    }

    private static TControl TryGetChildControl<TControl>(InputDevice device, string path)
        where TControl : InputControl
    {
        if (device == null || string.IsNullOrEmpty(path))
            return null;

        try
        {
            return device.GetChildControl<TControl>(path);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryReadStick(InputDevice device, bool preferPrimary, out Vector2 value)
    {
        value = Vector2.zero;
        if (device == null)
            return false;

        if (device is Gamepad gamepad)
        {
            value = preferPrimary ? gamepad.leftStick.ReadValue() : gamepad.rightStick.ReadValue();
            return true;
        }

        if (device is Joystick joystick)
        {
            value = joystick.stick.ReadValue();
            return true;
        }

        StickControl stick = TryGetChildControl<StickControl>(device, "leftStick")
                             ?? TryGetChildControl<StickControl>(device, "stick")
                             ?? TryGetChildControl<StickControl>(device, "rightStick");
        if (stick != null)
        {
            value = stick.ReadValue();
            return true;
        }

        Vector2Control v2 = TryGetChildControl<Vector2Control>(device, "stick")
                          ?? TryGetChildControl<Vector2Control>(device, "leftStick");
        if (v2 != null)
        {
            value = v2.ReadValue();
            return true;
        }

        AxisControl x = TryGetChildControl<AxisControl>(device, "x")
                      ?? TryGetChildControl<AxisControl>(device, "stick/x")
                      ?? TryGetChildControl<AxisControl>(device, "leftStick/x");
        AxisControl y = TryGetChildControl<AxisControl>(device, "y")
                      ?? TryGetChildControl<AxisControl>(device, "stick/y")
                      ?? TryGetChildControl<AxisControl>(device, "leftStick/y");
        if (x != null && y != null)
        {
            value = new Vector2(x.ReadValue(), y.ReadValue());
            return true;
        }

        ReadPrimaryTwoAxes(device, out value);
        return value.sqrMagnitude > 0.0001f;
    }

    private static bool TryReadSecondaryStick(InputDevice device, out Vector2 value)
    {
        value = Vector2.zero;
        if (device == null)
            return false;

        if (device is Gamepad gamepad)
        {
            value = gamepad.rightStick.ReadValue();
            return true;
        }

        StickControl stick = TryGetChildControl<StickControl>(device, "rightStick");
        if (stick != null)
        {
            value = stick.ReadValue();
            return true;
        }

        if (TryReadAxisPair(device, axisStartIndex: 2, out value))
            return true;

        // Clones com um só stick: sem analógico de mira separado.
        return false;
    }

    private static bool TryReadAxisPair(InputDevice device, int axisStartIndex, out Vector2 value)
    {
        value = Vector2.zero;
        int axisIndex = 0;
        float? x = null;

        foreach (InputControl control in device.allControls)
        {
            AxisControl axis = control as AxisControl;
            if (axis == null || axis.synthetic)
                continue;

            if (axisIndex == axisStartIndex)
                x = axis.ReadValue();
            else if (axisIndex == axisStartIndex + 1)
            {
                value = new Vector2(x ?? 0f, axis.ReadValue());
                return true;
            }

            axisIndex++;
        }

        return false;
    }

    private static void ReadPrimaryTwoAxes(InputDevice device, out Vector2 value)
    {
        value = Vector2.zero;
        int axisIndex = 0;
        foreach (InputControl control in device.allControls)
        {
            AxisControl axis = control as AxisControl;
            if (axis == null || axis.synthetic)
                continue;

            if (axisIndex == 0)
                value.x = axis.ReadValue();
            else if (axisIndex == 1)
            {
                value.y = axis.ReadValue();
                return;
            }

            axisIndex++;
        }
    }

    private static float ReadAxis(InputDevice device, string name)
    {
        if (device == null)
            return 0f;

        AxisControl axis = TryGetChildControl<AxisControl>(device, name);
        return axis != null ? axis.ReadValue() : 0f;
    }

    private static bool ReadButton(InputDevice device, int index)
    {
        if (device == null)
            return false;

        ButtonControl button = TryGetChildControl<ButtonControl>(device, $"button{index}");
        return button != null && button.isPressed;
    }

    private static bool WasButtonPressedThisFrame(InputDevice device, int index)
    {
        if (device == null)
            return false;

        ButtonControl button = TryGetChildControl<ButtonControl>(device, $"button{index}");
        return button != null && button.wasPressedThisFrame;
    }
}
