// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Tema visual do HUD de habilidades (ícones, cores e layout). Placeholders são usados quando campos ficam vazios.
// ---------------------------------------------------------------- 

using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAbilityHudTheme", menuName = "MidnightMeow/UI/Player Ability HUD Theme")]
public class PlayerAbilityHudTheme : ScriptableObject
{
    [Header("Ícones")]
    public Sprite passiveIcon;
    public Sprite dashIcon;
    public Sprite ability1Icon;
    public Sprite ability2Icon;

    [Header("Cores")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.92f);
    public Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.65f);
    public Color passiveFallbackColor = new Color(0.85f, 0.55f, 0.15f, 0.9f);
    public Color dashFallbackColor = new Color(0.35f, 0.75f, 0.95f, 0.9f);
    public Color abilityFallbackColor = new Color(0.75f, 0.55f, 0.2f, 0.9f);

    [Header("Layout")]
    public Vector2 anchoredPosition = new Vector2(24f, 24f);
    public float slotSize = 56f;
    public float slotSpacing = 68f;
}
