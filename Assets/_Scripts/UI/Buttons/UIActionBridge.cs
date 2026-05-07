using UnityEngine;
using System;
using System.Collections;

public class UIActionBridge : MonoBehaviour 
{
    [Header("UI References (Opcional)")]
    public GameObject pauseMenuObject; 
    public CursorManager cursorManager;

    private IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    private GameFlowManager GetFlowManager()
    {
        if (ServiceLocator.HasService<GameFlowManager>())
            return ServiceLocator.GetService<GameFlowManager>();

        var fallback = FindFirstObjectByType<GameFlowManager>();
        if (fallback != null)
        {
            ServiceLocator.RegisterService<GameFlowManager>(fallback);
            return fallback;
        }

        Debug.LogError("UIActionBridge: GameFlowManager não encontrado na cena nem no ServiceLocator.");
        return null;
    }

    public void LoadPhase(string phaseName)
    {
        StartCoroutine(DelayedAction(0.2f, () => 
        {
            Time.timeScale = 1f; 
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.ChangeScene(phaseName);
                return;
            }
            var flowManager = GetFlowManager();
            if (flowManager == null) return;
            flowManager.LoadPhase(phaseName);
        }));
    }

    public void LoadMenu()
    {
        StartCoroutine(DelayedAction(0.2f, () => 
        {
            Time.timeScale = 1f;
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.ChangeScene("Menu2");
                return;
            }
            var flowManager = GetFlowManager();
            if (flowManager == null) return;
            flowManager.LoadMenu();
        }));
    }

    public void LoadLobby()
    {
        StartCoroutine(DelayedAction(0.2f, () =>
        {
            Time.timeScale = 1f;
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.ChangeScene("Lobby");
                return;
            }

            var flowManager = GetFlowManager();
            if (flowManager == null) return;
            flowManager.LoadLobby();
        }));
    }

    // --- Métodos Restaurados ---
    public void ActivateScreen(GameObject screen) => screen.SetActive(true);
    public void DeactivateScreen(GameObject screen) => screen.SetActive(false);
    
    public void ToggleScreen(GameObject screenDesactivate, GameObject screenActivate)
    {
        screenDesactivate.SetActive(false);
        screenActivate.SetActive(true);
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuObject == null) return;
        Time.timeScale = 0f; 
        ActivateScreen(pauseMenuObject);
        if (cursorManager != null)
        {
            cursorManager.SetDefaultCursor();
        }
        
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuObject == null) return;
        Time.timeScale = 1f;
        DeactivateScreen(pauseMenuObject);
        if (cursorManager != null)
        {
            cursorManager.SetGameplayCursor();
        }
    }
    // ---------------------------

    public void QuitGame()
    {
        StartCoroutine(DelayedAction(0.2f, () => 
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }));
    }
}