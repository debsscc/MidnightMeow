using UnityEngine;

/// <summary>
/// Aplica tiers de ataque normal (range/dano) conforme progressão do jogador.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAbilityStatScaler : MonoBehaviour
{
    [SerializeField] private PlayerAbilityHandler abilityHandler;
    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private PlayerShooting shooting;

    private MeleeCombatStats _meleeRuntime;
    private float _baseFireRate;
    private float _baseDamageMultiplier = 1f;

    private void Awake()
    {
        if (abilityHandler == null) abilityHandler = GetComponent<PlayerAbilityHandler>();
        if (meleeCombat == null) meleeCombat = GetComponent<PlayerMeleeCombat>();
        if (shooting == null) shooting = GetComponent<PlayerShooting>();

        if (shooting != null)
        {
            _baseFireRate = shooting.BaseFireRate;
            _baseDamageMultiplier = shooting.DamageMultiplier;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnWaveStatusChanged += HandleWaveChanged;
        ApplyCurrentTier();
    }

    private void OnDisable()
    {
        GameEvents.OnWaveStatusChanged -= HandleWaveChanged;
    }

    private void HandleWaveChanged(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
        => ApplyCurrentTier();

    public void ApplyCurrentTier()
    {
        if (abilityHandler == null || abilityHandler.AbilitySet == null) return;

        int tier = abilityHandler.Progression.GetTierForSlot(AbilitySlot.PrimaryAttack);
        AbilityTierData data = abilityHandler.AbilitySet.GetPrimaryAttackTier(tier);

        if (meleeCombat != null && meleeCombat.CombatStats != null)
        {
            if (_meleeRuntime == null)
                _meleeRuntime = Instantiate(meleeCombat.CombatStats);

            _meleeRuntime.attackRange = data.range > 0f ? data.range : _meleeRuntime.attackRange;
            _meleeRuntime.damage = data.damage > 0f ? data.damage : _meleeRuntime.damage;
            meleeCombat.ApplyRuntimeStats(_meleeRuntime);
        }

        if (shooting != null)
        {
            if (data.damage > 0f)
                shooting.SetDamageMultiplier(_baseDamageMultiplier * (data.damage / Mathf.Max(0.01f, abilityHandler.AbilitySet.GetPrimaryAttackTier(1).damage)));
        }
    }
}
