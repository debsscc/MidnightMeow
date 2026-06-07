using UnityEngine;

/// <summary>
/// Dados de um contrato/missão exibido na tela de Preparação.
/// </summary>
[CreateAssetMenu(fileName = "Contract_", menuName = "MidnightMeow/Screen Flow/Contract Definition")]
public class ContractDefinition : ScriptableObject
{
    [Tooltip("Nome exibido no cartão.")]
    public string displayName = "Contrato";

    [TextArea(2, 5)]
    public string description = "Descrição da fase.";

    [Tooltip("Nível de dificuldade (1-5).")]
    [Range(1, 5)] public int difficulty = 1;

    [Tooltip("Recompensa em magículas ao completar.")]
    [Min(0)] public int magiculaReward = 1;

    [Tooltip("Cena de gameplay associada.")]
    public string gameplaySceneName = "Fase-1";
}
