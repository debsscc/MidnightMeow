using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mapeia estados do Animator base para clips específicos do personagem/inimigo.
/// </summary>
[Serializable]
public struct AnimatorClipOverrideEntry
{
    [Tooltip("Nome do clip original no controller base (substituído por clip).")]
    public string stateName;

    [Tooltip("Clip que substitui o motion do estado.")]
    public AnimationClip clip;
}

/// <summary>
/// Configuração data-driven de animações. Gera um AnimatorOverrideController em runtime.
/// </summary>
[CreateAssetMenu(fileName = "CharacterAnimationProfile", menuName = "MidnightMeow/Characters/Animation Profile")]
public class CharacterAnimationProfile : ScriptableObject
{
    [Header("Animator")]
    [Tooltip("Controller base compartilhado (ex.: AC_Player, AC_Enemy).")]
    public RuntimeAnimatorController baseController;

    [Tooltip("Substituições de clip por nome de estado.")]
    public AnimatorClipOverrideEntry[] clipOverrides = Array.Empty<AnimatorClipOverrideEntry>();

    [Header("Parâmetros")]
    public string moveSpeedParameter = "MoveSpeed";
    public string attackSpeedParameter = "AttackSpeed";
    public string onShootTrigger = "OnShoot";
    public string onAbility1Trigger = "OnAbility1";
    public string onAbility2Trigger = "OnAbility2";
    public string onDashTrigger = "OnDash";
    public string onDashAttackTrigger = "OnDashAttack";
    public string isDashingParameter = "IsDashing";
    public string isAttackingParameter = "IsAttacking";
    public string onTakeDamageTrigger = "OnDamage";
    public string onDieTrigger = "OnDie";

    [Header("Habilidades ativas")]
    [Tooltip("Clip do estado Ability1 (Q) no Animator.")]
    public AnimationClip ability1Clip;

    [Tooltip("Nome do estado Ability1 no controller base.")]
    public string ability1AnimatorStateName = "Ability1";

    [Tooltip("Clip do estado Ability2 (R) no Animator.")]
    public AnimationClip ability2Clip;

    [Tooltip("Nome do estado Ability2 no controller base.")]
    public string ability2AnimatorStateName = "Ability2";

    [Header("Ataque Ranged")]
    [Tooltip("Clip com Animation Event PerformFire (ex.: Cora_Base_Attack). Fonte única do timing de soltura.")]
    public AnimationClip attackClip;

    [Tooltip("Nome do estado no Animator que reproduz attackClip (ex.: Shooting).")]
    public string attackAnimatorStateName = "Shooting";

    [Tooltip("Estado de ataque melee (ex.: Hitting). Vazio = personagem ranged.")]
    public string meleeAttackAnimatorStateName = "Hitting";

    [Header("Configurações Avançadas")]
    [Tooltip("Duração do clipe de ataque para cálculo de AttackSpeed. Ignorado se attackClip estiver atribuído.")]
    public float attackAnimClipLength = 0.333f;

    [Tooltip("Delay antes de destruir o objeto após morte.")]
    public float deathDestroyDelay = 4f;

    [Header("Morte — apresentação")]
    [Tooltip("Nome do estado no Animator base (ex.: Dying).")]
    public string deathAnimatorStateName = "Dying";

    [Tooltip("Espera mínima após a animação de morte antes de dissolve/game over.")]
    public float postDeathHoldSeconds = 5f;

    [Tooltip("Fallback se o clip de morte ainda não estiver no override.")]
    public float deathClipLengthFallback = 1.5f;

    [Tooltip("Offset de sorting base para sprites.")]
    public int sortingOrderOffset = 5000;

    [Tooltip("Precisão do sorting por eixo Y.")]
    public int sortingPrecision = 100;

    public RuntimeAnimatorController BuildRuntimeController()
    {
        if (baseController == null)
            return null;

        if (clipOverrides == null || clipOverrides.Length == 0)
            return baseController;

        var overrideController = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;
            if (original == null)
                continue;

            AnimationClip replacement = FindOverrideClip(original.name);
            if (replacement != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
        }

        overrideController.ApplyOverrides(overrides);
        return overrideController;
    }

    private AnimationClip FindOverrideClip(string originalClipName)
    {
        for (int i = 0; i < clipOverrides.Length; i++)
        {
            AnimatorClipOverrideEntry entry = clipOverrides[i];
            if (entry.clip == null || string.IsNullOrEmpty(entry.stateName))
                continue;

            if (string.Equals(entry.stateName, originalClipName, StringComparison.OrdinalIgnoreCase))
                return entry.clip;
        }

        return null;
    }

    public int GetParameterHash(string parameterName) =>
        string.IsNullOrEmpty(parameterName) ? 0 : Animator.StringToHash(parameterName);
}
