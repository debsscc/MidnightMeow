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
    [SerializeField] private Button[] saveDeleteSlotButtons;
    [SerializeField] private Button deleteAllSavesButton;

    [Header("Saves — confirmação de exclusão")]
    [SerializeField] private GameObject deleteConfirmationRoot;
    [SerializeField] private TMP_Text deleteConfirmationText;
    [SerializeField] private Button deleteConfirmButton;
    [SerializeField] private Button deleteCancelButton;

    [Header("Continuar / Saves")]
    [SerializeField] private ContinueSavePanelController continueSavePanel;

    [Header("Opções")]
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button resetAudioDefaultsButton;

    [SerializeField] private bool buildPlaceholderIfMissing;

    private readonly List<int> _hostSaveSlots = new List<int>();
    private int? _pendingDeleteSlot;
    private bool _pendingDeleteAll;
    private bool _hiddenForLoading;

    private void Awake()
    {
        ApplyResponsiveCanvasScalers();
        ApplyMenuCanvasGammaSpace();

        if (buildPlaceholderIfMissing && mainMenuPanel == null)
            BuildPlaceholderUI();

        EnsureCreditsButtonIfMissing();
        if (continueSavePanel == null)
            continueSavePanel = GetComponent<ContinueSavePanelController>();
        if (buildPlaceholderIfMissing)
        {
            EnsureSaveDeleteUiIfMissing();
        }
        EnsureAudioVolumeSlidersIfMissing();
        EnsureResetAudioDefaultsButtonIfMissing();
        WireButtons();
        InitializeAudioVolumeSliders();
        HideDeleteConfirmation();
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
        if (visible)
        {
            _hiddenForLoading = true;
            HideMenuVisuals();
            return;
        }

        if (!_hiddenForLoading)
            return;

        _hiddenForLoading = false;
        ShowMainMenu();
    }

    private void HideMenuVisuals()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (legacyHubPanel != null)
            legacyHubPanel.SetActive(false);
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
        if (resetAudioDefaultsButton != null)
            resetAudioDefaultsButton.onClick.AddListener(OnResetAudioDefaultsRequested);

        if (saveSlotButtons != null)
        {
            for (int i = 0; i < saveSlotButtons.Length; i++)
            {
                int slot = i;
                if (saveSlotButtons[i] != null)
                    saveSlotButtons[i].onClick.AddListener(() => OnSaveSlotSelected(slot));
            }
        }

        if (saveDeleteSlotButtons != null)
        {
            for (int i = 0; i < saveDeleteSlotButtons.Length; i++)
            {
                int slot = i;
                if (saveDeleteSlotButtons[i] != null)
                    saveDeleteSlotButtons[i].onClick.AddListener(() => OnDeleteSaveSlotRequested(slot));
            }
        }

        if (deleteConfirmButton != null)
            deleteConfirmButton.onClick.AddListener(ExecutePendingDelete);
        if (deleteCancelButton != null)
            deleteCancelButton.onClick.AddListener(HideDeleteConfirmation);
        if (deleteAllSavesButton != null)
            deleteAllSavesButton.onClick.AddListener(OnDeleteAllSavesRequested);
    }

    public void ShowMainMenu()
    {
        HideDeleteConfirmation();
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
            {
                canvas.gameObject.SetActive(false);
                continue;
            }

            if (canvas.GetComponentInParent<CreditsOverlayController>(true) != null)
                continue;

            if (mainMenuPanel != null
                && (mainMenuPanel == canvas.gameObject || mainMenuPanel.transform.IsChildOf(canvas.transform)))
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

        if (continueSavePanel != null)
        {
            continueSavePanel.Open();
            return;
        }

        RefreshSavesPanel();
        navigator?.ShowPanel(PanelSaves);
    }

    public void OnOptions()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "options");
        RefreshAudioVolumeSlidersUi();
        navigator?.ShowPanel(PanelOptions);
    }

    private void OnResetAudioDefaultsRequested()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "reset_audio_defaults");
        GameAudioSettings.EnsureExists();
        GameAudioSettings.Instance?.ResetToDefaults();
        RefreshAudioVolumeSlidersUi();
    }

    public void OnCredits()
    {
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "credits");
        CreditsOverlayController.Open();
    }

    private void OnDeleteSaveSlotRequested(int slot)
    {
        if (!CanRequestSaveDeletion())
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.CanContinue(slot))
            return;

        GameSaveData data = save.PeekSlot(slot);
        if (data == null)
            return;

        _pendingDeleteAll = false;
        _pendingDeleteSlot = slot;
        ShowDeleteConfirmation(BuildDeleteSlotMessage(slot, data));
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"delete_save_prompt_slot_{slot + 1}");
    }

    private void OnDeleteAllSavesRequested()
    {
        if (!CanRequestSaveDeletion())
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.HasAnySave())
            return;

        _pendingDeleteAll = true;
        _pendingDeleteSlot = null;
        ShowDeleteConfirmation(BuildDeleteAllMessage(save));
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "delete_all_saves_prompt");
    }

    private void ExecutePendingDelete()
    {
        if (!CanRequestSaveDeletion())
        {
            HideDeleteConfirmation();
            return;
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
        {
            HideDeleteConfirmation();
            return;
        }

        if (_pendingDeleteAll)
        {
            int deleted = save.DeleteAllSlots();
            MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"delete_all_saves_confirmed_{deleted}");
        }
        else if (_pendingDeleteSlot.HasValue)
        {
            int slot = _pendingDeleteSlot.Value;
            save.DeleteSlot(slot);
            MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"delete_save_confirmed_slot_{slot + 1}");
        }

        HideDeleteConfirmation();
        RefreshContinueState();
    }

    private void HideDeleteConfirmation()
    {
        _pendingDeleteAll = false;
        _pendingDeleteSlot = null;

        if (deleteConfirmationRoot != null)
            deleteConfirmationRoot.SetActive(false);
    }

    private void ShowDeleteConfirmation(string message)
    {
        if (deleteConfirmationText != null)
            deleteConfirmationText.text = message;

        if (deleteConfirmationRoot != null)
        {
            deleteConfirmationRoot.transform.SetAsLastSibling();
            deleteConfirmationRoot.SetActive(true);
        }
    }

    private static bool CanRequestSaveDeletion()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return false;

        return true;
    }

    private static string BuildDeleteSlotMessage(int slot, GameSaveData data)
    {
        DateTime played = new DateTime(data.lastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime();
        var builder = new StringBuilder();
        builder.AppendLine($"Apagar Save {slot + 1}?");
        builder.AppendLine();
        builder.AppendLine($"Data: {played:dd/MM/yyyy HH:mm}");
        builder.AppendLine($"Magículas: {data.magiculas}");
        builder.AppendLine();
        builder.AppendLine("Esta ação não pode ser desfeita.");
        builder.AppendLine("(Save local — não afeta outros jogadores.)");
        return builder.ToString();
    }

    private static string BuildDeleteAllMessage(SaveProfileStore save)
    {
        int count = 0;
        for (int i = 0; i < GameSaveData.MaxSlots; i++)
        {
            if (save.HasSave(i))
                count++;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Apagar TODOS os saves?");
        builder.AppendLine();
        builder.AppendLine($"Serão removidos {count} arquivo(s) local(is).");
        builder.AppendLine();
        builder.AppendLine("Esta ação não pode ser desfeita.");
        builder.AppendLine("(Save local — não afeta outros jogadores.)");
        return builder.ToString();
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
        if (ContinueSavePanelController.TryHandleMenuBack())
            return;

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
        SaveProfileStore save = SaveProfileStore.Instance;
        bool canContinue = save != null && save.HasAnyHostSave();

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = canContinue;
        }

        RefreshSavesPanel();
        continueSavePanel?.RefreshFromStore();
    }

    private void RefreshDeleteAllSavesButton()
    {
        if (deleteAllSavesButton == null)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        bool hasAny = save != null && save.HasAnySave();
        deleteAllSavesButton.gameObject.SetActive(true);
        deleteAllSavesButton.interactable = hasAny;
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

        if (saveDeleteSlotButtons != null)
        {
            for (int i = 0; i < saveDeleteSlotButtons.Length; i++)
            {
                if (saveDeleteSlotButtons[i] == null)
                    continue;

                bool visible = _hostSaveSlots.Contains(i);
                saveDeleteSlotButtons[i].gameObject.SetActive(visible);
                saveDeleteSlotButtons[i].interactable = save.CanContinue(i);
            }
        }

        RefreshDeleteAllSavesButton();
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

        BuildDeleteConfirmationOverlay(canvas.transform);
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
        saveDeleteSlotButtons = new Button[GameSaveData.MaxSlots];
        for (int i = 0; i < saveSlotButtons.Length; i++)
        {
            float rowY = 0.45f - i * 0.1f;
            saveSlotButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, $"Continuar Save {i + 1}",
                new Vector2(0.5f, rowY), new Vector2(0.5f, rowY), new Vector2(-320f, -30f), new Vector2(-20f, 30f));
            saveSlotButtons[i].gameObject.SetActive(false);

            saveDeleteSlotButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, $"Apagar Save {i + 1}",
                new Vector2(0.5f, rowY), new Vector2(0.5f, rowY), new Vector2(20f, -30f), new Vector2(320f, 30f));
            saveDeleteSlotButtons[i].gameObject.SetActive(false);
        }

        savesBackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));

        deleteAllSavesButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Apagar todos os saves",
            new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), new Vector2(-260f, -32f), new Vector2(260f, 32f));

        return panel;
    }

    private GameObject BuildOptionsPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelOptions, new Color(0.05f, 0.07f, 0.1f, 0.94f));

        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Opções", 48,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300f, -90f), new Vector2(300f, -10f));

        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Áudio", 32,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.72f), new Vector2(0f, -20f), new Vector2(0f, 20f));

        masterVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(panel.transform, "Volume geral", 0.62f,
            GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefMasterVolume));
        musicVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(panel.transform, "Música", 0.52f,
            GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefMusicVolume));
        sfxVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(panel.transform, "SFX", 0.42f,
            GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefSfxVolume));

        resetAudioDefaultsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Restaurar padrões de áudio",
            new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), new Vector2(-280f, -32f), new Vector2(280f, 32f));

        optionsBackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));

        return panel;
    }

    private void EnsureAudioVolumeSlidersIfMissing()
    {
        if (masterVolumeSlider != null && musicVolumeSlider != null && sfxVolumeSlider != null)
            return;

        if (!buildPlaceholderIfMissing)
            return;

        Transform optionsRoot = transform.Find(PanelOptions);
        if (optionsRoot == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                optionsRoot = canvas.transform.Find(PanelOptions);
        }

        if (optionsRoot == null)
            return;

        if (masterVolumeSlider == null)
            masterVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(optionsRoot, "Volume geral", 0.62f,
                GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefMasterVolume));

        if (musicVolumeSlider == null)
            musicVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(optionsRoot, "Música", 0.52f,
                GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefMusicVolume));

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = ScreenFlowPlaceholderFactory.CreateLabeledSlider(optionsRoot, "SFX", 0.42f,
                GameAudioSettings.GetSavedLinear(GameAudioSettings.PrefSfxVolume));
    }

    private void EnsureResetAudioDefaultsButtonIfMissing()
    {
        if (resetAudioDefaultsButton != null || !buildPlaceholderIfMissing)
            return;

        Transform optionsRoot = transform.Find(PanelOptions);
        if (optionsRoot == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                optionsRoot = canvas.transform.Find(PanelOptions);
        }

        if (optionsRoot == null)
            return;

        resetAudioDefaultsButton = ScreenFlowPlaceholderFactory.CreateButton(optionsRoot, "Restaurar padrões de áudio",
            new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), new Vector2(-280f, -32f), new Vector2(280f, 32f));
    }

    private void InitializeAudioVolumeSliders()
    {
        GameAudioSettings.EnsureExists();
        GameAudioSettings settings = GameAudioSettings.Instance;
        settings?.ApplySavedVolumes();

        BindVolumeSlider(masterVolumeSlider, settings?.GetMasterVolume() ?? GameAudioSettings.DefaultLinearVolume,
            value => settings?.SetMasterVolume(value));
        BindVolumeSlider(musicVolumeSlider, settings?.GetMusicVolume() ?? GameAudioSettings.DefaultLinearVolume,
            value => settings?.SetMusicVolume(value));
        BindVolumeSlider(sfxVolumeSlider, settings?.GetSfxVolume() ?? GameAudioSettings.DefaultLinearVolume,
            value => settings?.SetSfxVolume(value));
    }

    private static void BindVolumeSlider(Slider slider, float initialValue, System.Action<float> onChanged)
    {
        if (slider == null || onChanged == null)
            return;

        slider.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.AddListener(value => onChanged(value));
    }

    private void RefreshAudioVolumeSlidersUi()
    {
        GameAudioSettings settings = GameAudioSettings.Instance;
        if (settings == null)
            return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(settings.GetMasterVolume());
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(settings.GetMusicVolume());
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settings.GetSfxVolume());
    }

    private void EnsureSaveDeleteUiIfMissing()
    {
        if (deleteConfirmationRoot == null)
            deleteConfirmationRoot = transform.Find("SaveDeleteConfirmation")?.gameObject;

        if (deleteConfirmationRoot == null && buildPlaceholderIfMissing)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                BuildDeleteConfirmationOverlay(canvas.transform);
        }

        if ((saveDeleteSlotButtons == null || saveDeleteSlotButtons.Length == 0) && buildPlaceholderIfMissing)
            TryCreateSaveDeleteSlotButtons();

        if (deleteAllSavesButton == null && buildPlaceholderIfMissing)
            TryCreateDeleteAllSavesButton();
    }

    private void TryCreateDeleteAllSavesButton()
    {
        Transform savesRoot = transform.Find(PanelSaves);
        if (savesRoot == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                savesRoot = canvas.transform.Find(PanelSaves);
        }

        if (savesRoot == null)
            return;

        deleteAllSavesButton = ScreenFlowPlaceholderFactory.CreateButton(savesRoot, "Apagar todos os saves",
            new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), new Vector2(-260f, -32f), new Vector2(260f, 32f));
    }

    private void BuildDeleteConfirmationOverlay(Transform parent)
    {
        if (deleteConfirmationRoot != null)
            return;

        deleteConfirmationRoot = ScreenFlowPlaceholderFactory.CreateModalOverlay(
            parent,
            "SaveDeleteConfirmation",
            new Color(0f, 0f, 0f, 0.94f),
            new Color(0.04f, 0.04f, 0.06f, 1f),
            new Vector2(920f, 520f),
            out RectTransform card);

        deleteConfirmationText = ScreenFlowPlaceholderFactory.CreateText(card,
            "Confirmar exclusão?", 26, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -40f), new Vector2(400f, 180f));

        deleteConfirmButton = ScreenFlowPlaceholderFactory.CreateButton(card, "Apagar",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 48f), new Vector2(-220f, 108f));

        deleteCancelButton = ScreenFlowPlaceholderFactory.CreateButton(card, "Cancelar",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 48f), new Vector2(420f, 108f));

        deleteConfirmationRoot.SetActive(false);
    }

    private void TryCreateSaveDeleteSlotButtons()
    {
        if (saveSlotButtons == null || saveSlotButtons.Length == 0)
            return;

        saveDeleteSlotButtons = new Button[saveSlotButtons.Length];
        for (int i = 0; i < saveSlotButtons.Length; i++)
        {
            if (saveSlotButtons[i] == null)
                continue;

            RectTransform continueRt = saveSlotButtons[i].GetComponent<RectTransform>();
            saveDeleteSlotButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(continueRt.parent, $"Apagar Save {i + 1}",
                continueRt.anchorMin, continueRt.anchorMax,
                new Vector2(continueRt.offsetMax.x + 8f, continueRt.offsetMin.y),
                new Vector2(continueRt.offsetMax.x + 300f, continueRt.offsetMax.y));
            saveDeleteSlotButtons[i].gameObject.SetActive(false);
        }
    }

    private void ApplyResponsiveCanvasScalers()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
            GameplayHudController.ApplyResponsiveCanvasScaler(canvases[i]);
    }

    private void ApplyMenuCanvasGammaSpace()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null)
                canvas.vertexColorAlwaysGammaSpace = true;
        }
    }
}
