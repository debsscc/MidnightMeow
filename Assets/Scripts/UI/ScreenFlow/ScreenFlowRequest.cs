using UnityEngine;

/// <summary>
/// Liga um evento do Inspector (botão, missão, trigger) a uma rota de cena do catálogo.
/// Use UnityEvents em <see cref="flowEvents"/> para SFX/VFX.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFlowRequest : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Opcional se routeId estiver preenchido.")]
    [SerializeField] private SceneFlowCatalog catalog;

    [SerializeField] private SceneFlowRouteDefinition routeAsset;

    [SerializeField] private string routeId;

    [Header("Transição")]
    [SerializeField] private ScreenTransitionMode modeOverride = ScreenTransitionMode.UseRouteDefault;

    [Header("Feedback")]
    [SerializeField] private FlowEventRelay flowEvents = new FlowEventRelay();

    [Tooltip("Atraso em segundos (tempo real) antes de iniciar a troca.")]
    [SerializeField] private float delaySeconds;

    private bool _pending;

    public void Execute()
    {
        if (_pending)
            return;

        if (ScreenFlowController.Instance != null && ScreenFlowController.Instance.IsTransitioning)
            return;

        if (delaySeconds > 0f)
            StartCoroutine(ExecuteDelayed());
        else
            RunTransition();
    }

    public void ExecuteRoute(string id)
    {
        routeId = id;
        Execute();
    }

    /// <summary>Para botão Sair do menu. Liga em On Click no Inspector.</summary>
    public void QuitApplication()
    {
        if (ContinueSavePanelController.TryHandleMenuBack())
            return;

        flowEvents.InvokeBefore(this);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        flowEvents.InvokeAfter(this);
    }

    private System.Collections.IEnumerator ExecuteDelayed()
    {
        _pending = true;
        yield return new WaitForSecondsRealtime(delaySeconds);
        _pending = false;
        RunTransition();
    }

    private void RunTransition()
    {
        flowEvents.InvokeBefore(this);

        ScreenFlowController controller = ScreenFlowController.Instance;
        if (controller == null)
        {
            Debug.LogError("ScreenFlowRequest: ScreenFlowController não encontrado. Inicie pelo Bootstrap.");
            return;
        }

        bool ok;
        if (routeAsset != null)
        {
            ScreenTransitionMode mode = modeOverride == ScreenTransitionMode.UseRouteDefault
                ? routeAsset.transitionMode
                : modeOverride;
            ok = controller.RequestScene(
                routeAsset.sceneName,
                mode,
                routeAsset.loadKind,
                routeAsset.fadeTime,
                routeAsset.minLoadingTime);
        }
        else if (!string.IsNullOrEmpty(routeId))
        {
            if (catalog != null)
                controller.SetCatalog(catalog);
            ok = controller.RequestRoute(routeId, modeOverride);
        }
        else
        {
            Debug.LogError("ScreenFlowRequest: defina routeAsset ou routeId.");
            return;
        }

        if (ok)
            controller.OnTransitionCompleted += HandleCompletedOnce;
    }

    private void HandleCompletedOnce(string _)
    {
        if (ScreenFlowController.Instance != null)
            ScreenFlowController.Instance.OnTransitionCompleted -= HandleCompletedOnce;
        flowEvents.InvokeAfter(this);
    }

    private void OnDisable()
    {
        if (ScreenFlowController.Instance != null)
            ScreenFlowController.Instance.OnTransitionCompleted -= HandleCompletedOnce;
    }
}
