using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Menu principal: Novo Jogo, Continuar (saves de host), Opções, feedback de playtest e Sair.
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    public const string PanelMain = "main";
    public const string PanelSaves = "saves";
    public const string PanelOptions = "options";

    [Header("Navegação")]
    [SerializeField] private ScreenPanelNavigator navigator;

    [Header("Painéis")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject legacyHubPanel;

    [Header("Botões — Menu")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button playtestFeedbackButton;
    [SerializeField] private Button quitButton;

    [Header("Playtest")]
    [SerializeField] private string playtestFormUrl =
        "https://docs.google.com/forms/d/e/1FAIpQLScqrERAjHtXbsp-kTXYh86otM1uvqKOICOwL0JFGYLe5203aw/viewform?usp=sharing&ouid=104196659444550947531";

    [Header("Saves")]
    [SerializeField] private TMP_Text savesListText;
    [SerializeField] private Button savesBackButton;
    [SerializeField] private Button[] saveSlotButtons;

    [Header("Opções")]
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private TMP_Text optionsInfoText;

    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private readonly List<int> _hostSaveSlots = new List<int>();

    private void Awake()
    {
        if (buildPlaceholderIfMissing && mainMenuPanel == null)
            BuildPlaceholderUI();

        EnsureCreditsButtonIfMissing();
        WireButtons();
        RefreshContinueState();

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            save = FindFirstObjectByType<SaveProfileStore>();

        if (save != null)
            save.OnProfileChanged += RefreshContinueState;
    }

    private void OnEnable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null)
            return;

        flow.OnLoadingScreenVisibilityChanged += HandleLoadingScreenVisibilityChanged;
    }

    private void OnDisable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null)
            return;

        flow.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;
    }

    private void OnDestroy()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged -= RefreshContinueState;
    }

    private void HandleLoadingScreenVisibilityChanged(bool visible)
    {
        if (!visible)
            return;

        HideMenuVisuals();
    }

    private void HideMenuVisuals()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (legacyHubPanel != null)
            legacyHubPanel.SetActive(false);

        Canvas ownedCanvas = GetComponentInChildren<Canvas>(true);
        if (ownedCanvas != null)
            ownedCanvas.gameObject.SetActive(false);
    }

    private void Start()
    {
        ShowMainMenu();
        ScreenFlowSceneReadiness.MarkReadyIfPending("Menu2");
    }

    private void WireButtons()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCredits);
        if (playtestFeedbackButton != null) playtestFeedbackButton.onClick.AddListener(OnOpenPlaytestForm);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (savesBackButton != null) savesBackButton.onClick.AddListener(ShowMainMenu);
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(ShowMainMenu);

        if (saveSlotButtons != null)
        {
            for (int i = 0; i < saveSlotButtons.Length; i++)
            {
                int slot = i;
                if (saveSlotButtons[i] != null)
                    saveSlotButtons[i].onClick.AddListener(() => OnSaveSlotSelected(slot));
            }
        }
    }

    public void ShowMainMenu()
    {
        HideLegacyMenuContent();
        navigator?.ShowPanel(PanelMain);
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
            if (canvas.gameObject.name is "FadeManager" or "CreditsOverlay")
                continue;

            if (canvas.GetComponentInParent<CreditsOverlayController>(true) != null)
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

        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "new_game");
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
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "continue");
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.HasAnyHostSave())
            return;

        RefreshSavesPanel();
        navigator?.ShowPanel(PanelSaves);
    }

    public void OnOptions()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "options");
        if (optionsInfoText != null)
            optionsInfoText.text = "Gráficos, áudio, controles e geral — placeholders para implementação futura.";

        navigator?.ShowPanel(PanelOptions);
    }

    public void OnCredits()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "credits");
        CreditsOverlayController.Open();
    }

    private void OnSaveSlotSelected(int slot)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.CanContinue(slot))
            return;

        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"continue_slot_{slot + 1}");
        GameSessionContext.BeginContinue(slot);
        save.LoadOrCreate(slot);

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(SceneFlowRouteIds.MenuToLobby);
        else
            ScreenFlowController.Instance?.RequestRoute(SceneFlowRouteIds.MenuToLobby);
    }

    public void OnQuit()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnOpenPlaytestForm()
    {
        if (string.IsNullOrWhiteSpace(playtestFormUrl))
        {
            Debug.LogWarning("[MainMenuController] URL do formulário de playtest não configurada.");
            return;
        }

        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "playtest_feedback");
        Application.OpenURL(playtestFormUrl);
    }

    private void RefreshContinueState()
    {
        if (continueButton == null)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        bool canContinue = save != null && save.HasAnyHostSave();
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = canContinue;
    }

    private void RefreshSavesPanel()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        _hostSaveSlots.Clear();

        if (save == null)
            return;

        save.GetHostSaveSlots(_hostSaveSlots);

        var builder = new StringBuilder();
        for (int i = 0; i < _hostSaveSlots.Count; i++)
        {
            int slot = _hostSaveSlots[i];
            GameSaveData data = save.PeekSlot(slot);
            if (data == null)
                continue;

            DateTime played = new DateTime(data.lastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime();
            builder.AppendLine($"Save {slot + 1} — {played:dd/MM/yyyy HH:mm}");
            if (!string.IsNullOrEmpty(data.lastJoinCode))
                builder.AppendLine($"  Código: {data.lastJoinCode}");
            builder.AppendLine($"  Magículas: {data.magiculas}");
        }

        if (savesListText != null)
            savesListText.text = builder.Length > 0 ? builder.ToString() : "Nenhum save de host encontrado.";

        if (saveSlotButtons != null)
        {
            for (int i = 0; i < saveSlotButtons.Length; i++)
            {
                if (saveSlotButtons[i] == null)
                    continue;

                bool visible = _hostSaveSlots.Contains(i);
                saveSlotButtons[i].gameObject.SetActive(visible);
                saveSlotButtons[i].interactable = save.CanContinue(i);
            }
        }
    }

    private void BuildPlaceholderUI()
    {
        navigator = gameObject.AddComponent<ScreenPanelNavigator>();
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        canvas.sortingOrder = 300;

        mainMenuPanel = BuildMainMenuPanel(canvas.transform);
        GameObject savesPanel = BuildSavesPanel(canvas.transform);
        GameObject optionsPanel = BuildOptionsPanel(canvas.transform);

        navigator.RegisterPanel(PanelMain, mainMenuPanel);
        navigator.RegisterPanel(PanelSaves, savesPanel);
        navigator.RegisterPanel(PanelOptions, optionsPanel);
        navigator.ShowPanel(PanelMain);
    }

    private GameObject BuildMainMenuPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelMain, new Color(0.05f, 0.05f, 0.08f, 0.92f));

        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Midnight Meow", 64,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400f, -120f), new Vector2(400f, -20f));

        const float left = 40f;
        const float width = 280f;
        const float height = 56f;
        const float gap = 16f;
        const float top = 220f;
        float y = top;

        newGameButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Novo Jogo",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -y - height), new Vector2(left + width, -y));
        y += height + gap;

        continueButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Continuar",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -y - height), new Vector2(left + width, -y));
        y += height + gap;

        optionsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Opções",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -y - height), new Vector2(left + width, -y));
        y += height + gap;

        creditsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Créditos",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -y - height), new Vector2(left + width, -y));
        y += height + gap;

        quitButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Sair",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -y - height), new Vector2(left + width, -y));

        playtestFeedbackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Feedback Playtest",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-304f, 24f), new Vector2(-24f, 96f));

        return panel;
    }

    private void EnsureCreditsButtonIfMissing()
    {
        if (creditsButton != null)
            return;

        Transform searchRoot = mainMenuPanel != null ? mainMenuPanel.transform : transform;
        creditsButton = FindCreditsButtonInHierarchy(searchRoot);
        if (creditsButton != null || !buildPlaceholderIfMissing)
            return;

        Transform buttonParent = mainMenuPanel != null ? mainMenuPanel.transform : transform;
        TryCreateCreditsButton(buttonParent);
    }

    private static Button FindCreditsButtonInHierarchy(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (ContainsCreditsKeyword(button.gameObject.name))
                return button;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && ContainsCreditsKeyword(label.text))
                return button;
        }

        return null;
    }

    private static bool ContainsCreditsKeyword(string value)
    {
        return !string.IsNullOrEmpty(value)
            && value.IndexOf("crédito", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void TryCreateCreditsButton(Transform parent)
    {
        const float left = 40f;
        const float width = 280f;
        const float height = 56f;
        const float gap = 16f;

        if (quitButton != null)
        {
            RectTransform quitRt = quitButton.GetComponent<RectTransform>();
            float topEdge = -quitRt.offsetMin.y + gap;
            creditsButton = ScreenFlowPlaceholderFactory.CreateButton(parent, "Créditos",
                quitRt.anchorMin, quitRt.anchorMax,
                new Vector2(quitRt.offsetMin.x, -topEdge - height),
                new Vector2(quitRt.offsetMax.x, -topEdge));
            return;
        }

        if (optionsButton != null)
        {
            RectTransform optionsRt = optionsButton.GetComponent<RectTransform>();
            float topEdge = -optionsRt.offsetMin.y + height + gap;
            creditsButton = ScreenFlowPlaceholderFactory.CreateButton(parent, "Créditos",
                optionsRt.anchorMin, optionsRt.anchorMax,
                new Vector2(optionsRt.offsetMin.x, -topEdge - height),
                new Vector2(optionsRt.offsetMax.x, -topEdge));
            return;
        }

        creditsButton = ScreenFlowPlaceholderFactory.CreateButton(parent, "Créditos",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(left, -492f), new Vector2(left + width, -436f));
    }

    private GameObject BuildSavesPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelSaves, new Color(0.06f, 0.06f, 0.1f, 0.94f));

        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Saves (partidas como host)", 42,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-500f, -100f), new Vector2(500f, -20f));

        savesListText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 24,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);

        saveSlotButtons = new Button[GameSaveData.MaxSlots];
        for (int i = 0; i < saveSlotButtons.Length; i++)
        {
            float rowY = 0.45f - i * 0.1f;
            saveSlotButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, $"Continuar Save {i + 1}",
                new Vector2(0.5f, rowY), new Vector2(0.5f, rowY), new Vector2(-220f, -30f), new Vector2(220f, 30f));
            saveSlotButtons[i].gameObject.SetActive(false);
        }

        savesBackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));

        return panel;
    }

    private GameObject BuildOptionsPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelOptions, new Color(0.05f, 0.07f, 0.1f, 0.94f));

        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Opções", 48,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300f, -90f), new Vector2(300f, -10f));

        optionsInfoText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 24,
            TextAlignmentOptions.Center, Color.white,
            new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero);

        optionsBackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));

        return panel;
    }
}
