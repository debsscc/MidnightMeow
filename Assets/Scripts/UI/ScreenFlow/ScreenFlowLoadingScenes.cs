/// <summary>
/// Cenas dedicadas de carregamento no fluxo de telas.
/// </summary>
public static class ScreenFlowLoadingScenes
{
    public const string Loading1 = "Loading1";
    public const string Loading2 = "Loading2";

    public static bool IsDedicatedLoadingScene(string sceneName) =>
        sceneName is Loading1 or Loading2;
}
