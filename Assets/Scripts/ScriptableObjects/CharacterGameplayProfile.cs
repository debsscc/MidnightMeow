using UnityEngine;

public enum CharacterPrimaryAttackMode
{
    None = 0,
    Ranged = 1,
    Melee = 2
}

/// <summary>
/// Perfil unificado de gameplay por personagem jogável.
/// Centraliza movimento, dash, ataque principal, habilidades e animações.
/// </summary>
[CreateAssetMenu(fileName = "CharacterGameplayProfile", menuName = "MidnightMeow/Characters/Gameplay Profile")]
public class CharacterGameplayProfile : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Personagem";

    [Header("Movimento e Vitalidade")]
    [Tooltip("Vida, movimento, dash, adrenalina e munição.")]
    public PlayerStats coreStats;

    [Header("Ataque Principal")]
    public CharacterPrimaryAttackMode primaryAttackMode = CharacterPrimaryAttackMode.Ranged;

    [Tooltip("Usado quando o modo é Ranged (ex.: Cora).")]
    public RangedCombatStats rangedAttack;

    [Tooltip("Usado quando o modo é Melee (ex.: Nixie).")]
    public MeleeCombatStats meleeAttack;

    [Header("Habilidades")]
    public CharacterAbilitySet abilitySet;

    [Header("Animações")]
    public CharacterAnimationProfile animationProfile;

    [Header("Configurações Avançadas")]
    [Tooltip("Camadas usadas por executores de habilidade (ex.: inimigos).")]
    public LayerMask enemyLayers;

    [Tooltip("Camadas ignoradas durante o dash.")]
    public LayerMask dashPassThroughLayers;

    [Tooltip("Tempo extra de failsafe do dash.")]
    public float dashFailsafeExtraSeconds = 0.35f;

    public bool UsesRangedAttack => primaryAttackMode == CharacterPrimaryAttackMode.Ranged;
    public bool UsesMeleeAttack => primaryAttackMode == CharacterPrimaryAttackMode.Melee;
}
