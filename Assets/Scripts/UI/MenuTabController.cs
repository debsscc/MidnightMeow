/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Navegação entre abas do menu (Levels, Settings, Upgrades, Controls).
---------------------------------------------------------------- */

using UnityEngine;

[DisallowMultipleComponent]
public class MenuTabController : MonoBehaviour
{
    [Tooltip("Telas do menu (Levels, Settings, Upgrades, Controls).")]
    [SerializeField] private GameObject[] menuTabs;

    [Tooltip("Aba ativa ao carregar a cena.")]
    [SerializeField] private GameObject defaultTab;

    private void Start()
    {
        if (defaultTab != null)
            OpenTab(defaultTab);
    }

    public void OpenTab(GameObject targetTab)
    {
        CloseOverlayPanels();

        if (menuTabs == null || menuTabs.Length == 0)
        {
            Debug.LogWarning("MenuTabController: Nenhuma aba foi configurada no array 'menuTabs'.");
            return;
        }

        foreach (GameObject tab in menuTabs)
        {
            if (tab == null)
                continue;

            tab.SetActive(tab == targetTab);
        }

        if (targetTab != null && targetTab.activeInHierarchy)
            UiSelectionUtility.SelectFirstUnder(targetTab.transform);
    }

    public void ResetToDefaultTab()
    {
        if (defaultTab != null)
            OpenTab(defaultTab);
    }

    private static void CloseOverlayPanels()
    {
        ContinueSavePanelController savePanel = FindFirstObjectByType<ContinueSavePanelController>();
        savePanel?.HideSavePanel();

        ControlsPanelController controls = ControlsPanelController.FindInScene();
        controls?.HidePanel();
    }
}
