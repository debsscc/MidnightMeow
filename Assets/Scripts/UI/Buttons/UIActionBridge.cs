using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class UIActionBridge : MonoBehaviour 
{
    [Header("UI References (Opcional)")]
    public GameObject pauseMenuObject; 
    public CursorManager cursorManager;

    private bool _sceneLoadPending;

    private IEnumerator DelayedSceneLoad(float delay, Action loadAction)
    {
        if (_sceneLoadPending)
            yield break;

        _sceneLoadPending = true;
        yield return new WaitForSecondsRealtime(delay);

        loadAction?.Invoke();
        _sceneLoadPending = false;
    }

    private void BeginSceneTransition(string sceneName)
    {
        Time.timeScale = 1f;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TryBeginTransition(sceneName);
            return;
        }

        var flowManager = GetFlowManager();
        if (flowManager == null) return;

        switch (sceneName)
        {
            case "Menu2":
                flowManager.LoadMenu();
                break;
            case "Lobby":
                flowManager.LoadLobby();
                break;
            default:
                flowManager.LoadPhase(sceneName);
                break;
        }
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
        if (_sceneLoadPending || SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning)
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginSceneTransition(phaseName)));
    }

    public void LoadMenu()
    {
        if (_sceneLoadPending || SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning)
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginSceneTransition("Menu2")));
    }

    public void LoadLobby()
    {
        if (_sceneLoadPending || SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning)
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginSceneTransition("Lobby")));
    }

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
            cursorManager.SetDefaultCursor();
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuObject == null) return;
        Time.timeScale = 1f;
        DeactivateScreen(pauseMenuObject);
        if (cursorManager != null)
            cursorManager.SetGameplayCursor();
    }

    public void QuitGame()
    {
        if (_sceneLoadPending) return;

        StartCoroutine(DelayedSceneLoad(0.2f, () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }));
    }
}
