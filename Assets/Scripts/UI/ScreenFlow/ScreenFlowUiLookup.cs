using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Busca elementos de UI por nome na hierarquia (fallback quando refs não estão ligadas no Inspector).
/// </summary>
public static class ScreenFlowUiLookup
{
    public static Button FindButton(string objectName, bool includeInactive = true)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == objectName)
                return buttons[i];
        }

        return null;
    }

    public static TMP_Text FindText(string objectName, bool includeInactive = true)
    {
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == objectName)
                return texts[i];
        }

        return null;
    }

    public static TMP_InputField FindInputField(bool includeInactive = true)
    {
        TMP_InputField[] fields = Object.FindObjectsByType<TMP_InputField>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return fields.Length > 0 ? fields[0] : null;
    }

    public static T EnsureComponent<T>(string objectName) where T : Component
    {
        T existing = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(objectName);
        return go.AddComponent<T>();
    }
}
