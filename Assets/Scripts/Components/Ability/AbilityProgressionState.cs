using System;
using UnityEngine;

/// <summary>
/// Estado de desbloqueio e tier das habilidades por fase.
/// </summary>
[Serializable]
public class AbilityProgressionState
{
    [Tooltip("Índice da fase atual (1 = só ataque + passiva).")]
    [Min(1)]
    public int phaseIndex = 1;

    public bool ability1Unlocked;
    public bool ability2Unlocked;

    [Range(1, 3)] public int primaryTier = 1;
    [Range(1, 3)] public int ability1Tier = 1;
    [Range(1, 3)] public int ability2Tier = 1;

    public bool IsSlotUnlocked(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.PrimaryAttack => true,
            AbilitySlot.Dash => true,
            AbilitySlot.Ability1 => true,
            AbilitySlot.Ability2 => true,
            _ => false
        };
    }

    public int GetUnlockWave(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.Ability1 => 2,
            AbilitySlot.Ability2 => 3,
            _ => 1
        };
    }

    public int GetTierForSlot(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.PrimaryAttack => primaryTier,
            AbilitySlot.Ability1 => ability1Tier,
            AbilitySlot.Ability2 => ability2Tier,
            AbilitySlot.Dash => 1,
            _ => 1
        };
    }

    public void SyncPhaseFromWaveIndex(int waveIndex)
    {
        phaseIndex = Mathf.Max(1, waveIndex + 1);
        if (phaseIndex >= 2 && !ability1Unlocked && !ability2Unlocked)
            ability1Unlocked = true;
        if (phaseIndex >= 3 && ability1Unlocked && !ability2Unlocked)
            ability2Unlocked = true;
    }
}
