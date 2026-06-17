using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica sprites/cores de <see cref="ScreenVisualTheme"/> em painéis e botões.
/// </summary>
public static class ScreenThemeApplier
{
    public static void ApplyPanel(Image panel, ScreenVisualTheme.ScreenSection section)
    {
        if (panel == null || section == null)
            return;

        if (section.background != null)
        {
            panel.sprite = section.background;
            panel.color = Color.white;
        }
        else
        {
            panel.color = section.panelColor;
        }
    }

    public static void ApplyButton(Button button, Sprite sprite, Color fallbackColor)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = fallbackColor;
        }
    }
}
