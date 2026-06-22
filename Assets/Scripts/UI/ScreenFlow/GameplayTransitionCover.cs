using System.Collections;
using UnityEngine;

/// <summary>
/// Garante que entidades de gameplay só sumam depois que o overlay de transição cobriu a tela.
/// </summary>
public static class GameplayTransitionCover
{
    private const float DefaultTimeoutSeconds = 3f;

    public static bool IsOpaque =>
        TransitionFadeOverlay.Instance != null && TransitionFadeOverlay.Instance.IsFadeOpaque;

    public static IEnumerator WaitUntilOpaque(float timeoutSeconds = DefaultTimeoutSeconds)
    {
        TransitionFadeOverlay.EnsureExists();
        ScreenFlowController.EnsureExists();

        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (IsOpaque)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
