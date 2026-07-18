using System;

/// <summary>
/// Decide o destino do botão Prosseguir na tela de vitória (próxima fase vs créditos).
/// </summary>
public static class VictoryContinueResolver
{
    public const int FinalContractIndex = 2;
    public const string FinalPhaseSceneName = "Fase-3";

    public static bool IsFinalPhase(int activeContractIndex, string activeGameplaySceneName)
    {
        if (activeContractIndex >= FinalContractIndex)
            return true;

        return !string.IsNullOrEmpty(activeGameplaySceneName)
               && activeGameplaySceneName.Equals(FinalPhaseSceneName, StringComparison.OrdinalIgnoreCase);
    }

    public static int InferContractIndexFromScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return -1;

        if (sceneName.Equals("Fase-1", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (sceneName.Equals("Fase-2", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (sceneName.Equals(FinalPhaseSceneName, StringComparison.OrdinalIgnoreCase))
            return FinalContractIndex;

        return -1;
    }

    public static int ResolveCurrentContractIndex(int activeContractIndex, string activeGameplaySceneName)
    {
        if (activeContractIndex >= 0)
            return activeContractIndex;

        int fromScene = InferContractIndexFromScene(activeGameplaySceneName);
        return fromScene >= 0 ? fromScene : 0;
    }

    public static int ResolveNextContractIndex(int activeContractIndex, string activeGameplaySceneName)
    {
        return ResolveCurrentContractIndex(activeContractIndex, activeGameplaySceneName) + 1;
    }
}
