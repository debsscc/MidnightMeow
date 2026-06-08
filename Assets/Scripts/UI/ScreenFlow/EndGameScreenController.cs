using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Telas de vitória e derrota: continuar para Preparation ou sair ao menu.
/// </summary>
[DisallowMultipleComponent]
public class EndGameScreenController : MonoBehaviour
{
    [SerializeField] private bool isVictory = true;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private void Awake()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene == "GameOver")
            isVictory = false;

        if (buildPlaceholderIfMissing && titleText == null)
            BuildPlaceholderUI();

        TryAutoResolveReferences();
        RewireLegacyMenuButton();

        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);
    }

    private void Start()
    {
        if (titleText != null)
            titleText.text = isVictory ? "Vitória!" : "Derrota";

        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
    }

    private void TryAutoResolveReferences()
    {
        if (continueButton == null)
            continueButton = ScreenFlowUiLookup.FindButton("Button_Continue") ?? ScreenFlowUiLookup.FindButton("Continue");
        if (exitButton == null)
            exitButton = ScreenFlowUiLookup.FindButton("Button_Menu") ?? ScreenFlowUiLookup.FindButton("Sair");
    }

    private void RewireLegacyMenuButton()
    {
        Button legacyMenu = ScreenFlowUiLookup.FindButton("Button_Menu");
        if (legacyMenu == null)
            return;

        legacyMenu.onClick.RemoveAllListeners();
        legacyMenu.onClick.AddListener(OnExit);

        if (exitButton == null)
            exitButton = legacyMenu;
    }

    private void OnContinue()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        PreparationSessionManager.Instance?.ResetRound();
        ScreenFlowStateMachine.ContinueAfterEndGame();
    }

    private void OnExit()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        ScreenFlowStateMachine.ExitToMainMenu();
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        Color bg = isVictory ? new Color(0.05f, 0.12f, 0.08f, 0.96f) : new Color(0.12f, 0.05f, 0.05f, 0.96f);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "EndGamePanel", bg);

        titleText = ScreenFlowPlaceholderFactory.CreateText(panel.transform,
            isVictory ? "Vitória!" : "Derrota", 72,
            TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(-400f, -60f), new Vector2(400f, 60f));

        continueButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Continuar",
            new Vector2(0.4f, 0.35f), new Vector2(0.4f, 0.35f), new Vector2(-160f, -40f), new Vector2(160f, 40f));

        exitButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Sair",
            new Vector2(0.6f, 0.35f), new Vector2(0.6f, 0.35f), new Vector2(-160f, -40f), new Vector2(160f, 40f));
    }
}
