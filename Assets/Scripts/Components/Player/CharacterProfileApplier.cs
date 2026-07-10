using UnityEngine;

/// <summary>
/// Injeta dados de <see cref="CharacterGameplayProfile"/> nos componentes do personagem.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
public class CharacterProfileApplier : MonoBehaviour
{
    [SerializeField] private CharacterGameplayProfile profile;

    public CharacterGameplayProfile Profile => profile;

    private void Awake()
    {
        if (profile == null)
            return;

        ApplyCoreStats();
        ApplyPrimaryAttack();
        ApplyAbilities();
        ApplyAnimation();
        ApplyAudio();
        ApplyAdvanced();
    }

    public void SetProfile(CharacterGameplayProfile newProfile)
    {
        profile = newProfile;
        if (profile == null)
            return;

        ApplyCoreStats();
        ApplyPrimaryAttack();
        ApplyAbilities();
        ApplyAnimation();
        ApplyAudio();
        ApplyAdvanced();
    }

    private void ApplyCoreStats()
    {
        if (profile.coreStats == null)
            return;

        if (TryGetComponent(out PlayerMovement movement))
            movement.ApplyRuntimeStats(profile.coreStats);

        if (TryGetComponent(out PlayerDash dash))
            dash.ApplyRuntimeStats(profile.coreStats);

        if (TryGetComponent(out PlayerAmmo ammo))
            ammo.ApplyRuntimeStats(profile.coreStats);

        if (TryGetComponent(out PlayerAim aim))
            aim.ApplyRuntimeStats(profile.coreStats);

        if (TryGetComponent(out PlayerAdrenaline adrenaline))
            adrenaline.ApplyRuntimeStats(profile.coreStats);

        if (TryGetComponent(out PlayerInitializer initializer))
            initializer.ApplyBaseStats(profile.coreStats);
    }

    private void ApplyPrimaryAttack()
    {
        if (profile.UsesRangedAttack && profile.rangedAttack != null)
        {
            if (TryGetComponent(out PlayerShooting shooting))
                shooting.ApplyRuntimeStats(profile.rangedAttack);

            if (TryGetComponent(out PlayerAim aim))
            {
                aim.ApplyRuntimeStats(profile.coreStats);
                aim.ApplyRangedCombatStats(profile.rangedAttack);
            }
        }

        if (profile.UsesMeleeAttack && profile.meleeAttack != null)
        {
            if (TryGetComponent(out PlayerMeleeCombat melee))
                melee.ApplyRuntimeStats(profile.meleeAttack);

            if (profile.meleeAttack.hitVisual != null && TryGetComponent(out MeleeAttackVisual meleeVisual))
                meleeVisual.Configure(profile.meleeAttack.hitVisual);
        }
    }

    private void ApplyAbilities()
    {
        if (profile.abilitySet == null)
            return;

        if (TryGetComponent(out PlayerAbilityHandler abilityHandler))
            abilityHandler.ApplyAbilitySet(profile.abilitySet);
    }

    private void ApplyAnimation()
    {
        if (profile.animationProfile == null)
            return;

        if (TryGetComponent(out AnimatorProfileBinder binder))
            binder.SetProfile(profile.animationProfile);
    }

    private void ApplyAudio()
    {
        if (profile.audioConfig == null)
            return;

        if (TryGetComponent(out PlayerAudioController audio))
            audio.ApplyConfig(profile.audioConfig);
    }

    private void ApplyAdvanced()
    {
        if (TryGetComponent(out PlayerDash dash) && profile.dashPassThroughLayers.value != 0)
            dash.ApplyPassThroughLayers(profile.dashPassThroughLayers);

        if (profile.dashFailsafeExtraSeconds > 0f && TryGetComponent(out PlayerDash dashFailsafe))
            dashFailsafe.ApplyFailsafeExtraSeconds(profile.dashFailsafeExtraSeconds);

        ApplyEnemyLayersToExecutors();
    }

    private void ApplyEnemyLayersToExecutors()
    {
        if (profile.enemyLayers.value == 0)
            return;

        if (TryGetComponent(out NixChargeAbilityExecutor charge))
            charge.ApplyEnemyLayers(profile.enemyLayers);

        if (TryGetComponent(out NixPushAbilityExecutor push))
            push.ApplyEnemyLayers(profile.enemyLayers);

        if (TryGetComponent(out NetworkPlayerAbilityRelay abilityRelay))
            abilityRelay.ApplyEnemyLayers(profile.enemyLayers);
    }
}
