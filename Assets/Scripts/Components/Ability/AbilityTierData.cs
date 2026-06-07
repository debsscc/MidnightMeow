using System;
using UnityEngine;

/// <summary>
/// Valores de balanceamento por tier (nível 1–3) de uma habilidade.
/// </summary>
[Serializable]
public struct AbilityTierData
{
    [Tooltip("Alcance ou raio principal da habilidade.")]
    public float range;

    [Tooltip("Dano aplicado (quando aplicável).")]
    public float damage;

    [Tooltip("Multiplicador de lentidão (0.5 = 50% da velocidade). Usado em Empurrão.")]
    [Range(0.05f, 1f)]
    public float slowMultiplier;

    [Tooltip("Duração do slow em segundos.")]
    public float slowDuration;

    [Tooltip("Duração do stun em segundos.")]
    public float stunDuration;

    [Tooltip("Força/distância de knockback.")]
    public float knockbackDistance;

    [Tooltip("Duração do knockback em segundos.")]
    public float knockbackDuration;

    [Tooltip("Cooldown da habilidade em segundos.")]
    public float cooldown;

    [Tooltip("Duração do efeito persistente (barreira, poça) em segundos.")]
    public float effectDuration;

    [Tooltip("Largura do retângulo (Investida da Nix) ou raio da poça.")]
    public float areaWidth;

    [Tooltip("DPS da poça (Cora R).")]
    public float damagePerSecond;

    public static AbilityTierData Lerp(AbilityTierData a, AbilityTierData b, float t)
    {
        return new AbilityTierData
        {
            range = Mathf.Lerp(a.range, b.range, t),
            damage = Mathf.Lerp(a.damage, b.damage, t),
            slowMultiplier = Mathf.Lerp(a.slowMultiplier, b.slowMultiplier, t),
            slowDuration = Mathf.Lerp(a.slowDuration, b.slowDuration, t),
            stunDuration = Mathf.Lerp(a.stunDuration, b.stunDuration, t),
            knockbackDistance = Mathf.Lerp(a.knockbackDistance, b.knockbackDistance, t),
            knockbackDuration = Mathf.Lerp(a.knockbackDuration, b.knockbackDuration, t),
            cooldown = Mathf.Lerp(a.cooldown, b.cooldown, t),
            effectDuration = Mathf.Lerp(a.effectDuration, b.effectDuration, t),
            areaWidth = Mathf.Lerp(a.areaWidth, b.areaWidth, t),
            damagePerSecond = Mathf.Lerp(a.damagePerSecond, b.damagePerSecond, t)
        };
    }
}
