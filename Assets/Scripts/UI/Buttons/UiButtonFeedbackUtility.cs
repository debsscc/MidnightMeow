// ----------------------------------------------------------------
// DESCRIÇÃO: Aplica SFX + juiciness em Buttons/Toggles de uma cena ou hierarquia.
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UiButtonFeedbackUtility
{
    private static readonly Color MenuHighlighted = new Color(0.92f, 0.88f, 0.72f, 1f);
    private static readonly Color MenuPressed = new Color(0.78f, 0.72f, 0.58f, 1f);
    private static readonly Color MenuSelected = new Color(0.88f, 0.82f, 0.65f, 1f);

    public static void ApplyToScene(Scene scene, bool includeInactive = true)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        UiSfxPlayer.EnsureExists();
        UiButtonSfxBootstrap.WireScene(scene);

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            ApplyToHierarchy(roots[i].transform, includeInactive);
    }

    public static void ApplyToHierarchy(Transform root, bool includeInactive = true)
    {
        if (root == null)
            return;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(includeInactive);
        for (int i = 0; i < selectables.Length; i++)
            ApplyToSelectable(selectables[i]);
    }

    public static void ApplyToSelectable(Selectable selectable)
    {
        if (selectable == null)
            return;

        if (selectable is Slider or Scrollbar)
            return;

        GameObject go = selectable.gameObject;
        if (go.GetComponent<UiSfxIgnore>() != null)
            return;

        UiButtonSfxBootstrap.EnsureOn(selectable);

        if (go.GetComponent<Button_Juiceness>() == null)
            go.AddComponent<Button_Juiceness>();

        StrengthenColorTint(selectable);
    }

    private static void StrengthenColorTint(Selectable selectable)
    {
        if (selectable.transition != Selectable.Transition.ColorTint)
            return;

        ColorBlock colors = selectable.colors;
        colors.highlightedColor = MenuHighlighted;
        colors.pressedColor = MenuPressed;
        colors.selectedColor = MenuSelected;
        colors.colorMultiplier = Mathf.Max(colors.colorMultiplier, 1f);
        colors.fadeDuration = Mathf.Min(colors.fadeDuration, 0.08f);
        selectable.colors = colors;
    }
}
