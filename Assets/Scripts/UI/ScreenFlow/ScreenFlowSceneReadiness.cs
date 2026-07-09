using System.Collections;
using UnityEngine;

/// <summary>
/// Sincroniza o overlay de transição com a inicialização da cena destino.
/// </summary>
public static class ScreenFlowSceneReadiness
{
    public const int DefaultRenderPaddingFrames = 2;

    private static string _pendingScene;
    private static bool _isReady;

    public static void BeginAwaiting(string sceneName)
    {
        _pendingScene = sceneName;
        _isReady = false;
    }

    public static void MarkReady()
    {
        _isReady = true;
    }

    public static void MarkReadyIfPending(string sceneName)
    {
        if (string.IsNullOrEmpty(_pendingScene) || _pendingScene != sceneName)
            return;

        MarkReady();
    }

    public static void CancelAwaiting()
    {
        _pendingScene = null;
        _isReady = false;
    }

    public static IEnumerator WaitUntilReady(string expectedScene, float timeoutSeconds = 10f)
    {
        if (string.IsNullOrEmpty(_pendingScene) || _pendingScene != expectedScene)
            yield break;

        float timer = 0f;
        while (!_isReady && timer < timeoutSeconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_isReady)
            Debug.LogWarning($"[ScreenFlow] Cena '{expectedScene}' não sinalizou prontidão a tempo; liberando overlay.");

        CancelAwaiting();
    }

    /// <summary>
    /// Recuo de frames após a cena reportar carregamento — permite Start() e o primeiro draw na GPU.
    /// </summary>
    public static IEnumerator WaitForRenderPadding(int extraFrames = DefaultRenderPaddingFrames)
    {
        int frames = Mathf.Max(0, extraFrames);
        for (int i = 0; i < frames; i++)
            yield return null;
    }

    /// <summary>
    /// Mantém o fade opaco até o <see cref="AsyncOperation"/> atingir 100% (isDone).
    /// </summary>
    public static IEnumerator WaitUntilLoadComplete(AsyncOperation asyncLoad)
    {
        if (asyncLoad == null)
            yield break;

        while (!asyncLoad.isDone)
            yield return null;
    }
}
