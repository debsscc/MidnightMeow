using UnityEngine;

/// <summary>
/// Referências diretas aos contratos de produção (carregado de Resources/).
/// Evita falhas de <see cref="Resources.FindObjectsOfTypeAll"/> no fluxo Preparação → Characters.
/// </summary>
[CreateAssetMenu(fileName = "ContractCatalog", menuName = "MidnightMeow/Contracts/Contract Catalog")]
public class ContractCatalog : ScriptableObject
{
    public ContractDefinition[] contracts;

    private static ContractCatalog _cached;

    public static ContractCatalog LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<ContractCatalog>("ContractCatalog");
        return _cached;
    }

    public bool TryGetContract(int index, out ContractDefinition contract)
    {
        contract = null;
        if (contracts == null || index < 0 || index >= contracts.Length)
            return false;

        contract = contracts[index];
        return contract != null;
    }
}
