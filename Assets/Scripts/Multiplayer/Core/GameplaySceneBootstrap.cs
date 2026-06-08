using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap mínimo ao entrar em cenas de gameplay: remove overlay de transição.
/// </summary>
public static class GameplaySceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "Fase-1" && scene != "Fase-2")
            return;

        ScreenFlowController.Instance?.ClearTransitionOverlay();

        Canvas hudCanvas = Object.FindFirstObjectByType<Canvas>();
        if (hudCanvas != null)
            PlayerAbilityHud.EnsureOnCanvas(hudCanvas);
    }
}
