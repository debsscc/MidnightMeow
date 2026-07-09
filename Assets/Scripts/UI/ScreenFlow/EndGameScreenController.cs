/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Telas de vitória/derrota — prosseguir, reiniciar, abandonar e créditos. Traduzido
---------------------------------------------------------------- */

using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EndGameScreenController : MonoBehaviour
{
    private const string DefaultPlaytestFormUrl =
        "https://docs.google.com/forms/d/e/1FAIpQLScqrERAjHtXbsp-kTXYh86otM1uvqKOICOwL0JFGYLe5203aw/viewform?usp=sharing&ouid=104196659444550947531";

    [SerializeField] private bool isVictory = true;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button feedbackButton;
    [SerializeField] private string playtestFormUrl = DefaultPlaytestFormUrl;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private void Awake()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene == "GameOver")
            isVictory = false;

        if (buildPlaceholderIfMissing && continueButton == null && exitButton == null)
            BuildPlaceholderUI();

        TryAutoResolveReferences();
        RewireLegacyMenuButton();
        WireButtons();
    }

    private void Start()
    {
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
        RefreshPrimaryActionLabel();
    }

    private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

    private void HandleLocaleChanged(Locale _) => RefreshPrimaryActionLabel();

    private void TryAutoResolveReferences()
    {
        if (continueButton == null)
        {
            continueButton = ScreenFlowUiLookup.FindButton("Button_Prosseguir")
                             ?? ScreenFlowUiLookup.FindButton("Button_Continue")
                             ?? ScreenFlowUiLookup.FindButton("Continue")
                             ?? FindButtonByNamePrefix("Button_Reininciar");
        }

        if (exitButton == null)
            exitButton = ScreenFlowUiLookup.FindButton("Button_Abandonar")
                         ?? ScreenFlowUiLookup.FindButton("Button_Menu")
                         ?? ScreenFlowUiLookup.FindButton("Sair");

        if (creditsButton == null)
            creditsButton = ScreenFlowUiLookup.FindButton("Button_Credits")
                            ?? FindButtonByLabelKeyword("crédito");

        if (feedbackButton == null)
            feedbackButton = ScreenFlowUiLookup.FindButton("Button_Feedback")
                             ?? FindButtonByLabelKeyword("feedback");
    }

    private void RewireLegacyMenuButton()
    {
        if (exitButton != null)
            return;

        Button legacyMenu = ScreenFlowUiLookup.FindButton("Button_Menu");
        if (legacyMenu == null || legacyMenu == feedbackButton)
            return;

        Bind(legacyMenu, OnAbandon);
        exitButton = legacyMenu;
    }

    private void WireButtons()
    {
        Bind(continueButton, OnPrimaryAction);
        Bind(exitButton, OnAbandon);
        Bind(creditsButton, OnCredits);
        Bind(feedbackButton, OnOpenPlaytestForm);
    }

    private void RefreshPrimaryActionLabel()
    {
        if (continueButton == null)
            return;

        bool pt = IsPortuguese();
        string label = isVictory
            ? (pt ? "Prosseguir" : "Continue")
            : (pt ? "Reiniciar fase" : "Restart stage");
        SetButtonLabel(continueButton, label);
    }

    private void OnPrimaryAction()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        if (isVictory)
        {
            PreparationSessionManager.Instance?.ResetRound();
            ScreenFlowStateMachine.ContinueAfterEndGame();
            return;
        }

        ScreenFlowStateMachine.RequestRestartCurrentGameplay();
    }

    private void OnAbandon()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        ScreenFlowStateMachine.ExitToMainMenu();
    }

    private void OnCredits()
    {
        CreditsOverlayController.Open();
    }

    private void OnOpenPlaytestForm()
    {
        if (string.IsNullOrWhiteSpace(playtestFormUrl))
        {
            Debug.LogWarning("[EndGameScreenController] URL do formulário de playtest não configurada.");
            return;
        }

        Application.OpenURL(playtestFormUrl);
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        Color bg = isVictory ? new Color(0.05f, 0.12f, 0.08f, 0.96f) : new Color(0.12f, 0.05f, 0.05f, 0.96f);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "EndGamePanel", bg);

        bool pt = IsPortuguese();
        string primaryLabel = isVictory
            ? (pt ? "Prosseguir" : "Continue")
            : (pt ? "Reiniciar fase" : "Restart stage");

        continueButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform,
            primaryLabel,
            new Vector2(0.4f, 0.35f), new Vector2(0.4f, 0.35f), new Vector2(-160f, -40f), new Vector2(160f, 40f));

        exitButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, pt ? "Abandonar" : "Quit",
            new Vector2(0.6f, 0.35f), new Vector2(0.6f, 0.35f), new Vector2(-160f, -40f), new Vector2(160f, 40f));

        creditsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, pt ? "Créditos" : "Credits",
            new Vector2(0.35f, 0.2f), new Vector2(0.35f, 0.2f), new Vector2(-160f, -40f), new Vector2(160f, 40f));

        feedbackButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, pt ? "Feedback Playtest" : "Playtest Feedback",
            new Vector2(0.65f, 0.2f), new Vector2(0.65f, 0.2f), new Vector2(-160f, -40f), new Vector2(160f, 40f));
    }

    private static Button FindButtonByNamePrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return null;

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            string name = button.gameObject.name;
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return button;
        }

        return null;
    }

    private static Button FindButtonByLabelKeyword(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return null;

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null
                && label.text.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return button;
        }

        return null;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null || string.IsNullOrEmpty(label))
            return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = label;
    }
}
