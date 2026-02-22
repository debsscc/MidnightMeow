using UnityEngine;
using System;
using System.Collections;

public class UIActionBridge : MonoBehaviour 
{
    [Header("UI References (Opcional)")]
    public GameObject pauseMenuObject; // Adicionado de volta

    private IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    public void LoadPhase(string phaseName)
    {
        StartCoroutine(DelayedAction(0.2f, () => 
        {
            Time.timeScale = 1f; 
            var flowManager = ServiceLocator.GetService<GameFlowManager>();
            flowManager.LoadPhase(phaseName);
        }));
    }

    public void LoadMenu()
    {
        StartCoroutine(DelayedAction(0.2f, () => 
        {
            Time.timeScale = 1f;
            var flowManager = ServiceLocator.GetService<GameFlowManager>();
            flowManager.LoadMenu();
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
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuObject == null) return;
        Time.timeScale = 1f;
        DeactivateScreen(pauseMenuObject);
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