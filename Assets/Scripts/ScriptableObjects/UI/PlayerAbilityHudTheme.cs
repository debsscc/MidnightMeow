// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Tema visual do HUD de habilidades (ícones, cores e layout). Placeholders são usados quando campos ficam vazios.
// ---------------------------------------------------------------- 

using TMPro;
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
    public Color labelColor = Color.black;
    public Color labelLockedColor = new Color(0f, 0f, 0f, 0.45f);
    public Color passiveCounterColor = Color.white;
    [Tooltip("Fonte TMP da HUD de habilidades (Inknut Antiqua Black).")]
    public TMP_FontAsset hudFont;

    [Header("Layout")]
    public Vector2 anchoredPosition = new Vector2(12f, 10f);
    public float slotSize = 128f;
    public float slotSpacing = 130f;
    [Range(0.05f, 0.35f)]
    [Tooltip("Margem horizontal do overlay de cooldown/lock.")]
    public float overlayInset = 0.18f;
    [Range(0.02f, 0.35f)]
    [Tooltip("Margem vertical do overlay (menor = caixa mais alta).")]
    public float overlayInsetY = 0.07f;
    [Tooltip("Desloca o overlay no X (negativo = esquerda) pra alinhar com a arte.")]
    public float overlayShiftX = -0.10f;
    [Tooltip("Faixa (0–1) da caixa branca do ícone — teclas e contador da passiva.")]
    public float labelBandMinX = -0.04f;
    public float labelBandMaxX = 0.82f;
    public float labelBandMinY = 0.14f;
    public float labelBandMaxY = 0.30f;
    [Tooltip("Faixa do timer de cooldown no topo do ícone.")]
    public float cooldownTimerMinX = -0.03f;
    public float cooldownTimerMaxX = 0.97f;
    public float cooldownTimerMinY = 0.84f;
    public float cooldownTimerMaxY = 1.14f;
    [Range(12, 32)] public int labelFontSize = 17;
    [Range(14, 32)] public int timerFontSize = 22;
}
