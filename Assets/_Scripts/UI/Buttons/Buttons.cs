///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: Componente de UI que gerencia botões de telas de UI.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Buttons : MonoBehaviour
{
    public bool isPopUpActive = false;

    [Header("Reference Scenes")]
    [SerializeField] private string mainMenuScene = "Menu2";
    [SerializeField] public  GameObject pauseMenuObject;

    [SerializeField] public GameObject popUpQuitGame;
    [SerializeField] private string gameOverScene = "GameOver";

    private IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    public void QuitGame()
    {
        StartCoroutine(DelayedAction(0.2f, () =>
        {
            isPopUpActive = false;
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }));
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(DelayedAction(0.2f, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }));
    }

    public void LoadSceneByIndex(int sceneIndex)
    {
        StartCoroutine(DelayedAction(0.2f, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneIndex);
        }));
    }
    
    public void ReloadCurrentScene()
    {
        StartCoroutine(DelayedAction(0.2f, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }));
    }

    public void GoToMainMenu()
    {
        LoadScene(mainMenuScene);
    }

    public void OpenPauseMenu()
    {
        Time.timeScale = 0f; 
        ActivateScreen(pauseMenuObject);
    }

    public void ClosePauseMenu()
    {
        Time.timeScale = 1f;
        DeactivateScreen(pauseMenuObject);

        if (popUpQuitGame != null)
        {
            DeactivateScreen(popUpQuitGame);
        }
    }
    public void QuitGamePopUp()
    {
        isPopUpActive = true;
        ActivateScreen(popUpQuitGame);
    }
    public void CloseQuitGamePopUp()
    {
        isPopUpActive = false;
        DeactivateScreen(popUpQuitGame);
    }

    public void GoToGameOver()
    {
        LoadScene(gameOverScene);
    }

    public void ActivateScreen(GameObject screen)
    {
        screen.SetActive(true);
    }

    public void DeactivateScreen(GameObject screen)
    {
        screen.SetActive(false);
    }

    public void ToggleScreen(GameObject screenDesactivate, GameObject screenActivate)
    {
        screenDesactivate.SetActive(false);
        screenActivate.SetActive(true);
    }
}
