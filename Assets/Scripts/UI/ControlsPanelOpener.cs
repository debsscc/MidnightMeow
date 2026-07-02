/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-07-01
DESCRIÇÃO: Abre o ControlsPanelController e retorna ao painel configurado (ex.: Opções).
---------------------------------------------------------------- */

using UnityEngine;

[DisallowMultipleComponent]
public class ControlsPanelOpener : MonoBehaviour
{
    [SerializeField] private ControlsPanelController controlsPanel;
    [SerializeField] private GameObject returnPanel;

    public void OpenControls()
    {
        ControlsPanelController panel = ResolvePanel();
        if (panel == null)
        {
            Debug.LogWarning("ControlsPanelOpener: ControlsPanelController não encontrado.");
            return;
        }

        panel.ShowFrom(returnPanel);
    }

    private ControlsPanelController ResolvePanel()
    {
        if (controlsPanel != null)
            return controlsPanel;

        return ControlsPanelController.FindInScene();
    }
}
