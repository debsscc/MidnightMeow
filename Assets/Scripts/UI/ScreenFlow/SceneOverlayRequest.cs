using UnityEngine;

/// <summary>
/// Abre/fecha um overlay por ID — ideal para botões de pause, baú, etc.
/// </summary>
[DisallowMultipleComponent]
public class SceneOverlayRequest : MonoBehaviour
{
    [SerializeField] private SceneOverlayController controller;

    [SerializeField] private string overlayId = "pause";

    [SerializeField] private FlowEventRelay flowEvents = new FlowEventRelay();

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<SceneOverlayController>();
    }

    public void Open()
    {
        if (controller == null)
        {
            Debug.LogError("SceneOverlayRequest: SceneOverlayController não encontrado.");
            return;
        }

        flowEvents.InvokeBefore(this);
        controller.OpenOverlay(overlayId);
    }

    public void Close()
    {
        if (controller == null)
            return;

        flowEvents.InvokeBefore(this);
        controller.CloseOverlay(overlayId);
        flowEvents.InvokeAfter(this);
    }

    public void CloseTop()
    {
        controller?.CloseTopOverlay();
    }
}
