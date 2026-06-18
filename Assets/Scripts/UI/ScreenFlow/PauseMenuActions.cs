using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ações do menu de pause. Solo: reiniciar fase. MP: reiniciar desabilitado.
/// Abandonar usa o mesmo fluxo de vitória/derrota (ExitToMainMenu).
/// </summary>
[DisallowMultipleComponent]
public class PauseMenuActions : MonoBehaviour
{
    [SerializeField] private GameObject quitConfirmationRoot;
    [SerializeField] private GameObject controlsPanel;

    private Button _resumeButton;
    private Button _restartButton;
    private Button _abandonButton;
    private Button _controlsButton;
    private Button _quitAppButton;
    private Button _confirmQuitAppButton;
    private Button _cancelQuitAppButton;
    private Button _creditsButton;
    private Button _controlsBackButton;

    private Transform _backgroundRoot;
    private readonly List<GameObject> _pauseMainContent = new List<GameObject>();

    private void Awake()
    {
        if (quitConfirmationRoot == null)
            quitConfirmationRoot = transform.Find("Background_PopUp")?.gameObject;

        ResolveButtons();
        EnsureCreditsButtonIfMissing();
        WireButtons();
        HideQuitConfirmation();
    }

    private void OnEnable()
    {
        HideQuitConfirmation();
        HideControls();
        RefreshForCurrentMode();
    }

    public void ClosePauseMenu()
    {
        if (GameFlowOrchestrator.Instance != null)
        {
            GameFlowOrchestrator.Instance.RequestResume();
            return;
        }

        UIActionBridge bridge = FindFirstObjectByType<UIActionBridge>();
        if (bridge != null)
        {
            bridge.ClosePauseMenu();
            return;
        }

        FindFirstObjectByType<GameManager2>()?.ResumeGame();
    }

    public void ShowCredits()
    {
        CreditsOverlayController.OpenFromPause();
    }

    public void RestartCurrentPhase()
    {
        if (!GameSessionContext.IsSinglePlayer)
            return;

        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        Time.timeScale = 1f;
        GameFlowOrchestrator.Instance?.NotifyPauseChanged(false);
        ScreenFlowStateMachine.RestartCurrentGameplay();
    }

    public void AbandonRun()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        Time.timeScale = 1f;
        GameFlowOrchestrator.Instance?.NotifyPauseChanged(false);
        ScreenFlowStateMachine.ExitToMainMenu();
    }

    public void ShowQuitConfirmation()
    {
        if (quitConfirmationRoot != null)
            quitConfirmationRoot.SetActive(true);
    }

    public void HideQuitConfirmation()
    {
        if (quitConfirmationRoot != null)
            quitConfirmationRoot.SetActive(false);
    }

    public void QuitApplication()
    {
        Time.timeScale = 1f;
        HideQuitConfirmation();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowControls()
    {
        GameObject panel = ResolveControlsPanel();
        SetPauseMainContentVisible(false);

        if (panel != null)
            panel.SetActive(true);
    }

    public void HideControls()
    {
        GameObject panel = ResolveControlsPanel();
        if (panel != null)
            panel.SetActive(false);

        SetPauseMainContentVisible(true);
    }

    /// <summary>Legado — redireciona para <see cref="RestartCurrentPhase"/>.</summary>
    public void ReloadCurrentScene() => RestartCurrentPhase();

    /// <summary>Legado — redireciona para <see cref="QuitApplication"/>.</summary>
    public void QuitGame() => ShowQuitConfirmation();

    private void ResolveButtons()
    {
        _resumeButton = FindButton("Resume");
        _restartButton = FindButton("Replay");
        _abandonButton = FindButton("Menu");
        _controlsButton = FindButton("Config");
        _creditsButton = FindCreditsButton();
        _quitAppButton = FindQuitAppEntryButton();
        _confirmQuitAppButton = FindConfirmQuitButton();
        _cancelQuitAppButton = FindButton("Don'tQuit");
        _controlsBackButton = FindControlsBackButton();
    }

    private void WireButtons()
    {
        Bind(_resumeButton, ClosePauseMenu);
        Bind(_restartButton, RestartCurrentPhase);
        Bind(_abandonButton, AbandonRun);
        Bind(_controlsButton, ShowControls);
        Bind(_creditsButton, ShowCredits);
        Bind(_quitAppButton, ShowQuitConfirmation);
        Bind(_confirmQuitAppButton, QuitApplication);
        Bind(_cancelQuitAppButton, HideQuitConfirmation);
        Bind(_controlsBackButton, HideControls);

        SetButtonLabel(_restartButton, "Reiniciar fase");
        SetButtonLabel(_abandonButton, "Abandonar");
        SetButtonLabel(_creditsButton, "Créditos");
    }

    private void EnsureCreditsButtonIfMissing()
    {
        if (_creditsButton != null)
            return;

        _creditsButton = FindCreditsButton();
        if (_creditsButton != null)
            return;

        Transform buttons1 = transform.Find("Background/Buttons/Buttons1");
        if (buttons1 == null)
            return;

        Button template = _controlsButton != null ? _controlsButton : _abandonButton;
        if (template == null)
            return;

        RectTransform templateRt = template.GetComponent<RectTransform>();
        Vector2 size = templateRt.sizeDelta;
        float gap = 12f;
        Vector2 shift = new Vector2(0f, -size.y - gap);

        _creditsButton = ScreenFlowPlaceholderFactory.CreateButton(buttons1, "Credits",
            templateRt.anchorMin, templateRt.anchorMax,
            templateRt.offsetMin + shift,
            templateRt.offsetMax + shift);
    }

    private Button FindCreditsButton()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null)
                continue;

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
               && value.IndexOf("crédito", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshForCurrentMode()
    {
        bool solo = GameSessionContext.IsSinglePlayer;

        if (_restartButton != null)
        {
            _restartButton.interactable = solo;
            _restartButton.gameObject.SetActive(solo);
        }
    }

    private Button FindQuitAppEntryButton()
    {
        Transform buttons2 = transform.Find("Buttons2");
        if (buttons2 == null)
            return null;

        foreach (Button button in buttons2.GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name == "Quit")
                return button;
        }

        return null;
    }

    private Button FindConfirmQuitButton()
    {
        Transform popup = transform.Find("Background_PopUp/QuitPopUp");
        if (popup == null)
            return null;

        foreach (Button button in popup.GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name == "Quit")
                return button;
        }

        return null;
    }

    private Button FindButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name == objectName)
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

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null || string.IsNullOrEmpty(label))
            return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = label;
    }

    private Button FindControlsBackButton()
    {
        GameObject panel = ResolveControlsPanel();
        if (panel == null)
            return null;

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name == "Back")
                return button;
        }

        return null;
    }

    private void CachePauseMainContent()
    {
        if (_backgroundRoot != null)
            return;

        Transform background = transform.Find("Background");
        if (background == null)
            return;

        _backgroundRoot = background;
        GameObject controls = ResolveControlsPanel();

        for (int i = 0; i < background.childCount; i++)
        {
            Transform child = background.GetChild(i);
            if (child == null || (controls != null && child.gameObject == controls))
                continue;

            _pauseMainContent.Add(child.gameObject);
        }
    }

    private void SetPauseMainContentVisible(bool visible)
    {
        CachePauseMainContent();

        for (int i = 0; i < _pauseMainContent.Count; i++)
        {
            GameObject content = _pauseMainContent[i];
            if (content != null)
                content.SetActive(visible);
        }
    }

    private GameObject ResolveControlsPanel()
    {
        if (controlsPanel != null)
            return controlsPanel;

        Transform background = transform.Find("Background");
        if (background != null)
        {
            Transform controls = background.Find("Controls");
            if (controls != null)
            {
                controlsPanel = controls.gameObject;
                return controlsPanel;
            }
        }

        GameObject found = GameObject.Find("Controls");
        if (found != null)
            controlsPanel = found;

        return controlsPanel;
    }
}
