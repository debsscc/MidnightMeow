//--------------------------------------------------
// FUNÇÃO: Garante Highlighted/Selected visíveis nos Selectables (ColorTint).
//--------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiSelectableFocusVisual : MonoBehaviour
{
    private static readonly Color UnityDefaultMuted = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);

    [Tooltip("Usado quando Highlighted/Selected estão iguais ao Normal (padrão Unity).")]
    [SerializeField] private Color fallbackFocusColor = new Color(0.56078434f, 0.22352943f, 0.42352945f, 0.9490196f);

    [SerializeField] private bool includeInactive = true;

    private void Awake() => Apply();

    private void OnEnable() => Apply();

    public void Apply()
    {
        // MainMenuController fica num GO sem filhos — busca Selectables da cena ativa.
        Selectable[] selectables = Object.FindObjectsByType<Selectable>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        UnityEngine.SceneManagement.Scene scene = gameObject.scene;
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null || selectable.gameObject.scene != scene)
                continue;

            ApplyTo(selectable);
        }
    }

    private void ApplyTo(Selectable selectable)
    {
        if (selectable == null || selectable.transition != Selectable.Transition.ColorTint)
            return;

        ColorBlock colors = selectable.colors;
        Color normal = colors.normalColor;
        Color highlighted = colors.highlightedColor;
        Color selected = colors.selectedColor;

        bool highlightedWeak = IsNearlySame(highlighted, normal) || IsNearlySame(highlighted, UnityDefaultMuted);
        bool selectedWeak = IsNearlySame(selected, normal) || IsNearlySame(selected, UnityDefaultMuted);

        if (highlightedWeak)
            highlighted = fallbackFocusColor;

        // Selected deve parecer hover — senão o foco de gamepad some e o mouse no botão focado também.
        if (selectedWeak || IsNearlySame(selected, UnityDefaultMuted))
            selected = highlighted;

        colors.highlightedColor = highlighted;
        colors.selectedColor = selected;
        selectable.colors = colors;
    }

    private static bool IsNearlySame(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.04f
               && Mathf.Abs(a.g - b.g) < 0.04f
               && Mathf.Abs(a.b - b.b) < 0.04f
               && Mathf.Abs(a.a - b.a) < 0.04f;
    }
}
