using UnityEngine;

/// <summary>
/// Resolve contrato → cena de gameplay de forma centralizada (solo e MP).
/// </summary>
public static class ContractSceneResolver
{
    public const string DefaultSceneName = "Fase-1";
    private const int ContractCount = 3;

    public static int ResolveActiveContractIndex()
    {
        if (GameSessionContext.ActiveContractIndex >= 0)
            return GameSessionContext.ActiveContractIndex;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null && save.Active.selectedContractIndex >= 0)
            return save.Active.selectedContractIndex;

        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session != null && session.SelectedContractIndex >= 0)
            return session.SelectedContractIndex;

        return -1;
    }

    public static ContractDefinition ResolveContract(int index)
    {
        if (index < 0 || index >= ContractCount)
            return null;

        ContractCatalog catalog = ContractCatalog.LoadCached();
        if (catalog != null && catalog.TryGetContract(index, out ContractDefinition fromCatalog))
            return fromCatalog;

        return FindContractAsset($"Contract_{index + 1}");
    }

    public static string ResolveSceneName(int index)
    {
        ContractDefinition contract = ResolveContract(index);
        if (contract == null || string.IsNullOrEmpty(contract.gameplaySceneName))
            return DefaultSceneName;

        return contract.gameplaySceneName;
    }

    public static void ApplyToSession(int index)
    {
        if (index < 0)
            index = ResolveActiveContractIndex();

        GameSessionContext.ActiveContractIndex = index;
        GameSessionContext.ActiveGameplaySceneName = ResolveSceneName(index);

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null && index >= 0)
        {
            save.Active.selectedContractIndex = index;
            save.SaveActive();
        }
    }

    public static ContractDefinition[] ResolveAllContracts()
    {
        var contracts = new ContractDefinition[ContractCount];
        for (int i = 0; i < ContractCount; i++)
            contracts[i] = ResolveContract(i);
        return contracts;
    }

    public static void FillMissingSlots(ContractDefinition[] contracts)
    {
        if (contracts == null || contracts.Length < ContractCount)
            return;

        for (int i = 0; i < ContractCount; i++)
        {
            if (contracts[i] != null)
                continue;

            contracts[i] = ResolveContract(i);
            if (contracts[i] != null)
                continue;

            contracts[i] = ScriptableObject.CreateInstance<ContractDefinition>();
            contracts[i].displayName = $"Contrato {i + 1}";
            contracts[i].description = i == 0 ? "Fase inicial." : "Bloqueado por enquanto.";
            contracts[i].gameplaySceneName = DefaultSceneName;
        }
    }

    private static ContractDefinition FindContractAsset(string assetName)
    {
        ContractDefinition[] loaded = Resources.FindObjectsOfTypeAll<ContractDefinition>();
        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].name == assetName)
                return loaded[i];
        }

        return null;
    }
}
