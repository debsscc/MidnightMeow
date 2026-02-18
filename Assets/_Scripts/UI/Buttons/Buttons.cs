///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: Componente de UI que gerencia botões de telas de UI.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.SceneManagement;

public class Buttons : MonoBehaviour
{
    public bool isPopUpActive = false;

    [Header("Reference Scenes")]
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] public  GameObject pauseMenuObject;

    [SerializeField] public GameObject popUpQuitGame;
    [SerializeField] private string gameOverScene = "GameOver";

    //-------Quit Application-------//
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // editor mode
        #else
            Application.Quit(); // build final
        #endif
        isPopUpActive = false;
    }

    //-------Scene Management-------//
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
    
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    //-----PopUps----------//
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