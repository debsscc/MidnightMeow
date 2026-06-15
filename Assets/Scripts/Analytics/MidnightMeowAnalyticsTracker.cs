using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Escuta eventos do jogo e envia métricas para o GameAnalytics.
///Acumula stats da run local e manda resumo em vitória/derrota.
[DisallowMultipleComponent]
public class MidnightMeowAnalyticsTracker : MonoBehaviour
{
    private bool _runActive;
    private string _runScene = string.Empty;
    private float _runStartTime;
    private float _pauseStartTime;
    private float _totalPausedSeconds;
    private bool _isPaused;

    private int _enemiesKilled;
    private int _scienceCollected;
    private int _ammoCollected;
    private int _maxWaveReached;
    private int _playerDownCount;
    private int _lastReportedWave;

    private ScreenFlowController _screenFlow;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameEvents.OnWaveStatusChanged += HandleWaveStatusChanged;
        GameEvents.OnCienciaCollected += HandleCienciaCollected;
        GameEvents.OnAmmoCollected += HandleAmmoCollected;
        GameEvents.OnPlayerDefeated += HandlePlayerDefeated;
        GameEvents.OnNightEnded += HandleNightEnded;
        GameEvents.OnPauseChanged += HandlePauseChanged;
        GameEvents.OnAdrenalineLow += HandleAdrenalineLow;
        GameEvents.OnEnemyKilledByPlayer += HandleEnemyKilled;
        GameEvents.OnPlayerJoined += HandlePlayerJoined;
        GameEvents.OnPlayerLeft += HandlePlayerLeft;
        GameEvents.OnAllPlayersDefeated += HandleAllPlayersDefeated;
        MultiplayerGameManager.OnGameStateChanged += HandleMultiplayerGameStateChanged;

        TryBindScreenFlow();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindScreenFlow();

        GameEvents.OnWaveStatusChanged -= HandleWaveStatusChanged;
        GameEvents.OnCienciaCollected -= HandleCienciaCollected;
        GameEvents.OnAmmoCollected -= HandleAmmoCollected;
        GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
        GameEvents.OnNightEnded -= HandleNightEnded;
        GameEvents.OnPauseChanged -= HandlePauseChanged;
        GameEvents.OnAdrenalineLow -= HandleAdrenalineLow;
        GameEvents.OnEnemyKilledByPlayer -= HandleEnemyKilled;
        GameEvents.OnPlayerJoined -= HandlePlayerJoined;
        GameEvents.OnPlayerLeft -= HandlePlayerLeft;
        GameEvents.OnAllPlayersDefeated -= HandleAllPlayersDefeated;
        MultiplayerGameManager.OnGameStateChanged -= HandleMultiplayerGameStateChanged;
    }

    private void Update()
    {
        if (_screenFlow == null)
            TryBindScreenFlow();
    }

    private void TryBindScreenFlow()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null || flow == _screenFlow)
            return;

        UnbindScreenFlow();
        _screenFlow = flow;
        _screenFlow.OnTransitionStarted += HandleTransitionStarted;
        _screenFlow.OnTransitionCompleted += HandleTransitionCompleted;
    }

    private void UnbindScreenFlow()
    {
        if (_screenFlow == null)
            return;

        _screenFlow.OnTransitionStarted -= HandleTransitionStarted;
        _screenFlow.OnTransitionCompleted -= HandleTransitionCompleted;
        _screenFlow = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MidnightMeowAnalytics.TrackScreenArrived(scene.name);

        if (GameplaySceneBootstrap.IsGameplayScene(scene.name))
            BeginRun(scene.name);
    }

    private void HandleTransitionStarted(string sceneName)
    {
        MidnightMeowAnalytics.TrackScreenTransitionStarted(ResolvePendingRouteId(), sceneName);
    }

    private void HandleTransitionCompleted(string sceneName)
    {
        MidnightMeowAnalytics.TrackScreenArrived(sceneName, ResolvePendingRouteId());
    }

    private static string ResolvePendingRouteId()
    {
        return string.IsNullOrEmpty(GameSessionContext.PendingRouteId)
            ? ScreenFlowStateMachine.CurrentPhase.ToString()
            : GameSessionContext.PendingRouteId;
    }

    private void BeginRun(string sceneName)
    {
        ResetRunCounters();
        _runActive = true;
        _runScene = sceneName;
        _runStartTime = Time.time;

        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        MidnightMeowAnalytics.SetSessionMode(isMultiplayer);
        MidnightMeowAnalytics.TrackRunStart(sceneName);
        MidnightMeowAnalytics.TrackFlowPhase(ScreenFlowPhase.Gameplay);
    }

    private void ResetRunCounters()
    {
        _enemiesKilled = 0;
        _scienceCollected = 0;
        _ammoCollected = 0;
        _maxWaveReached = 0;
        _playerDownCount = 0;
        _lastReportedWave = 0;
        _totalPausedSeconds = 0f;
        _isPaused = false;
        _pauseStartTime = 0f;
    }

    private void HandleWaveStatusChanged(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        if (!_runActive || currentWave <= 0)
            return;

        _maxWaveReached = Mathf.Max(_maxWaveReached, currentWave);

        if (currentWave > _lastReportedWave)
        {
            MidnightMeowAnalytics.TrackWaveStarted(currentWave, totalWaves, _runScene);
            _lastReportedWave = currentWave;
        }

        if (enemiesRemaining <= 0 && currentWave > 0)
            MidnightMeowAnalytics.TrackWaveCompleted(currentWave, _runScene);
    }

    private void HandleCienciaCollected(int amount)
    {
        if (amount <= 0)
            return;

        _scienceCollected += amount;
    }

    private void HandleAmmoCollected()
    {
        _ammoCollected++;
    }

    private void HandlePlayerDefeated()
    {
        _playerDownCount++;
        MidnightMeowAnalytics.TrackPlayerDowned(_playerDownCount);

        // Solo: derrota encerra a run imediatamente (MP espera GameState.Defeat).
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            EndRun(victory: false);
    }

    private void HandleNightEnded()
    {
        EndRun(victory: true);
    }

    private void HandlePauseChanged(bool paused)
    {
        if (paused)
        {
            _isPaused = true;
            _pauseStartTime = Time.unscaledTime;
        }
        else if (_isPaused)
        {
            _totalPausedSeconds += Time.unscaledTime - _pauseStartTime;
            _isPaused = false;
        }

        MidnightMeowAnalytics.TrackPauseChanged(paused, _totalPausedSeconds);
    }

    private void HandleAdrenalineLow()
    {
        if (_runActive)
            MidnightMeowAnalytics.TrackAdrenalineLow();
    }

    private void HandleEnemyKilled(ulong killerClientId)
    {
        if (!IsLocalKill(killerClientId))
            return;

        _enemiesKilled++;
        MidnightMeowAnalytics.TrackEnemyKill(_enemiesKilled);
    }

    private static bool IsLocalKill(ulong killerClientId)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            return true;

        return killerClientId == network.LocalClientId || killerClientId == 0;
    }

    private void HandlePlayerJoined(ulong clientId, bool isLocalPlayer)
    {
        MidnightMeowAnalytics.TrackPlayerJoined(clientId, isLocalPlayer);
    }

    private void HandlePlayerLeft(ulong clientId)
    {
        MidnightMeowAnalytics.TrackPlayerLeft(clientId);
    }

    private void HandleAllPlayersDefeated()
    {
        MidnightMeowAnalytics.TrackAllPlayersDefeated();
        EndRun(victory: false);
    }

    private void HandleMultiplayerGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Victory:
                EndRun(victory: true);
                break;
            case GameState.Defeat:
                EndRun(victory: false);
                break;
        }
    }

    private void EndRun(bool victory)
    {
        if (!_runActive)
            return;

        // Em MP, derrota parcial (down) não encerra — só quando todos caem (GameState.Defeat).
        if (!victory && MultiplayerGameManager.Instance != null &&
            MultiplayerGameManager.Instance.PlayersAlive > 0)
            return;

        _runActive = false;

        if (_isPaused)
        {
            _totalPausedSeconds += Time.unscaledTime - _pauseStartTime;
            _isPaused = false;
        }

        float survival = Mathf.Max(0f, Time.time - _runStartTime - _totalPausedSeconds);
        MidnightMeowAnalytics.TrackRunEnd(
            _runScene,
            victory,
            survival,
            _enemiesKilled,
            _scienceCollected,
            _ammoCollected,
            _maxWaveReached,
            _playerDownCount,
            _totalPausedSeconds);
    }

    /// <summary>Chamado pela UI ao escolher contrato.</summary>
    public static void NotifyContractSelected(int contractIndex, string gameplayScene)
    {
        MidnightMeowAnalytics.TrackContractSelected(contractIndex, gameplayScene);
        MidnightMeowAnalytics.TrackFlowPhase(ScreenFlowPhase.ContractSelect);
    }

    /// <summary>Chamado pela UI ao clicar botões importantes.</summary>
    public static void NotifyUiClick(string screen, string action)
    {
        MidnightMeowAnalytics.TrackUiClick(screen, action);
    }
}
