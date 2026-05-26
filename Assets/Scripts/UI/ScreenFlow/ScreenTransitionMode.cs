/// <summary>
/// Modo visual de troca de cena. Novos modos podem ser adicionados no <see cref="ScreenFlowController"/>.
/// </summary>
public enum ScreenTransitionMode
{
    /// <summary>Usa o modo definido na rota (ScriptableObject).</summary>
    UseRouteDefault = 0,
    Instant = 1,
    Fade = 2,
    LoadingScreen = 3
}
