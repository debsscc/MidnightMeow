///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: GameManager to test pause state.
// ---------------------------------------------------------------- */

using System;
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
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
    }

private void Update()
{
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        // if is playing, pauses
        if (currentState == GameStates.Playing) 
        {
            Debug.Log("-> Chamando PauseGame()");
            PauseGame();
            return;
        }

        // if its paused, resumes
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

        if (buttonsManager != null)
            buttonsManager.ClosePauseMenu();
        else if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);
    }
}