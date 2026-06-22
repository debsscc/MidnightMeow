///* ----------------------------------------------------------------
// ATUALIZADO EM: 17-02-2026
// REVISADO POR: Arquiteto de Sistemas
// DESCRIÇÃO: GameManager de Fase. Controla estados, pause e transições via GameFlowManager.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
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
    
    [Tooltip("Configurações de delay e cenas para vitória/derrota.")]
    [SerializeField] private GameConfig gameConfig;

    private GameStates currentState = GameStates.Playing;
    public GameStates CurrentState => currentState;

    public event Action<GameStates> OnGameStateChanged;

    [Header("Progression")]
    [Tooltip("Optional: reference to the global PlayerProgressionData SO. If left empty, will try ServiceLocator.")]
    [SerializeField] private PlayerProgressionData progressionData;

    private void Awake()
    {
        // Colisão Player x Enemy é gerida por PlayerDamageImmunity (passagem breve após dano).
    }

    private void Start()
    {
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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
        GameEvents.InvokePauseChanged(true);
        ShowPauseOverlay();
    }

    public void ResumeGame()
    {
        if (currentState != GameStates.Paused) return;

        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
        GameEvents.InvokePauseChanged(false);
        HidePauseOverlay();
    }

    /// <summary>Abre o canvas de pause sem alterar timeScale (multiplayer).</summary>
    public void ShowPauseOverlay()
    {
        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().ResetToDefault();

        if (sceneOverlayController == null)
            sceneOverlayController = FindFirstObjectByType<SceneOverlayController>();

        if (sceneOverlayController != null)
            sceneOverlayController.OpenOverlay(pauseOverlayId);
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(true);
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
