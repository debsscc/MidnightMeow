///* ----------------------------------------------------------------
// ATUALIZADO EM: 17-02-2026
// REVISADO POR: Arquiteto de Sistemas
// DESCRIÇÃO: GameManager de Fase. Controla estados, pause e transições via GameFlowManager.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameStates
{
    Playing,
    Paused,
    Victory,
    Defeat
}

public class GameManager2 : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Arraste o painel principal do Pause Menu aqui.")]
    [SerializeField] private GameObject pauseMenuObject;

    [Tooltip("Opcional: gerencia visibilidade do pause via SceneOverlayController.")]
    [SerializeField] private SceneOverlayController sceneOverlayController;

    [SerializeField] private string pauseOverlayId = "pause";

    [Header("Pause — countdown de resume (multiplayer)")]
    [SerializeField] private TMP_Text resumeCountdownText;
    
    [Tooltip("Configurações de delay e cenas para vitória/derrota.")]
    [SerializeField] private GameConfig gameConfig;

    private GameStates currentState = GameStates.Playing;
    public GameStates CurrentState => currentState;

    private TMP_Text _runtimeResumeCountdownText;
    private Coroutine _resumeCountdownRoutine;

    public bool IsResumeCountdownActive => _resumeCountdownRoutine != null;

    public event Action<GameStates> OnGameStateChanged;

    [Header("Progression")]
    [Tooltip("Optional: reference to the global PlayerProgressionData SO. If left empty, will try ServiceLocator.")]
    [SerializeField] private PlayerProgressionData progressionData;

    private void Awake()
    {
        ResolvePauseReferences();
    }

    private void Start()
    {
        ResolvePauseReferences();
        InitializePhase();
    }

    private void OnEnable()
    {
        GameEvents.OnNightEnded += HandleNightEnded;
        GameEvents.OnPlayerDefeated += HandlePlayerDefeated;
        GameEvents.OnPauseChanged += HandleExternalPauseChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnNightEnded -= HandleNightEnded;
        GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
        GameEvents.OnPauseChanged -= HandleExternalPauseChanged;
    }

    private void InitializePhase()
    {
        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        // Notify systems that the game is in playing state (not paused)
        GameEvents.InvokePauseChanged(false);
        
        if (sceneOverlayController == null)
            sceneOverlayController = FindFirstObjectByType<SceneOverlayController>();

        if (sceneOverlayController != null)
            sceneOverlayController.CloseAllOverlays();
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);

        if (ServiceLocator.HasService<CursorManager>())
        {
             ServiceLocator.GetService<CursorManager>().SetGameplayCursor();
        }
        else
        {
            Debug.LogWarning("GameManager2: CursorManager não encontrado no ServiceLocator.");
        }

        if (progressionData == null && ServiceLocator.HasService<PlayerProgressionData>())
        {
            progressionData = ServiceLocator.GetService<PlayerProgressionData>();
        }

        RoundMagiculaTracker.EnsureExists();
        RoundMagiculaTracker.Instance?.ResetRound();
    }

    private void Update()
    {
        bool keyboardPause = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadPause = GamepadInputUtility.Current != null
            && GamepadInputUtility.Current.startButton.wasPressedThisFrame;
        if (!gamepadPause && GenericControllerInput.HasHidFallbackDevice)
            gamepadPause = GenericControllerInput.WasPausePressedThisFrame();
        if (keyboardPause || gamepadPause)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (currentState == GameStates.Playing)
        {
            if (GameFlowOrchestrator.Instance != null)
            {
                GameFlowOrchestrator.Instance.RequestPause();
                return;
            }

            PauseGame();
        }
        else if (currentState == GameStates.Paused)
        {
            if (GameFlowOrchestrator.Instance != null)
            {
                GameFlowOrchestrator.Instance.RequestResume();
                return;
            }

            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (currentState != GameStates.Playing) return;

        currentState = GameStates.Paused;
        GameEvents.InvokePauseChanged(true);
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
        ShowPauseOverlay();
    }

    public void ResumeGame()
    {
        if (currentState != GameStates.Paused) return;
        if (_resumeCountdownRoutine != null) return;

        // Solo (mesmo com NGO host local): retoma na hora. Countdown só em MP real.
        if (GameSessionContext.IsSinglePlayer)
        {
            CompleteResumeImmediately();
            return;
        }

        BeginResumeCountdown();
    }

    public void BeginResumeCountdown()
    {
        if (currentState != GameStates.Paused) return;
        if (_resumeCountdownRoutine != null) return;

        _resumeCountdownRoutine = StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        HidePauseOverlay();

        for (int seconds = 3; seconds >= 1; seconds--)
        {
            ShowResumeCountdown(seconds);
            yield return new WaitForSecondsRealtime(1f);
        }

        ShowResumeCountdown(0);
        HideResumeCountdown();
        _resumeCountdownRoutine = null;

        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
        GameEvents.InvokePauseChanged(false);
    }

    private void CompleteResumeImmediately()
    {
        if (_resumeCountdownRoutine != null)
        {
            StopCoroutine(_resumeCountdownRoutine);
            _resumeCountdownRoutine = null;
        }

        if (currentState != GameStates.Paused)
            return;

        HideResumeCountdown();
        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
        GameEvents.InvokePauseChanged(false);
        HidePauseOverlay();
    }

    /// <summary>Abre o canvas de pause sem alterar timeScale (multiplayer).</summary>
    public void ShowPauseOverlay()
    {
        ResolvePauseReferences();

        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().ResetToDefault();

        if (sceneOverlayController == null)
            sceneOverlayController = FindFirstObjectByType<SceneOverlayController>();

        if (sceneOverlayController != null)
            sceneOverlayController.OpenOverlay(pauseOverlayId);
        else if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(true);
            GameplayHudController.BringOverlayToFront(pauseMenuObject.transform);
        }

        RefreshResumeButtonInteractable();
    }

    /// <summary>Exibe contagem regressiva 3→1 antes de retomar (multiplayer).</summary>
    public void ShowResumeCountdown(int seconds)
    {
        if (seconds > 0)
            HidePauseOverlay();

        TMP_Text label = ResolveResumeCountdownText();
        if (label == null)
            return;

        if (seconds <= 0)
        {
            label.gameObject.SetActive(false);
            RefreshResumeButtonInteractable();
            return;
        }

        EnsureResumeCountdownVisible(label);
        label.gameObject.SetActive(true);
        label.text = seconds.ToString();
        RefreshResumeButtonInteractable();
    }

    public void HideResumeCountdown()
    {
        TMP_Text label = ResolveResumeCountdownText();
        if (label != null)
            label.gameObject.SetActive(false);

        RefreshResumeButtonInteractable();
    }

    private void EnsurePauseOverlayVisible()
    {
        ResolvePauseReferences();

        if (sceneOverlayController != null)
            sceneOverlayController.OpenOverlay(pauseOverlayId);
        else if (pauseMenuObject != null)
        {
            if (!pauseMenuObject.activeSelf)
                pauseMenuObject.SetActive(true);
            GameplayHudController.BringOverlayToFront(pauseMenuObject.transform);
        }
    }

    private void ResolvePauseReferences()
    {
        if (sceneOverlayController == null)
            sceneOverlayController = FindFirstObjectByType<SceneOverlayController>();

        if (pauseMenuObject != null)
            return;

        PauseMenuActions pauseActions = FindFirstObjectByType<PauseMenuActions>(FindObjectsInactive.Include);
        if (pauseActions != null)
        {
            pauseMenuObject = pauseActions.gameObject;
            return;
        }

        GameObject named = GameObject.Find("PauseMenu");
        if (named != null)
            pauseMenuObject = named;
    }

    private static void EnsureResumeCountdownVisible(TMP_Text label)
    {
        if (label == null)
            return;

        Transform countdownTransform = label.transform;
        if (!countdownTransform.gameObject.activeSelf)
            countdownTransform.gameObject.SetActive(true);

        countdownTransform.SetAsLastSibling();

        if (countdownTransform.parent is RectTransform parentRect)
        {
            RectTransform rt = countdownTransform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(240f, 160f);
            }

            GameplayHudController.BringOverlayToFront(parentRect);
        }
    }

    private TMP_Text ResolveResumeCountdownText()
    {
        if (resumeCountdownText != null)
            return resumeCountdownText;

        if (_runtimeResumeCountdownText != null)
            return _runtimeResumeCountdownText;

        GameplayHudController hud = FindFirstObjectByType<GameplayHudController>();
        Transform parent = hud != null ? hud.transform : null;
        if (parent == null)
        {
            ResolvePauseReferences();
            parent = pauseMenuObject != null ? pauseMenuObject.transform : null;
        }

        if (parent == null)
            return null;

        Transform existing = parent.Find("ResumeCountdown");
        if (existing != null)
        {
            _runtimeResumeCountdownText = existing.GetComponent<TMP_Text>();
            return _runtimeResumeCountdownText;
        }

        GameObject go = new GameObject("ResumeCountdown", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(240f, 160f);

        _runtimeResumeCountdownText = go.AddComponent<TextMeshProUGUI>();
        _runtimeResumeCountdownText.alignment = TextAlignmentOptions.Center;
        _runtimeResumeCountdownText.fontSize = 96f;
        _runtimeResumeCountdownText.color = Color.white;
        _runtimeResumeCountdownText.raycastTarget = false;
        GameplayUiFonts.Apply(_runtimeResumeCountdownText);
        _runtimeResumeCountdownText.gameObject.SetActive(false);
        return _runtimeResumeCountdownText;
    }

    private void RefreshResumeButtonInteractable()
    {
        PauseMenuActions pauseActions = pauseMenuObject != null
            ? pauseMenuObject.GetComponent<PauseMenuActions>()
            : FindFirstObjectByType<PauseMenuActions>();

        pauseActions?.RefreshResumeInteractable();
    }

    /// <summary>Fecha o canvas de pause sem alterar timeScale (multiplayer).</summary>
    public void HidePauseOverlay()
    {
        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().SetGameplayCursor();

        if (sceneOverlayController == null)
            sceneOverlayController = FindFirstObjectByType<SceneOverlayController>();

        if (sceneOverlayController != null)
            sceneOverlayController.CloseOverlay(pauseOverlayId);
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);
    }

    private void HandleExternalPauseChanged(bool paused)
    {
        if (paused)
        {
            if (currentState != GameStates.Playing)
                return;

            currentState = GameStates.Paused;
            OnGameStateChanged?.Invoke(currentState);
            ShowPauseOverlay();
            return;
        }

        if (currentState != GameStates.Paused)
            return;

        if (_resumeCountdownRoutine != null)
            return;

        currentState = GameStates.Playing;
        OnGameStateChanged?.Invoke(currentState);
        HidePauseOverlay();
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        RoundMagiculaTracker.Instance?.ResetRound();
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (ServiceLocator.HasService<GameFlowManager>())
        {
            var flowManager = ServiceLocator.GetService<GameFlowManager>();
            flowManager.LoadPhase(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
    }

    private void HandleNightEnded()
    {
        if (!ShouldHandleLocalEndGameSequence())
            return;

        StartCoroutine(HandleEndGameSequence(true));
    }

    private void HandlePlayerDefeated()
    {
        if (!ShouldHandleLocalEndGameSequence())
            return;

        StartCoroutine(HandleEndGameSequence(false));
    }

    private static bool ShouldHandleLocalEndGameSequence()
    {
        if (MultiplayerGameManager.Instance == null)
            return true;

        Unity.Netcode.NetworkManager net = Unity.Netcode.NetworkManager.Singleton;
        return net == null || !net.IsListening;
    }

    private IEnumerator HandleEndGameSequence(bool isVictory)
    {
        currentState = isVictory ? GameStates.Victory : GameStates.Defeat;
        
        float delay = 2f;
        string sceneToLoad = string.Empty;

        if (gameConfig != null)
        {
            delay = isVictory ? gameConfig.victoryDelay : 0f;
            sceneToLoad = isVictory ? gameConfig.victorySceneName : gameConfig.defeatSceneName;
        }
        else if (isVictory)
        {
            delay = 2f;
        }

        yield return new WaitForSecondsRealtime(delay);

        RoundMagiculaTracker tracker = RoundMagiculaTracker.Instance;
        if (tracker != null)
        {
            if (progressionData != null && tracker.RoundTotal > 0)
                progressionData.AddScience(tracker.RoundTotal);

            tracker.CommitToSave();
        }

        if (isVictory)
            SaveProfileStore.Instance?.MarkActiveContractCompleted();

        GameSessionContext.ResetContractRound();

        if (isVictory ? ScreenFlowStateMachine.ShowVictoryScreen() : ScreenFlowStateMachine.ShowDefeatScreen())
        {
            Debug.Log($"GameManager2: Indo para tela de {(isVictory ? "vitória" : "derrota")}.");
            yield break;
        }

        string fallbackScene = string.IsNullOrEmpty(sceneToLoad)
            ? (isVictory ? "VictoryScene" : "GameOver")
            : sceneToLoad;
        if (ScreenFlowController.Instance != null)
        {
            Debug.Log($"GameManager2: Fallback para '{fallbackScene}' após {(isVictory ? "vitória" : "derrota")}.");
            ScreenFlowController.Instance.TryBeginTransition(fallbackScene);
            yield break;
        }

        if (ServiceLocator.HasService<GameFlowManager>())
        {
            ServiceLocator.GetService<GameFlowManager>().LoadPhase(fallbackScene);
            yield break;
        }

        Debug.LogWarning($"GameManager2: carregando '{fallbackScene}' diretamente (sem ScreenFlowController).");
        SceneManager.LoadScene(fallbackScene);
    }
}
