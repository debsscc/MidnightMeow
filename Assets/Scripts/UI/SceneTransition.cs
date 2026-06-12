///* ----------------------------------------------------------------
// AUTOR: Débora Carvalho
// DATA: 2026-04-01
// DESCRIÇÃO: Registra visuais de fade/loading da cena no ScreenFlowController persistente.
// A lógica de transição ficou centralizada em ScreenFlowController.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-250)]
public class SceneTransition : MonoBehaviour
{
    public float fadeTime = 1f;
    public float minLoadingTime = 2f;
    public Image fadeImage;
    public GameObject loadingScreen;

    private void Awake()
    {
        ScreenFlowController.EnsureExists();
        RegisterWithScreenFlow();
    }

    private void OnEnable()
    {
        if (ScreenFlowController.Instance != null)
            RegisterWithScreenFlow();
    }

    private void RegisterWithScreenFlow()
    {
        ScreenFlowController.Instance?.RegisterSceneVisuals(fadeImage, loadingScreen, fadeTime, minLoadingTime);
    }

    /// <summary>Compatibilidade com botões antigos. Prefira ScreenFlowRequest.</summary>
    public bool TryBeginTransition(string sceneName)
    {
        if (ScreenFlowController.Instance != null)
            return ScreenFlowController.Instance.TryBeginTransition(sceneName);

        return false;
    }

    public void ChangeScene(string sceneName) => TryBeginTransition(sceneName);

    public bool IsTransitioning =>
        ScreenFlowController.Instance != null && ScreenFlowController.Instance.IsTransitioning;

    public AsyncOperation CurrentAsyncLoad =>
        ScreenFlowController.Instance != null ? ScreenFlowController.Instance.CurrentAsyncLoad : null;
}
