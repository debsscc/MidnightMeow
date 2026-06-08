using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Orquestra transições de cena, pause e operações sensíveis a corrida entre UI, rede e ScreenFlowController.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class GameFlowOrchestrator : MonoBehaviour
{
    public static GameFlowOrchestrator Instance { get; private set; }

    public event Action<bool> OnPauseOrchestrated;

    public bool IsTransitionLocked { get; private set; }
    public bool IsPauseActive { get; private set; }

    private float _transitionLockUntil;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            ServiceLocator.RegisterService(this);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"GameFlowOrchestrator: ServiceLocator: {ex.Message}");
        }
    }

    private void OnEnable()
    {
        if (ScreenFlowController.Instance != null)
        {
            ScreenFlowController.Instance.OnTransitionStarted += HandleTransitionStarted;
            ScreenFlowController.Instance.OnTransitionCompleted += HandleTransitionCompleted;
        }
    }

    private void Start()
    {
        if (ScreenFlowController.Instance != null)
        {
            ScreenFlowController.Instance.OnTransitionStarted += HandleTransitionStarted;
            ScreenFlowController.Instance.OnTransitionCompleted += HandleTransitionCompleted;
        }
    }

    private void OnDisable()
    {
        if (ScreenFlowController.Instance != null)
        {
            ScreenFlowController.Instance.OnTransitionStarted -= HandleTransitionStarted;
            ScreenFlowController.Instance.OnTransitionCompleted -= HandleTransitionCompleted;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsTransitionLocked && Time.unscaledTime >= _transitionLockUntil)
            IsTransitionLocked = false;
    }

    private void HandleTransitionStarted(string _)
    {
        LockTransitions(0.5f);
        CloseAllOverlaysSafe();
        Time.timeScale = 1f;
        IsPauseActive = false;
    }

    private void HandleTransitionCompleted(string _)
    {
        LockTransitions(0.25f);
    }

    public void LockTransitions(float seconds)
    {
        IsTransitionLocked = true;
        _transitionLockUntil = Time.unscaledTime + Mathf.Max(0.05f, seconds);
    }

    public bool CanRequestTransition()
    {
        if (IsTransitionLocked)
            return false;

        if (ScreenFlowController.Instance != null && ScreenFlowController.Instance.IsTransitioning)
            return false;

        return true;
    }

    public bool TryRequestRoute(string routeId)
    {
        if (!CanRequestTransition())
            return false;

        if (ScreenFlowController.Instance == null)
            return false;

        LockTransitions(0.35f);
        return ScreenFlowController.Instance.RequestRoute(routeId);
    }

    public void RequestPause()
    {
        if (IsPauseActive || IsTransitionLocked)
            return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            MultiplayerGameManager mp = MultiplayerGameManager.Instance;
            if (mp != null)
            {
                mp.RequestPauseRpc();
                return;
            }
        }

        GameManager2 local = FindFirstObjectByType<GameManager2>();
        if (local != null)
        {
            local.PauseGame();
            IsPauseActive = true;
            OnPauseOrchestrated?.Invoke(true);
            return;
        }

        SceneOverlayController overlay = FindFirstObjectByType<SceneOverlayController>();
        if (overlay != null)
        {
            overlay.OpenOverlay("pause");
            IsPauseActive = true;
            OnPauseOrchestrated?.Invoke(true);
        }
    }

    public void RequestResume()
    {
        if (!IsPauseActive)
            return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            MultiplayerGameManager mp = MultiplayerGameManager.Instance;
            if (mp != null)
            {
                mp.RequestResumeRpc();
                return;
            }
        }

        GameManager2 local = FindFirstObjectByType<GameManager2>();
        if (local != null)
        {
            local.ResumeGame();
            IsPauseActive = false;
            OnPauseOrchestrated?.Invoke(false);
            return;
        }

        SceneOverlayController overlay = FindFirstObjectByType<SceneOverlayController>();
        if (overlay != null)
        {
            overlay.CloseOverlay("pause");
            IsPauseActive = false;
            OnPauseOrchestrated?.Invoke(false);
        }
    }

    public void NotifyPauseChanged(bool paused)
    {
        IsPauseActive = paused;
        OnPauseOrchestrated?.Invoke(paused);
    }

    private static void CloseAllOverlaysSafe()
    {
        SceneOverlayController overlay = FindFirstObjectByType<SceneOverlayController>();
        overlay?.CloseAllOverlays();
    }
}
