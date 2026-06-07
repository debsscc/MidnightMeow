using UnityEngine;

/// <summary>
/// Configuração da passiva por personagem (kill streak + duração).
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

    [Header("Nix — Cleave")]
    [Tooltip("Máximo de inimigos atingidos pelo ataque normal com passiva ativa.")]
    [Min(1)]
    public int cleaveMaxTargets = 3;

    [Header("Cora — Ricochete")]
    [Tooltip("Bounces extras nos projéteis do ataque normal com passiva ativa.")]
    [Min(0)]
    public int bonusBounces = 2;
}
