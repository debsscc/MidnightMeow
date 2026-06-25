using UnityEngine;

/// <summary>
/// Configuração global de desbloqueio de contratos/fases.
/// </summary>
[CreateAssetMenu(fileName = "ContractProgressionConfig", menuName = "MidnightMeow/Progression/Contract Progression Config")]
public class ContractProgressionConfig : ScriptableObject
{
    [Tooltip("Quando true, todos os contratos ficam selecionáveis (testes). Desligue para progressão linear.")]
    public bool unlockAllContractsForTesting = true;

    private static ContractProgressionConfig _cached;

    public static ContractProgressionConfig LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<ContractProgressionConfig>("ContractProgressionConfig");
        return _cached;
    }
}
