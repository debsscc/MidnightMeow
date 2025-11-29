// GameManager.cs
// /*----------------------------------------------
// Creation Date: 2025-11-09 21:59
// Author: Debs S Carvalho
// ----------------------------------------------*/

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Prepping,
    Playing,
    Paused,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private HouseController house;
    //[SerializeField] private EnemySpawner spawner;  

    [Header("Settings")]
    [SerializeField] private float endDelay = 2f;

    private GameState currentState = GameState.Prepping;
    public GameState CurrentState => currentState;

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        HouseController.OnHouseDestroyed += HandleHouseDestroyed;
    }

    private void OnDisable()
    {
        HouseController.OnHouseDestroyed -= HandleHouseDestroyed;
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (currentState == GameState.Playing) return;

        currentState = GameState.Playing;
        OnGameStateChanged?.Invoke(currentState);

        //if (spawner != null) spawner.BeginSpawning();
        Debug.Log("[GameManager] Game Started");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
    }

    public void HandleHouseDestroyed()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Defeat;
        OnGameStateChanged?.Invoke(currentState);

        //if (spawner != null) spawner.StopSpawning();
        Debug.Log("[GameManager] Defeat - house destroyed");

        Invoke(nameof(RestartOrShowUI), endDelay);
    }

    public void HandleVictory()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Victory;
        OnGameStateChanged?.Invoke(currentState);

        //if (spawner != null) spawner.StopSpawning();
        Debug.Log("[GameManager] Victory!");

        Invoke(nameof(RestartOrShowUI), endDelay);
    }

    private void RestartOrShowUI()
    {
        // placeholder: reinicia a cena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ForceDefeat() => HandleHouseDestroyed();
    public void ForceVictory() => HandleVictory();
}
