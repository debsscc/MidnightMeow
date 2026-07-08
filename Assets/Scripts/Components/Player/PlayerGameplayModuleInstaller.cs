using UnityEngine;

/// <summary>
/// Garante componentes de gameplay/revive/imunidade no prefab sem editar YAML manualmente.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PlayerGameplayModuleInstaller : MonoBehaviour
{
    [SerializeField] private bool installDamageImmunity = true;
    [SerializeField] private bool installDownedUI = true;
    [SerializeField] private bool installCarriageRepairInteraction = true;
    [SerializeField] private bool installMeleeHitVisual = true;
    [SerializeField] private bool installAbilityDebugVisual = false;
    [SerializeField] private bool installRatHoleSealInteraction = true;

    private void Awake()
    {
        if (installDamageImmunity && GetComponent<PlayerDamageImmunity>() == null)
            gameObject.AddComponent<PlayerDamageImmunity>();

        if (installRatHoleSealInteraction && GetComponent<PlayerRatHoleSealInteraction>() == null)
            gameObject.AddComponent<PlayerRatHoleSealInteraction>();

        if (installRatHoleSealInteraction && GetComponent<RatHoleSealPromptUI>() == null)
            gameObject.AddComponent<RatHoleSealPromptUI>();

        if (GetComponent<NetworkPlayerRevive>() != null && GetComponent<PlayerDownedReviveInteraction>() == null)
            gameObject.AddComponent<PlayerDownedReviveInteraction>();

        if (installCarriageRepairInteraction && GetComponent<PlayerCarriageRepairInteraction>() == null)
            gameObject.AddComponent<PlayerCarriageRepairInteraction>();

        if (installDownedUI && GetComponent<DownedPlayerWorldUI>() == null)
            gameObject.AddComponent<DownedPlayerWorldUI>();

        // RevivePromptWorldUI obsoleto — UI consolidada em DownedPlayerWorldUI no jogador caído.

        if (installMeleeHitVisual && GetComponent<PlayerMeleeCombat>() != null &&
            GetComponent<MeleeAttackVisual>() == null)
            gameObject.AddComponent<MeleeAttackVisual>();

        if (GetComponent<PlayerMeleeCombat>() != null && GetComponent<PlayerMeleeHitFeedback>() == null)
            gameObject.AddComponent<PlayerMeleeHitFeedback>();

        if (installAbilityDebugVisual && ShouldInstallAbilityDebugVisual() &&
            GetComponent<PlayerAbilityHandler>() != null &&
            GetComponent<AbilityDebugVisualHost>() == null)
            gameObject.AddComponent<AbilityDebugVisualHost>();

        if (GetComponent<PlayerFacingController>() == null)
            gameObject.AddComponent<PlayerFacingController>();
    }

    private static bool ShouldInstallAbilityDebugVisual()
    {
#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
    }
}
