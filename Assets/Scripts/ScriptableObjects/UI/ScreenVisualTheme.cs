using UnityEngine;

/// <summary>
/// Tema visual reutilizável para telas e HUD (sprites, cores e botões).
/// Controllers aplicam via <see cref="ScreenThemeApplier"/>.
/// </summary>
[CreateAssetMenu(fileName = "ScreenVisualTheme", menuName = "MidnightMeow/UI/Screen Visual Theme")]
public class ScreenVisualTheme : ScriptableObject
{
    [System.Serializable]
    public class ScreenSection
    {
        public Sprite background;
        public Sprite primaryButton;
        public Sprite secondaryButton;
        public Color panelColor = new Color(0.06f, 0.06f, 0.08f, 0.96f);
        public Color primaryTextColor = Color.white;
    }

    public ScreenSection mainMenu;
    public ScreenSection lobby;
    public ScreenSection preparation;
    public ScreenSection characters;
    public ScreenSection gameplayHud;

    [Header("Gameplay HUD")]
    public Sprite feedbackButtonSprite;
    public Color feedbackButtonColor = new Color(0.2f, 0.55f, 0.95f, 0.95f);
    public Vector2 feedbackButtonSize = new Vector2(280f, 72f);

    [Header("Ability HUD")]
    public PlayerAbilityHudTheme abilityHudTheme;
}
