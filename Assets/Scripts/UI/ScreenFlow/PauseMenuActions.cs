using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ações do menu de pause. Substitui referências legadas à classe Buttons.
/// </summary>
[DisallowMultipleComponent]
public class PauseMenuActions : MonoBehaviour
{
    [SerializeField] private UIActionBridge uiActionBridge;
    [SerializeField] private GameManager2 gameManager;

    public void ClosePauseMenu()
    {
        if (GameFlowOrchestrator.Instance != null)
        {
            GameFlowOrchestrator.Instance.RequestResume();
            return;
        }

        if (uiActionBridge != null)
        {
            uiActionBridge.ClosePauseMenu();
            return;
        }

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager2>();

        gameManager?.ResumeGame();
    }

    public void ReloadCurrentScene()
    {
        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.LockTransitions(1f);

        Time.timeScale = 1f;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager2>();

        if (gameManager != null)
        {
            gameManager.RestartCurrentScene();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        if (uiActionBridge == null)
            uiActionBridge = FindFirstObjectByType<UIActionBridge>();

        if (uiActionBridge != null)
        {
            uiActionBridge.QuitGame();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
