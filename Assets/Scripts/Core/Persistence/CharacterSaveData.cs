using System;

/// <summary>
/// Progressão persistida de um personagem jogável (tiers de habilidade).
/// </summary>
[Serializable]
public class CharacterSaveData
{
    public LobbyCharacterType characterType = LobbyCharacterType.Default;
    public int primaryTier = 1;
    public int ability1Tier = 1;
    public int ability2Tier = 1;

    public int GetTierForSlot(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.PrimaryAttack => primaryTier,
            AbilitySlot.Ability1 => ability1Tier,
            AbilitySlot.Ability2 => ability2Tier,
            _ => 1
        };
    }

    public void SetTierForSlot(AbilitySlot slot, int tier)
    {
        int clamped = UnityEngine.Mathf.Clamp(tier, 1, 3);
        switch (slot)
        {
            case AbilitySlot.PrimaryAttack:
                primaryTier = clamped;
                break;
            case AbilitySlot.Ability1:
                ability1Tier = clamped;
                break;
            case AbilitySlot.Ability2:
                ability2Tier = clamped;
                break;
        }
    }
}
