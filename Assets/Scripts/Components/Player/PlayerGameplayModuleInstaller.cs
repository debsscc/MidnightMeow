using UnityEngine;

/// <summary>
/// Garante componentes de gameplay/revive/imunidade no prefab sem editar YAML manualmente.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PlayerGameplayModuleInstaller : MonoBehaviour
{
    [SerializeField] private bool installDamageImmunity = true;
    [SerializeField] private bool installDownedUI = true;
    [SerializeField] private bool installReviveZoneVisual = true;
    [SerializeField] private bool installRevivePromptUI = true;
    [SerializeField] private bool installMeleeDebugVisual = true;
    [SerializeField] private bool installAbilityDebugVisual = true;

    private void Awake()
    {
        if (installDamageImmunity && GetComponent<PlayerDamageImmunity>() == null)
            gameObject.AddComponent<PlayerDamageImmunity>();

        if (installDownedUI && GetComponent<DownedPlayerWorldUI>() == null)
            gameObject.AddComponent<DownedPlayerWorldUI>();

        if (installReviveZoneVisual && GetComponent<DownedReviveZoneVisual>() == null)
            gameObject.AddComponent<DownedReviveZoneVisual>();

        if (installRevivePromptUI && GetComponent<NetworkPlayerRevive>() != null &&
            GetComponent<RevivePromptWorldUI>() == null)
            gameObject.AddComponent<RevivePromptWorldUI>();

        if (installMeleeDebugVisual && GetComponent<PlayerMeleeCombat>() != null &&
            GetComponent<MeleeAttackDebugVisual>() == null)
            gameObject.AddComponent<MeleeAttackDebugVisual>();

        if (installAbilityDebugVisual && GetComponent<PlayerAbilityHandler>() != null &&
            GetComponent<AbilityDebugVisualHost>() == null)
            gameObject.AddComponent<AbilityDebugVisualHost>();
    }
}
