using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class UIActionBridge : MonoBehaviour
{
    [Header("UI References (Opcional)")]
    public GameObject pauseMenuObject;
    public CursorManager cursorManager;

    [Header("Screen Flow (preferir ScreenFlowRequest no botão)")]
    [SerializeField] private string lobbyRouteId = SceneFlowRouteIds.MenuToLobby;
    [SerializeField] private string menuRouteId = SceneFlowRouteIds.ReturnToMenu;

    [Header("Overlay (opcional — preferir SceneOverlayRequest)")]
    [SerializeField] private SceneOverlayController overlayController;
    [SerializeField] private string pauseOverlayId = "pause";

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

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.TryBeginTransition(sceneName))
            return;

        GameFlowManager flowManager = GetFlowManager();
        if (flowManager == null)
            return;

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

    private void BeginRoute(string routeId, string sceneFallback)
    {
        Time.timeScale = 1f;

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.RequestRoute(routeId))
            return;

        if (!string.IsNullOrEmpty(sceneFallback))
            BeginSceneTransition(sceneFallback);
    }

    private GameFlowManager GetFlowManager()
    {
        if (ServiceLocator.HasService<GameFlowManager>())
            return ServiceLocator.GetService<GameFlowManager>();

        GameFlowManager fallback = FindFirstObjectByType<GameFlowManager>();
        if (fallback != null)
        {
            ServiceLocator.RegisterService<GameFlowManager>(fallback);
            return fallback;
        }

        Debug.LogError("UIActionBridge: GameFlowManager não encontrado.");
        return null;
    }

    private bool IsBlocked()
    {
        return _sceneLoadPending
            || (ScreenFlowController.Instance != null && ScreenFlowController.Instance.IsTransitioning);
    }

    public void LoadPhase(string phaseName)
    {
        if (IsBlocked())
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginSceneTransition(phaseName)));
    }

    public void LoadMenu()
    {
        if (IsBlocked())
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginRoute(menuRouteId, "Menu2")));
    }

    public void LoadLobby()
    {
        if (IsBlocked())
            return;

        StartCoroutine(DelayedSceneLoad(0.2f, () => BeginRoute(lobbyRouteId, "Lobby")));
    }

    public void ActivateScreen(GameObject screen) => screen.SetActive(true);
    public void DeactivateScreen(GameObject screen) => screen.SetActive(false);

    /// <summary>Abre o painel de controles e esconde Opções (Menu2).</summary>
    public void OpenControlsFromSettings()
    {
        ControlsPanelController panel = ControlsPanelController.FindInScene();
        if (panel == null)
        {
            Debug.LogWarning("UIActionBridge: ControlsPanelController não encontrado na cena.");
            return;
        }

        GameObject settings = GameObject.Find("Setings");
        panel.ShowFrom(settings);
    }

    /// <summary>Abre o painel de saves a partir de Opções (Menu2).</summary>
    public void OpenSaveFromSettings()
    {
        GameObject settings = GameObject.Find("Setings");
        ContinueSavePanelController savePanel = FindFirstObjectByType<ContinueSavePanelController>();
        if (savePanel != null)
        {
            savePanel.OpenFromSettings(settings);
            return;
        }

        if (settings != null)
            settings.SetActive(false);

        GameObject saveRoot = GameObject.Find("Save");
        if (saveRoot != null && saveRoot.GetComponent<Button>() == null)
            saveRoot.SetActive(true);
    }

    public void ToggleScreen(GameObject screenDesactivate, GameObject screenActivate)
    {
        screenDesactivate.SetActive(false);
        screenActivate.SetActive(true);
    }

    public void OpenPauseMenu()
    {
        if (overlayController != null)
        {
            overlayController.OpenOverlay(pauseOverlayId);
            if (cursorManager != null)
                cursorManager.SetDefaultCursor();
            return;
        }

        if (pauseMenuObject == null)
            return;

        Time.timeScale = 0f;
        ActivateScreen(pauseMenuObject);
        if (cursorManager != null)
            cursorManager.SetDefaultCursor();
    }

    public void ClosePauseMenu()
    {
        if (overlayController != null)
        {
            overlayController.CloseOverlay(pauseOverlayId);
            if (cursorManager != null)
                cursorManager.SetGameplayCursor();
            return;
        }

        if (pauseMenuObject == null)
            return;

        Time.timeScale = 1f;
        DeactivateScreen(pauseMenuObject);
        if (cursorManager != null)
            cursorManager.SetGameplayCursor();
    }

    public void QuitGame()
    {
        if (_sceneLoadPending)
            return;

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
