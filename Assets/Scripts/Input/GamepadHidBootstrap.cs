//--------------------------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 09/07/2026
// FUNÇÃO: BOOTSTRAP QUE PROMODE CONTROLES HID MAL CLASSIFICADOS PARA LAYOUTS GAMEPAD DO INPUT SYSTEM.
//--------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;

public static class GamepadHidBootstrap
{
    private static readonly IReadOnlyList<HidPromotionRule> PromotionRules = BuildPromotionRules();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterMatchers()
    {
        for (int i = 0; i < PromotionRules.Count; i++)
        {
            HidPromotionRule rule = PromotionRules[i];
            if (rule.LayoutType == null)
                continue;

            InputSystem.RegisterLayoutMatcher(rule.LayoutType.Name, rule.ToMatcher());
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InputSystem.onDeviceChange -= HandleDeviceChange;
        InputSystem.onDeviceChange += HandleDeviceChange;

        foreach (InputDevice device in InputSystem.devices)
            ProcessDevice(device, InputDeviceChange.Added);
    }

    private static void HandleDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
        {
            GenericControllerInput.UnregisterDevice(device);
            return;
        }

        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            ProcessDevice(device, change);
    }

    private static void ProcessDevice(InputDevice device, InputDeviceChange change)
    {
        if (device == null)
            return;

        // Mouse/Keyboard/Pen nunca são controles — tratá-los como HID quebra mira e ataque.
        if (IsPointerOrKeyboardDevice(device))
            return;

        HidDeviceIdentity identity = HidDeviceIdentity.FromDescription(device.description);
        LogDeviceDetected(device, identity, change);

        if (device is Gamepad)
        {
            LogAlreadyGamepad(device);
            return;
        }

        if (device is Joystick)
        {
            Debug.Log($"[GamepadHidBootstrap] {device.displayName} reconhecido como Joystick.");
            return;
        }

        if (IsXInputInterface(device.description))
        {
            Debug.Log($"[GamepadHidBootstrap] Xbox/XInput: {device.displayName}");
            return;
        }

        if (ShouldUseGenericHidFallback(device))
        {
            GenericControllerInput.RegisterHidCandidate(device);
            LogGenericFallback(device, identity);
            return;
        }

        for (int i = 0; i < PromotionRules.Count; i++)
        {
            HidPromotionRule rule = PromotionRules[i];
            if (!rule.Matches(device.description))
                continue;

            if (TryPromoteDevice(device, rule))
                return;
        }

        GenericControllerInput.RegisterHidCandidate(device);
        LogGenericFallback(device, identity);
    }

    private static bool IsPointerOrKeyboardDevice(InputDevice device)
    {
        return device is Mouse
            || device is Keyboard
            || device is Pen
            || device is Touchscreen
            || device is Pointer;
    }

    private static bool ShouldUseGenericHidFallback(InputDevice device)
    {
        if (device is Gamepad || device is Joystick || IsPointerOrKeyboardDevice(device))
            return false;

        if (!string.Equals(device.description.interfaceName, "HID", StringComparison.OrdinalIgnoreCase))
            return false;

        string layout = device.layout ?? string.Empty;
        if (layout.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0
            || layout.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0
            || layout.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0
            || layout.IndexOf("SwitchPro", StringComparison.OrdinalIgnoreCase) >= 0
            || layout.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        return true;
    }

    private static bool TryPromoteDevice(InputDevice device, HidPromotionRule rule)
    {
        try
        {
            InputDeviceDescription description = device.description;
            string name = device.name;
            InputSystem.RemoveDevice(device);
            InputDevice promoted = InputSystem.AddDevice(description);

            if (promoted == null)
            {
                Debug.LogWarning($"[GamepadHidBootstrap] Promoção falhou para {rule.Label} ({name}).");
                return false;
            }

            if (promoted is Gamepad || promoted is Joystick)
            {
                Debug.Log($"[GamepadHidBootstrap] {rule.Label}: promovido para {promoted.layout} ({promoted.displayName}).");
                return true;
            }

            GenericControllerInput.RegisterHidCandidate(promoted);
            Debug.LogWarning(
                $"[GamepadHidBootstrap] {rule.Label}: layout {promoted.layout} não virou Gamepad — usando fallback genérico.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GamepadHidBootstrap] Erro ao promover {rule.Label}: {ex.Message}");
            return false;
        }
    }

    private static void LogDeviceDetected(InputDevice device, HidDeviceIdentity identity, InputDeviceChange change)
    {
        Debug.Log(
            $"[GamepadHidBootstrap] Device {change}: name={device.name}, layout={device.layout}, " +
            $"displayName={device.displayName}, {identity}");
    }

    private static void LogAlreadyGamepad(InputDevice device)
    {
        string family = "Gamepad genérico";
        if (device is DualShockGamepad)
            family = "Sony DualShock/DualSense";
        else if (device is SwitchProControllerHID)
            family = "Nintendo Switch Pro";
        else if (device is XInputController)
            family = "Xbox (XInput)";

        Debug.Log($"[GamepadHidBootstrap] {device.displayName} já é Gamepad ({device.layout}, {family}).");
    }

    private static void LogGenericFallback(InputDevice device, HidDeviceIdentity identity)
    {
        Debug.LogWarning(
            "[GamepadHidBootstrap] Controle genérico/HID — leitura via GenericControllerInput.\n" +
            $"  device={device.name}, layout={device.layout}\n" +
            $"  {identity}\n" +
            "  Dica PC: cabo USB, Steam Input ou DS4Windows (modo Xbox) melhoram compatibilidade.");
    }

    private static bool IsXInputInterface(InputDeviceDescription description)
    {
        return string.Equals(description.interfaceName, "XInput", StringComparison.OrdinalIgnoreCase);
    }

    private static List<HidPromotionRule> BuildPromotionRules()
    {
        const int NintendoVendorId = 0x057E;

        return new List<HidPromotionRule>
        {
            new HidPromotionRule
            {
                Label = "Nintendo Switch Pro Controller",
                LayoutType = typeof(SwitchProControllerHID),
                VendorId = NintendoVendorId,
                ProductId = 0x2009
            },
            new HidPromotionRule
            {
                Label = "Nintendo Switch Pro (nome)",
                LayoutType = typeof(SwitchProControllerHID),
                ManufacturerContains = "Nintendo",
                ProductContains = "Pro Controller"
            }
        };
    }
}

public readonly struct HidDeviceIdentity
{
    public string InterfaceName { get; }
    public string Manufacturer { get; }
    public string Product { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public bool HasVendorProduct { get; }

    public HidDeviceIdentity(
        string interfaceName,
        string manufacturer,
        string product,
        int vendorId,
        int productId,
        bool hasVendorProduct)
    {
        InterfaceName = interfaceName;
        Manufacturer = manufacturer;
        Product = product;
        VendorId = vendorId;
        ProductId = productId;
        HasVendorProduct = hasVendorProduct;
    }

    public static HidDeviceIdentity FromDescription(InputDeviceDescription description)
    {
        bool hasIds = HidDeviceIdentityParser.TryGetVendorProduct(
            description.capabilities, out int vendorId, out int productId);

        return new HidDeviceIdentity(
            description.interfaceName ?? string.Empty,
            description.manufacturer ?? string.Empty,
            description.product ?? string.Empty,
            vendorId,
            productId,
            hasIds);
    }

    public override string ToString()
    {
        return HasVendorProduct
            ? $"interface={InterfaceName}, manufacturer={Manufacturer}, product={Product}, vendorId={VendorId} (0x{VendorId:X4}), productId={ProductId} (0x{ProductId:X4})"
            : $"interface={InterfaceName}, manufacturer={Manufacturer}, product={Product}";
    }
}

internal static class HidDeviceIdentityParser
{
    public static bool TryGetVendorProduct(string capabilities, out int vendorId, out int productId)
    {
        vendorId = 0;
        productId = 0;

        if (string.IsNullOrEmpty(capabilities))
            return false;

        return TryReadIntField(capabilities, "vendorId", out vendorId)
               && TryReadIntField(capabilities, "productId", out productId);
    }

    private static bool TryReadIntField(string json, string field, out int value)
    {
        value = 0;
        string token = "\"" + field + "\":";
        int index = json.IndexOf(token, StringComparison.Ordinal);
        if (index < 0)
            return false;

        index += token.Length;
        int end = index;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            end++;

        return end > index && int.TryParse(json.Substring(index, end - index), out value);
    }
}

internal sealed class HidPromotionRule
{
    public string Label;
    public Type LayoutType;
    public int? VendorId;
    public int? ProductId;
    public string ManufacturerContains;
    public string ProductEquals;
    public string ProductContains;
    public string RequiredInterface = "HID";

    public bool Matches(InputDeviceDescription description)
    {
        HidDeviceIdentity identity = HidDeviceIdentity.FromDescription(description);

        if (!string.IsNullOrEmpty(RequiredInterface)
            && !string.Equals(identity.InterfaceName, RequiredInterface, StringComparison.OrdinalIgnoreCase))
            return false;

        if (VendorId.HasValue && (!identity.HasVendorProduct || identity.VendorId != VendorId.Value))
            return false;

        if (ProductId.HasValue && (!identity.HasVendorProduct || identity.ProductId != ProductId.Value))
            return false;

        if (!string.IsNullOrEmpty(ManufacturerContains)
            && identity.Manufacturer.IndexOf(ManufacturerContains, StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        if (!string.IsNullOrEmpty(ProductEquals)
            && !string.Equals(identity.Product, ProductEquals, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(ProductContains)
            && identity.Product.IndexOf(ProductContains, StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        return true;
    }

    public InputDeviceMatcher ToMatcher()
    {
        InputDeviceMatcher matcher = new InputDeviceMatcher();

        if (!string.IsNullOrEmpty(RequiredInterface))
            matcher = matcher.WithInterface(RequiredInterface);

        if (VendorId.HasValue)
            matcher = matcher.WithCapability("vendorId", VendorId.Value);

        if (ProductId.HasValue)
            matcher = matcher.WithCapability("productId", ProductId.Value);

        if (!string.IsNullOrEmpty(ManufacturerContains))
            matcher = matcher.WithManufacturerContains(ManufacturerContains);

        if (!string.IsNullOrEmpty(ProductEquals))
            matcher = matcher.WithProduct(ProductEquals);

        return matcher;
    }
}
