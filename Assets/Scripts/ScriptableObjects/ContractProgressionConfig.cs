using UnityEngine;

/// <summary>
/// Configuração global de desbloqueio de contratos/fases.
/// </summary>
[CreateAssetMenu(fileName = "ContractProgressionConfig", menuName = "MidnightMeow/Progression/Contract Progression Config")]
public class ContractProgressionConfig : ScriptableObject
{
    [Tooltip("Quando true, todos os contratos ficam selecionáveis (testes). Em produção deve ficar false (só Contrato 1 no Novo Jogo).")]
    public bool unlockAllContractsForTesting = false;

    private static ContractProgressionConfig _cached;

    public static ContractProgressionConfig LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<ContractProgressionConfig>("ContractProgressionConfig");
        return _cached;
    }
}
