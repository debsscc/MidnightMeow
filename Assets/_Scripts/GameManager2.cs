///* ----------------------------------------------------------------
// ATUALIZADO EM: 17-02-2026
// REVISADO POR: Arquiteto de Sistemas
// DESCRIÇÃO: GameManager de Fase. Controla estados, pause e transições via GameFlowManager.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    
    [Tooltip("Configurações de delay e cenas para vitória/derrota.")]
    [SerializeField] private GameConfig gameConfig;

    private GameStates currentState = GameStates.Playing;
    public GameStates CurrentState => currentState;

    public event Action<GameStates> OnGameStateChanged;

    [Header("Progression")]
    [Tooltip("Optional: reference to the global PlayerProgressionData SO. If left empty, will try ServiceLocator.")]
    [SerializeField] private PlayerProgressionData progressionData;

    // Temporary accumulator for collected ciencia during the current run
    private int _tempCollectedScience = 0;

    private void Awake()
    {
        // Opcional: Registrar no Service Locator se outras classes da fase precisarem ler o CurrentState.
        // ServiceLocator.RegisterService<GameManager2>(this);
    }

    private void Start()
    {
        // Inicializa o estado jogável e cursores logo no início da cena
        InitializePhase();
    }

    private void OnEnable()
    {
        GameEvents.OnNightEnded += HandleNightEnded;
        GameEvents.OnPlayerDefeated += HandlePlayerDefeated;
        GameEvents.OnCienciaCollected += HandleCienciaCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnNightEnded -= HandleNightEnded;
        GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
        GameEvents.OnCienciaCollected -= HandleCienciaCollected;
        
        // ServiceLocator.UnregisterService<GameManager2>(); // Descomente se registrar no Awake
    }

    private void InitializePhase()
    {
        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }

        // Recupera o serviço de cursor globalmente
        if (ServiceLocator.HasService<CursorManager>())
        {
             ServiceLocator.GetService<CursorManager>().SetGameplayCursor();
        }
        else
        {
            Debug.LogWarning("GameManager2: CursorManager não encontrado no ServiceLocator.");
        }

        // Resolve progression data from ServiceLocator if unset
        if (progressionData == null && ServiceLocator.HasService<PlayerProgressionData>())
        {
            progressionData = ServiceLocator.GetService<PlayerProgressionData>();
        }
    }

    private void Update()
    {
        // Captura o botão Esc via novo Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (currentState == GameStates.Playing) 
        {
            PauseGame();
        }
        else if (currentState == GameStates.Paused) 
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (currentState != GameStates.Playing) return;

        currentState = GameStates.Paused;
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
        
        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().ResetToDefault();
        
        if (pauseMenuObject != null)
            pauseMenuObject.SetActive(true);
    }

    public void ResumeGame()
    {
        if (currentState != GameStates.Paused) return;

        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);

        if (ServiceLocator.HasService<CursorManager>())
            ServiceLocator.GetService<CursorManager>().SetGameplayCursor();

        if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);
    }

    private void HandleNightEnded()
    {
        StartCoroutine(HandleEndGameSequence(true));
    }

    private void HandlePlayerDefeated()
    {
        StartCoroutine(HandleEndGameSequence(false));
    }

    /// <summary>
    /// Corrotina unificada para lidar com o fim do jogo (Vitória ou Derrota).
    /// </summary>
    private IEnumerator HandleEndGameSequence(bool isVictory)
    {
        // Atualiza o estado para evitar interações (como Pausar o jogo enquanto morre)
        currentState = isVictory ? GameStates.Victory : GameStates.Defeat;
        
        float delay = 2f;
        string sceneToLoad = string.Empty;

        if (gameConfig != null)
        {
            delay = isVictory ? gameConfig.victoryDelay : gameConfig.defeatDelay;
            sceneToLoad = isVictory ? gameConfig.victorySceneName : gameConfig.defeatSceneName;
        }

        yield return new WaitForSecondsRealtime(delay);

        // Commit collected ciencia to global progression data before leaving the phase
        if (progressionData != null && _tempCollectedScience > 0)
        {
            progressionData.AddScience(_tempCollectedScience);
            _tempCollectedScience = 0;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Utiliza nossa ponte arquitetural para transição de cenas
            if (ServiceLocator.HasService<GameFlowManager>())
            {
                var flowManager = ServiceLocator.GetService<GameFlowManager>();
                flowManager.LoadPhase(sceneToLoad);
            }
            else
            {
                Debug.LogError("GameManager2: GameFlowManager não registrado. Impossível trocar de cena.");
            }
        }
        else
        {
            Debug.LogWarning("GameManager2: Nome da cena de destino vazio no GameConfig.");
        }
    }

    private void HandleCienciaCollected(int amount)
    {
        if (amount <= 0) return;
        _tempCollectedScience += amount;
        Debug.Log($"GameManager2: Ciencia collected +{amount}. Temp total: {_tempCollectedScience}");
    }
}