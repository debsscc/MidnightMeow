using UnityEngine;

/// <summary>
/// Executor de uma habilidade específica acionado pelo <see cref="PlayerAbilityHandler"/>.
/// </summary>
public interface IAbilityExecutor
{
    CharacterAbilityType AbilityType { get; }

    /// <summary>
    /// Executa a habilidade no tier informado. Retorna duração de bloqueio de ações.
    /// </summary>
    float Execute(AbilityTierData tierData, AbilityExecutionContext context);
}

/// <summary>
/// Contexto compartilhado na execução de habilidades.
/// </summary>
public readonly struct AbilityExecutionContext
{
    public readonly GameObject User;
    public readonly Vector2 AimDirection;
    public readonly Vector2 PlacementPosition;
    public readonly int Tier;
    public readonly ulong OwnerClientId;

    public AbilityExecutionContext(
        GameObject user,
        Vector2 aimDirection,
        Vector2 placementPosition,
        int tier,
        ulong ownerClientId)
    {
        User = user;
        AimDirection = aimDirection;
        PlacementPosition = placementPosition;
        Tier = tier;
        OwnerClientId = ownerClientId;
    }
}
