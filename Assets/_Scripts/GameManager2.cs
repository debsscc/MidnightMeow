///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: GameManager to test pause state.
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
    public static GameManager2 Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject pauseMenuObject;
    private Buttons buttonsManager; 
    [SerializeField] private GameConfig gameConfig;

    private GameStates currentState = GameStates.Playing;
    public GameStates CurrentState => currentState;

    public event Action<GameStates> OnGameStateChanged;

    private void Awake()
{    
    Instance = this;
    
    buttonsManager = FindObjectOfType<Buttons>();
    if (pauseMenuObject == null) 
        pauseMenuObject = GameObject.Find("PauseMenu");
}


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.OnNightEnded += HandleNightEnded;
        GameEvents.OnPlayerDefeated += HandlePlayerDefeated;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.OnNightEnded -= HandleNightEnded;
        GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        
        GameObject foundPauseMenu = GameObject.Find("PauseMenu");
        
        if (foundPauseMenu != null)
        {
            pauseMenuObject = foundPauseMenu;
            pauseMenuObject.SetActive(false);
            
            buttonsManager = FindObjectOfType<Buttons>();
            
            if (buttonsManager == null)
            {
                Debug.LogWarning("Script 'Buttons' não encontrado na cena!");
            }
        }

        if (CursorManager.Instance != null)
        {
            if (scene.name == "Fase-1")
            {
                CursorManager.Instance.SetGameplayCursor();
            }
            else
            {
                CursorManager.Instance.ResetToDefault();
            }
        }
    }

private void Update()
{
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        if (currentState == GameStates.Playing) 
        {
            Debug.Log("-> Chamando PauseGame()");
            PauseGame();
            return;
        }

        if (currentState == GameStates.Paused) 
        {
            bool popUpAtivo = false;

            if (buttonsManager != null && buttonsManager.popUpQuitGame != null)
            {
                popUpAtivo = buttonsManager.popUpQuitGame.activeInHierarchy;
            }

            if (popUpAtivo)
            {
                return;
            }
            ResumeGame();
        }
    }
}

    public void PauseGame()
    {
        if (currentState != GameStates.Playing) return;

        currentState = GameStates.Paused;
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
        
        if (CursorManager.Instance != null)
            CursorManager.Instance.ResetToDefault();
        
        if (buttonsManager != null)
            buttonsManager.OpenPauseMenu();
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(true);
    }

    public void ResumeGame()
    {
        if (currentState != GameStates.Paused) return;

        currentState = GameStates.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);

        if (CursorManager.Instance != null)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == "Fase-1")
                CursorManager.Instance.SetGameplayCursor();
        }

        if (buttonsManager != null)
            buttonsManager.ClosePauseMenu();
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);
    }

    private void HandleNightEnded()
    {
        StartCoroutine(HandleVictorySequence());
    }

    private void HandlePlayerDefeated()
    {
        StartCoroutine(HandleDefeatSequence());
    }

    private IEnumerator HandleVictorySequence()
    {
        float delay = gameConfig != null ? gameConfig.victoryDelay : 2f;
        yield return new WaitForSecondsRealtime(delay);

        if (gameConfig != null && !string.IsNullOrEmpty(gameConfig.victorySceneName))
        {
            SceneManager.LoadScene(gameConfig.victorySceneName);
        }
        else
        {
            Debug.LogWarning("GameManager2: GameConfig or victorySceneName not set. Cannot load victory scene.");
        }
    }

    private IEnumerator HandleDefeatSequence()
    {
        float delay = gameConfig != null ? gameConfig.defeatDelay : 2f;
        yield return new WaitForSecondsRealtime(delay);

        if (gameConfig != null && !string.IsNullOrEmpty(gameConfig.defeatSceneName))
        {
            SceneManager.LoadScene(gameConfig.defeatSceneName);
        }
        else
        {
            Debug.LogWarning("GameManager2: GameConfig or defeatSceneName not set. Cannot load defeat scene.");
        }
    }
}
