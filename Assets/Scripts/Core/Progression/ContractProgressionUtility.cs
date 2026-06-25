/// <summary>
/// Regras de desbloqueio de contratos (linear ou modo teste).
/// </summary>
public static class ContractProgressionUtility
{
    public const int ContractCount = 3;

    public static bool IsContractUnlocked(int contractIndex, GameSaveData save)
    {
        if (contractIndex < 0 || contractIndex >= ContractCount)
            return false;

        ContractProgressionConfig config = ContractProgressionConfig.LoadCached();
        if (config != null && config.unlockAllContractsForTesting)
            return true;

        if (contractIndex == 0)
            return true;

        if (save == null)
            return false;

        return save.IsContractCompleted(contractIndex - 1);
    }

    public static string GetLockedReason(int contractIndex, GameSaveData save)
    {
        if (IsContractUnlocked(contractIndex, save))
            return string.Empty;

        return contractIndex switch
        {
            1 => "Conclua o Contrato 1 para desbloquear.",
            2 => "Conclua o Contrato 2 para desbloquear.",
            _ => "Contrato bloqueado."
        };
    }
}
