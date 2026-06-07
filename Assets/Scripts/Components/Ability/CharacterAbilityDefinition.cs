using UnityEngine;

/// <summary>
/// Definição data-driven de uma habilidade com três tiers de escalonamento.
/// </summary>
[CreateAssetMenu(fileName = "CharacterAbility", menuName = "Abilities/Character Ability Definition")]
public class CharacterAbilityDefinition : ScriptableObject
{
    [Tooltip("Tipo usado para roteamento e sincronização de rede.")]
    public CharacterAbilityType abilityType;

    [Tooltip("Nome exibido (provisório / WIP).")]
    public string displayName;

    [Tooltip("Tier 1 — valores iniciais.")]
    public AbilityTierData tier1;

    [Tooltip("Tier 2 — valores intermediários.")]
    public AbilityTierData tier2;

    [Tooltip("Tier 3 — valores máximos.")]
    public AbilityTierData tier3;

    [Tooltip("Tempo de execução que bloqueia outras ações (windup + active).")]
    public float executionLockDuration = 0.35f;

    public AbilityTierData GetTierData(int tier)
    {
        return tier switch
        {
            1 => tier1,
            2 => tier2,
            3 => tier3,
            _ => tier1
        };
    }
}
