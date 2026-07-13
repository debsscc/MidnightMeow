using UnityEngine;

/// <summary>
/// Conjunto de habilidades e passiva de um personagem jogável.
/// </summary>
[CreateAssetMenu(fileName = "CharacterAbilitySet", menuName = "Abilities/Character Ability Set")]
public class CharacterAbilitySet : ScriptableObject
{
    [Tooltip("Identificador do personagem (Nix / Cora).")]
    public string characterId;

    [Tooltip("Habilidade Q (Ability1).")]
    public CharacterAbilityDefinition ability1;

    [Tooltip("Habilidade R (Ability2).")]
    public CharacterAbilityDefinition ability2;

    [Tooltip("Configuração da passiva.")]
    public PassiveAbilityConfig passive;

    [Header("HUD de habilidades (canto inferior esquerdo)")]
    [Tooltip("Ícone da passiva na barra de habilidades.")]
    public Sprite passiveHudIcon;
    [Tooltip("Ícone do dash na barra de habilidades.")]
    public Sprite dashHudIcon;
    [Tooltip("Ícone da habilidade Q (Ability1).")]
    public Sprite ability1HudIcon;
    [Tooltip("Ícone da habilidade R (Ability2).")]
    public Sprite ability2HudIcon;

    [Header("Ataque Normal (escala com primaryTier)")]
    public AbilityTierData primaryAttackTier1;
    public AbilityTierData primaryAttackTier2;
    public AbilityTierData primaryAttackTier3;

    public AbilityTierData GetPrimaryAttackTier(int tier)
    {
        return tier switch
        {
            2 => primaryAttackTier2,
            3 => primaryAttackTier3,
            _ => primaryAttackTier1
        };
    }
}
