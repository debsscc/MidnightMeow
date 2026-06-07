using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu principal: Novo Jogo, Continuar (host com save) e Sair.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject legacyHubPanel;

    [Header("Botões")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private void Awake()
    {
        if (buildPlaceholderIfMissing && mainMenuPanel == null)
            BuildPlaceholderUI();

        WireButtons();
        RefreshContinueState();
        ShowMainMenu();

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            save = FindFirstObjectByType<SaveProfileStore>();

        if (save != null)
            save.OnProfileChanged += RefreshContinueState;
    }

    private void OnDestroy()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged -= RefreshContinueState;
    }

    private void WireButtons()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
    }

    public void ShowMainMenu()
    {
        HideLegacyMenuContent();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (legacyHubPanel != null) legacyHubPanel.SetActive(false);
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
    }

    private void HideLegacyMenuContent()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.gameObject.name == "FadeManager")
                continue;

            if (mainMenuPanel != null && mainMenuPanel.transform.IsChildOf(canvas.transform))
                continue;

            canvas.gameObject.SetActive(false);
        }
    }

    public void OnNewGame()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        GameSessionContext.BeginNewGame();
        SaveProfileStore save = SaveProfileStore.Instance;
        save?.LoadOrCreate(0);

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(SceneFlowRouteIds.MenuToLobby);
        else
            ScreenFlowController.Instance?.RequestRoute(SceneFlowRouteIds.MenuToLobby);
    }

    public void OnContinue()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.CanContinue())
            return;

        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        GameSessionContext.BeginContinue();
        save.LoadOrCreate(0);

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(SceneFlowRouteIds.MenuToLobby);
        else
            ScreenFlowController.Instance?.RequestRoute(SceneFlowRouteIds.MenuToLobby);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshContinueState()
    {
        if (continueButton == null)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        continueButton.interactable = save != null && save.CanContinue();
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        mainMenuPanel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "MainMenuPanel", new Color(0.05f, 0.05f, 0.08f, 0.92f));

        ScreenFlowPlaceholderFactory.CreateText(mainMenuPanel.transform, "Midnight Meow", 64,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400f, -120f), new Vector2(400f, -20f));

        newGameButton = ScreenFlowPlaceholderFactory.CreateButton(mainMenuPanel.transform, "Novo Jogo",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, 40f), new Vector2(220f, 120f));
        continueButton = ScreenFlowPlaceholderFactory.CreateButton(mainMenuPanel.transform, "Continuar",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -100f), new Vector2(220f, -20f));
        quitButton = ScreenFlowPlaceholderFactory.CreateButton(mainMenuPanel.transform, "Sair",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -240f), new Vector2(220f, -160f));
    }

}
