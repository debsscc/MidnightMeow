using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Resolve o asset de ações de UI do projeto (preloaded) e aplica no EventSystem.
/// </summary>
public static class ProjectInputActions
{
    private static InputActionAsset _uiActions;

    public static InputActionAsset UiActions
    {
        get
        {
            if (_uiActions == null)
                _uiActions = FindUiActionsAsset();
            return _uiActions;
        }
    }

    public static void ApplyToUiModule(InputSystemUIInputModule module)
    {
        if (module == null)
            return;

        if (UiModuleActionsValid(module))
            return;

        InputActionAsset asset = UiActions;
        if (asset != null)
            module.actionsAsset = asset;

        if (!UiModuleActionsValid(module))
            module.AssignDefaultActions();
    }

    public static bool UiModuleActionsValid(InputSystemUIInputModule module)
    {
        if (module == null)
            return false;

        return module.actionsAsset != null
               && module.move != null && module.move.action != null
               && module.submit != null && module.submit.action != null;
    }

    private static InputActionAsset FindUiActionsAsset()
    {
        InputActionAsset[] assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        for (int i = 0; i < assets.Length; i++)
        {
            InputActionAsset asset = assets[i];
            if (asset != null && asset.name == "InputSystem_Actions")
                return asset;
        }

        return null;
    }
}
