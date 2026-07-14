using UnityEngine;

/// <summary>
/// Configuração da passiva por personagem (kill streak + duração + efeitos por herói).
/// </summary>
[CreateAssetMenu(fileName = "PassiveAbilityConfig", menuName = "Abilities/Passive Ability Config")]
public class PassiveAbilityConfig : ScriptableObject
{
    [Tooltip("Abates consecutivos necessários para ativar a passiva.")]
    [Min(1)]
    public int killsRequired = 5;

    [Tooltip("Duração da passiva ativa em segundos.")]
    [Min(0.1f)]
    public float passiveDuration = 5f;

    [Header("Nix — Stun")]
    [Tooltip("Duração do stun aplicado após o knockback do ataque normal com passiva ativa.")]
    [Min(0f)]
    public float stunDuration = 1.25f;

    [Header("Cora — Respingo / Splash")]
    [Tooltip("Quantidade de sub-projéteis teleguiados gerados no impacto com passiva ativa.")]
    [Min(0)]
    public int splashCount = 3;

    [Tooltip("Raio de busca de alvos para os respingos.")]
    [Min(0.1f)]
    public float splashRange = 4f;

    [Tooltip("Fração do dano original aplicada em cada respingo (ex.: 0.5 = 50%).")]
    [Range(0f, 2f)]
    public float splashDamagePercentage = 0.5f;

    [Tooltip("Se true, prioriza inimigos distintos. Se false (ou sem alvos extras), podem ir no mesmo.")]
    public bool prioritizeDifferentEnemies = true;
}
