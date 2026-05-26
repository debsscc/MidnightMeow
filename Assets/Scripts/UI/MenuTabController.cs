using UnityEngine;

/// <summary>
/// Controla a navegação local entre as telas do menu (Abas).
/// Garante que apenas uma tela esteja ativa por vez.
/// </summary>
[DisallowMultipleComponent]
public class MenuTabController : MonoBehaviour
{
    [Tooltip("Arraste todas as telas que fazem parte deste menu (Levels, Settings, Upgrades, Controls).")]
    [SerializeField] private GameObject[] menuTabs;

    [Tooltip("A tela que deve aparecer ativada por padrão ao carregar a cena.")]
    [SerializeField] private GameObject defaultTab;

    private void Start()
    {
        // Garante que o menu inicie no estado correto
        if (defaultTab != null)
        {
            OpenTab(defaultTab);
        }
    }

    /// <summary>
    /// Desativa todas as abas e ativa apenas a aba solicitada.
    /// Este método deve ser chamado pelo evento OnClick() dos botões.
    /// </summary>
    public void OpenTab(GameObject targetTab)
    {
        if (menuTabs == null || menuTabs.Length == 0)
        {
            Debug.LogWarning("MenuTabController: Nenhuma aba foi configurada no array 'menuTabs'.");
            return;
        }

        foreach (var tab in menuTabs)
        {
            if (tab == null) continue;
            
            // Ativa se for a aba alvo, desativa caso contrário
            tab.SetActive(tab == targetTab);
        }
    }
}