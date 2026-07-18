/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: UI da cena Lobby — modos host/entrar/solo, pronto e transições de fluxo.
---------------------------------------------------------------- */

using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LobbySceneUIController : MonoBehaviour
{
    private enum LobbyUiMode
    {
        ModeSelect,
        HostWaiting,
        ClientJoin,
        SoloConfirm
    }

    private const int JoinCodeLength = 6;

    [Header("Conexao")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button charactersButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private TMP_Text insertCodeTitle;
    [SerializeField] private TMP_Text codeTitle;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Toggle readyToggle;

    [Header("Fluxo")]
    [SerializeField] private int requiredPlayersForLoading = 2;
    [SerializeField] private float readyShowDelaySeconds = 1.5f;
    [SerializeField] private bool autoResolveMissingRefs = true;

    [Header("Layout solo (painel direito)")]
    [SerializeField] private Vector2 soloStatusAnchoredPos = new Vector2(318f, -250f);
    [SerializeField] private Vector2 soloReadyAnchoredPos = new Vector2(309f, 40f);
    [SerializeField] private Vector2 soloDisconnectAnchoredPos = new Vector2(309f, -109f);

    private LobbyUiMode _mode = LobbyUiMode.ModeSelect;
    private bool _matchTransitionStarted;
    private bool _clientConnectInProgress;
    private bool _hostLeftReceived;
    private bool _hostStartInProgress;
    private bool _suppressDisconnectUi;
    private Coroutine _readyDelayCoroutine;
    private Vector2 _defaultStatusAnchoredPos;
    private Vector2 _defaultReadyAnchoredPos;
    private Vector2 _defaultDisconnectAnchoredPos;
    private bool _defaultLayoutCaptured;

    private void Awake()
    {
        if (autoResolveMissingRefs)
            TryAutoResolveReferences();

        ConfigureJoinCodeInput();

        if (hostButton != null) hostButton.onClick.AddListener(EnterHostMode);
        if (joinButton != null) joinButton.onClick.AddListener(EnterJoinMode);
        if (soloButton != null) soloButton.onClick.AddListener(EnterSoloMode);
        if (charactersButton != null) charactersButton.onClick.AddListener(OnBackOrCharactersClicked);
        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(CopyJoinCode);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);
        if (readyToggle != null)
        {
            readyToggle.transition = Selectable.Transition.None;
            readyToggle.toggleTransition = Toggle.ToggleTransition.None;
            readyToggle.onValueChanged.AddListener(OnReadyToggleChanged);
        }

        if (instructionText != null)
            instructionText.gameObject.SetActive(true);

        ApplyLobbyButtonFeedback();
    }

    private void ApplyLobbyButtonFeedback()
    {
        UiButtonFeedbackUtility.ApplyToScene(gameObject.scene);
        ApplyDisconnectIdleFade();
    }

    /// <summary>
    /// Desconectar fica um pouco fade no idle; no hover/seleção volta à opacidade total.
    /// Precisa rodar depois do <see cref="UiButtonFeedbackUtility"/> (que reforça Highlighted/Pressed).
    /// </summary>
    private void ApplyDisconnectIdleFade()
    {
        if (disconnectButton == null)
            return;

        ColorBlock colors = disconnectButton.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.55f);
        colors.highlightedColor = new Color(colors.highlightedColor.r, colors.highlightedColor.g, colors.highlightedColor.b, 1f);
        colors.selectedColor = new Color(colors.selectedColor.r, colors.selectedColor.g, colors.selectedColor.b, 1f);
        colors.pressedColor = new Color(colors.pressedColor.r, colors.pressedColor.g, colors.pressedColor.b, 1f);
        disconnectButton.colors = colors;
    }

    private void OnEnable()
    {
        ConnectionManager.OnHostLeftSession += HandleHostLeftSession;
        StartCoroutine(BindManagersRoutine());
    }

    private void Start()
    {
        ApplyLobbyButtonFeedback();

        if (GameSessionContext.AutoHostOnLobbyEnter)
            StartCoroutine(AutoContinueRoutine());

        HandleJoinCodeUpdated(ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty);
        ApplyViewMode();
        RefreshPlayerCount();
        ShowPendingConnectionMessageIfAny();
        ScreenFlowSceneReadiness.MarkReadyIfPending("Lobby");
    }

    private void ShowPendingConnectionMessageIfAny()
    {
        string pending = GameSessionContext.PendingConnectionMessage;
        if (string.IsNullOrEmpty(pending))
            return;

        GameSessionContext.PendingConnectionMessage = string.Empty;

        // Mostra o aviso amigável mesmo na seleção de modo (statusText fica oculto por padrão aí).
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            SetStatus(UiLocalization.TranslateLobbyConnectionMessage(pending));
        }
    }

    private void OnDisable()
    {
        ConnectionManager.OnHostLeftSession -= HandleHostLeftSession;
        StopAllCoroutines();
        _readyDelayCoroutine = null;
        UnbindManagers();
    }

    private void ConfigureJoinCodeInput()
    {
        if (joinCodeInput == null)
            return;

        joinCodeInput.characterLimit = JoinCodeLength;
        joinCodeInput.onValueChanged.AddListener(OnJoinCodeInputChanged);
    }

    private void TryAutoResolveReferences()
    {
        if (hostButton == null) hostButton = ScreenFlowUiLookup.FindButton("Host");
        if (joinButton == null) joinButton = ScreenFlowUiLookup.FindButton("Join");
        if (soloButton == null) soloButton = ScreenFlowUiLookup.FindButton("Solo_StartGame");
        if (disconnectButton == null) disconnectButton = ScreenFlowUiLookup.FindButton("Disconnect");
        if (copyCodeButton == null) copyCodeButton = ScreenFlowUiLookup.FindButton("CopyCode");
        if (charactersButton == null) charactersButton = ScreenFlowUiLookup.FindButton("Back");
        if (joinCodeText == null) joinCodeText = ScreenFlowUiLookup.FindText("JoinCodeDisplay");
        if (instructionText == null) instructionText = ScreenFlowUiLookup.FindText("ERROCODE");
        if (statusText == null) statusText = ScreenFlowUiLookup.FindText("Status");
        if (playersText == null) playersText = ScreenFlowUiLookup.FindText("PlayerCount");
        if (insertCodeTitle == null) insertCodeTitle = ScreenFlowUiLookup.FindText("Insert_code");
        if (codeTitle == null) codeTitle = ScreenFlowUiLookup.FindText("Code_Title");
        if (joinCodeInput == null) joinCodeInput = ScreenFlowUiLookup.FindInputField();
        if (readyToggle == null) readyToggle = FindReadyToggle();
    }

    private static Toggle FindReadyToggle()
    {
        Toggle[] toggles = Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] != null && toggles[i].gameObject.name == "Toggle")
                return toggles[i];
        }

        return null;
    }

    private IEnumerator BindManagersRoutine()
    {
        float timeout = 10f;
        float elapsed = 0f;
        while ((ConnectionManager.Instance == null || LobbySessionManager.Instance == null) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        UnbindManagers();

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager cm = ConnectionManager.Instance;
            cm.OnJoinCodeObtained += HandleJoinCodeUpdated;
            cm.OnConnectionProgress += HandleConnectionProgress;
            cm.OnConnectionFailed += HandleConnectionFailed;
            cm.OnDisconnected += HandleDisconnected;
            cm.OnClientJoined += HandleClientJoined;
            cm.OnHostStarted += HandleHostStarted;
            cm.OnClientConnected += HandleClientConnected;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged += RefreshPlayerCount;
            LobbySessionManager.Instance.OnJoinCodeChanged += HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError += HandleLobbyError;
        }
    }

    private void UnbindManagers()
    {
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager cm = ConnectionManager.Instance;
            cm.OnJoinCodeObtained -= HandleJoinCodeUpdated;
            cm.OnConnectionProgress -= HandleConnectionProgress;
            cm.OnConnectionFailed -= HandleConnectionFailed;
            cm.OnDisconnected -= HandleDisconnected;
            cm.OnClientJoined -= HandleClientJoined;
            cm.OnHostStarted -= HandleHostStarted;
            cm.OnClientConnected -= HandleClientConnected;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged -= RefreshPlayerCount;
            LobbySessionManager.Instance.OnJoinCodeChanged -= HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError -= HandleLobbyError;
        }
    }

    private IEnumerator AutoContinueRoutine()
    {
        SetStatusKey("lobby.status.resuming_host");
        float timeout = 8f;
        while (ConnectionManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
        {
            SetStatusKey("lobby.status.no_connection_manager");
            yield break;
        }

        _mode = LobbyUiMode.HostWaiting;
        ApplyViewMode();

        var hostTask = ConnectionManager.Instance.StartHostAsync();
        while (!hostTask.IsCompleted)
            yield return null;
    }

    private void EnterHostMode()
    {
        if (_hostStartInProgress || (IsNetworkConnected() && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost))
        {
            ApplyViewMode();
            return;
        }

        AbandonConnectionForModeSwitch();
        _mode = LobbyUiMode.HostWaiting;
        ApplyViewMode();
        StartHost();
    }

    private void EnterJoinMode()
    {
        AbandonConnectionForModeSwitch();
        _mode = LobbyUiMode.ClientJoin;
        _clientConnectInProgress = false;
        if (joinCodeInput != null)
            joinCodeInput.text = string.Empty;

        ApplyViewMode();
        SetStatusKey("lobby.status.enter_code");
    }

    private void EnterSoloMode()
    {
        AbandonConnectionForModeSwitch();
        _mode = LobbyUiMode.SoloConfirm;
        ApplyViewMode();
        SetStatusKey("lobby.solo.prompt");
    }

    /// <summary>
    /// Cancela qualquer host/cliente em criação ao trocar de modo (ex.: clicou Solo enquanto o
    /// código multiplayer estava sendo gerado). Se já houver conexão ativa, derruba sem disparar
    /// a UI de desconexão; se ainda estiver criando, o teardown ocorre quando o host concluir.
    /// </summary>
    private void AbandonConnectionForModeSwitch()
    {
        StopReadyDelay();
        _clientConnectInProgress = false;
        _matchTransitionStarted = false;

        // Cancela um StartHostAsync ainda em alocação de Relay (evita start->shutdown e erros de Relay).
        ConnectionManager.Instance?.CancelPendingHostStart();

        if (!IsNetworkConnected())
            return;

        TeardownConnectionSilently();
    }

    private void TeardownConnectionSilently()
    {
        if (!IsNetworkConnected() || ConnectionManager.Instance == null)
            return;

        _suppressDisconnectUi = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            ConnectionManager.Instance.DisconnectAsHost();
        else
            ConnectionManager.Instance.Disconnect();
    }

    private bool IsMultiplayerMode => _mode == LobbyUiMode.HostWaiting || _mode == LobbyUiMode.ClientJoin;

    private void CancelCurrentMode()
    {
        if (IsNetworkConnected())
        {
            Disconnect();
            return;
        }

        ReturnToModeSelect();
    }

    private void OnBackOrCharactersClicked()
    {
        if (_mode != LobbyUiMode.ModeSelect)
            CancelCurrentMode();
        else
            OpenCharacters();
    }

    private async void StartHost()
    {
        if (_hostStartInProgress || ConnectionManager.Instance == null)
            return;

        if (IsNetworkConnected() && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            return;

        _hostStartInProgress = true;
        GameSessionContext.BeginMultiplayer();
        SetStatusKey("lobby.status.initializing_host");

        try
        {
            await ConnectionManager.Instance.StartHostAsync();
            RefreshPlayerCount();
        }
        finally
        {
            _hostStartInProgress = false;
        }
    }

    private async void TryConnectClient()
    {
        if (ConnectionManager.Instance == null || _clientConnectInProgress || IsNetworkConnected())
            return;

        string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim().ToUpper() : string.Empty;
        if (joinCode.Length != JoinCodeLength)
        {
            SetStatusKey("lobby.status.enter_code");
            return;
        }

        GameSessionContext.BeginMultiplayer();
        _clientConnectInProgress = true;
        SetReadyVisible(false);
        SetStatusKey("lobby.status.connecting");
        await ConnectionManager.Instance.StartClientAsync(joinCode);
        _clientConnectInProgress = false;
    }

    private void ConfirmSolo()
    {
        GameSessionContext.BeginSinglePlayer();
        SetStatusKey("lobby.status.starting_solo");
        StartCoroutine(BeginPreparationWhenReadyRoutine());
    }

    private IEnumerator BeginPreparationWhenReadyRoutine()
    {
        yield return ScreenFlowTransitionGate.WaitUntilReady();

        if (_matchTransitionStarted)
            yield break;

        TryBeginPreparation();
    }

    private void OpenCharacters()
    {
        ScreenFlowStateMachine.OpenCharactersFromLobby();
    }

    private void OnReadyToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        if (readyToggle != null)
            readyToggle.SetIsOnWithoutNotify(false);

        switch (_mode)
        {
            case LobbyUiMode.SoloConfirm:
                ConfirmSolo();
                break;
            case LobbyUiMode.HostWaiting:
                HostStartGame();
                break;
            case LobbyUiMode.ClientJoin:
                TryConnectClient();
                break;
        }
    }

    private void HostStartGame()
    {
        if (!IsNetworkConnected() || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            return;

        if (GetConnectedPlayerCount() < requiredPlayersForLoading)
        {
            SetStatusKey("lobby.status.waiting_players", requiredPlayersForLoading);
            return;
        }

        GameSessionContext.BeginMultiplayer();

        if (LobbySessionManager.Instance != null && LobbySessionManager.Instance.IsSpawned)
        {
            _matchTransitionStarted = true;
            LobbySessionManager.Instance.RequestStartGameRpc();
        }
        else
            TryBeginPreparation();
    }

    private void TryBeginPreparation()
    {
        if (_matchTransitionStarted)
            return;

        _matchTransitionStarted = true;
        if (LobbyMatchFlow.TryBeginMatchFromLobby())
            SetStatusKey("lobby.status.loading_prep");
        else
        {
            _matchTransitionStarted = false;
            SetStatusKey("lobby.status.flow_unavailable");
        }
    }

    private void HandleHostStarted()
    {
        if (_mode != LobbyUiMode.HostWaiting)
        {
            // Usuário saiu do multiplayer enquanto o host era criado: derruba sem mostrar nada.
            TeardownConnectionSilently();
            return;
        }

        SetStatusKey("lobby.status.waiting_second");
        RefreshPlayerCount();
        ApplyViewMode();
    }

    private void HandleClientJoined(ulong _)
    {
        if (!IsMultiplayerMode)
            return;

        RefreshPlayerCount();
        ScheduleHostReadyIfNeeded();
    }

    private void HandleClientConnected()
    {
        if (_mode != LobbyUiMode.ClientJoin)
        {
            // Usuário trocou de modo enquanto a conexão era estabelecida: desconecta em silêncio.
            TeardownConnectionSilently();
            return;
        }

        SetStatusKey("lobby.status.connected");
        ApplyViewMode();
        RefreshPlayerCount();
    }

    private void Disconnect()
    {
        _matchTransitionStarted = false;
        _clientConnectInProgress = false;
        StopReadyDelay();

        if (!IsNetworkConnected())
        {
            ReturnToModeSelect();
            return;
        }

        bool wasHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        if (wasHost && ConnectionManager.Instance != null)
            ConnectionManager.Instance.DisconnectAsHost();
        else
            ConnectionManager.Instance?.Disconnect();
    }

    private void ReturnToModeSelect()
    {
        _mode = LobbyUiMode.ModeSelect;
        ApplyViewMode();
        ClearStatus();
        RefreshPlayerCount();
    }

    private void CopyJoinCode()
    {
        string code = ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty;
        if (string.IsNullOrWhiteSpace(code))
            return;

        GUIUtility.systemCopyBuffer = code;
        SetStatusKey("lobby.status.code_copied", code);
    }

    private void HandleDisconnected()
    {
        _matchTransitionStarted = false;
        _clientConnectInProgress = false;

        if (_suppressDisconnectUi)
        {
            // Teardown intencional ao trocar de modo: mantém a tela atual (ex.: Solo) intacta.
            _suppressDisconnectUi = false;
            _hostLeftReceived = false;
            return;
        }

        ReturnToModeSelect();

        if (_hostLeftReceived)
        {
            _hostLeftReceived = false;
            return;
        }

        SetStatusKey("lobby.status.disconnected");
    }

    private void HandleHostLeftSession()
    {
        _hostLeftReceived = true;
        _matchTransitionStarted = false;
        _clientConnectInProgress = false;
        ReturnToModeSelect();
        SetStatusKey("lobby.status.host_left");
    }

    private void HandleConnectionProgress(string message)
    {
        if (!IsMultiplayerMode)
            return;

        SetStatus(UiLocalization.TranslateLobbyConnectionMessage(message));
    }

    private void HandleConnectionFailed(string message)
    {
        _clientConnectInProgress = false;

        if (!IsMultiplayerMode)
            return;

        SetStatus(UiLocalization.TranslateLobbyConnectionMessage(message));
        UpdateJoinReadyVisibility();
        ApplyViewMode();
    }

    private void HandleLobbyError(string message)
    {
        _matchTransitionStarted = false;
        SetStatus(UiLocalization.TranslateLobbyConnectionMessage(message));
    }

    private void HandleJoinCodeUpdated(string code)
    {
        if (joinCodeText == null)
            return;

        joinCodeText.text = UiLocalization.FormatLobbyCode(code);
    }

    private void OnJoinCodeInputChanged(string _)
    {
        if (joinCodeInput == null)
            return;

        string normalized = joinCodeInput.text.Trim().ToUpper();
        if (joinCodeInput.text != normalized)
        {
            joinCodeInput.SetTextWithoutNotify(normalized);
        }

        UpdateJoinReadyVisibility();
    }

    private void RefreshPlayerCount()
    {
        if (playersText == null)
            return;

        int count = GetConnectedPlayerCount();
        playersText.text = UiLocalization.FormatLobbyPlayerCount(count, requiredPlayersForLoading);
        ScheduleHostReadyIfNeeded();
    }

    private int GetConnectedPlayerCount()
    {
        if (NetworkManager.Singleton != null && IsNetworkConnected())
            return NetworkManager.Singleton.ConnectedClientsIds.Count;

        return 0;
    }

    private void ScheduleHostReadyIfNeeded()
    {
        if (_mode != LobbyUiMode.HostWaiting)
        {
            // Fora do modo host, quem decide a visibilidade do toggle é UpdateReadyToggleForMode/
            // UpdateJoinReadyVisibility. Não esconder aqui, senão o botão Jogar do solo some quando
            // RefreshPlayerCount é disparado durante o teardown do host (troca rápida host->solo).
            StopReadyDelay();
            return;
        }

        if (!IsHostWithEnoughPlayers())
        {
            StopReadyDelay();
            SetReadyVisible(false);
            return;
        }

        if (_readyDelayCoroutine != null)
            return;

        SetReadyVisible(false);
        _readyDelayCoroutine = StartCoroutine(ShowHostReadyAfterDelayRoutine());
    }

    private IEnumerator ShowHostReadyAfterDelayRoutine()
    {
        yield return new WaitForSeconds(readyShowDelaySeconds);

        _readyDelayCoroutine = null;

        if (_mode == LobbyUiMode.HostWaiting && IsHostWithEnoughPlayers())
        {
            SetReadyVisible(true);
            SetStatusKey("lobby.status.players_connected");
        }
    }

    private void StopReadyDelay()
    {
        if (_readyDelayCoroutine != null)
        {
            StopCoroutine(_readyDelayCoroutine);
            _readyDelayCoroutine = null;
        }
    }

    private void UpdateJoinReadyVisibility()
    {
        if (_mode != LobbyUiMode.ClientJoin)
            return;

        if (IsNetworkConnected() || _clientConnectInProgress)
        {
            SetReadyVisible(false);
            return;
        }

        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
        SetReadyVisible(code.Length == JoinCodeLength);
    }

    private void ApplyViewMode()
    {
        bool connected = IsNetworkConnected();
        bool isHost = connected && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        bool showHostPanel = _mode == LobbyUiMode.HostWaiting;
        bool showJoinPanel = _mode == LobbyUiMode.ClientJoin;
        bool showSoloPanel = _mode == LobbyUiMode.SoloConfirm;
        bool showRightPanel = showHostPanel || showJoinPanel || showSoloPanel;

        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (joinButton != null) joinButton.gameObject.SetActive(true);
        if (soloButton != null) soloButton.gameObject.SetActive(true);

        if (joinCodeText != null)
            joinCodeText.gameObject.SetActive(showHostPanel && connected);

        if (copyCodeButton != null)
            copyCodeButton.gameObject.SetActive(showHostPanel && connected && isHost);

        if (joinCodeInput != null)
            joinCodeInput.gameObject.SetActive(showJoinPanel && !connected);

        if (insertCodeTitle != null)
            insertCodeTitle.gameObject.SetActive(showJoinPanel && !connected);

        if (codeTitle != null)
            codeTitle.gameObject.SetActive(false);

        bool showPlayerCount = (showHostPanel && connected) || (showJoinPanel && connected);
        if (playersText != null)
            playersText.gameObject.SetActive(showPlayerCount);

        if (statusText != null)
            statusText.gameObject.SetActive(showRightPanel);

        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(showRightPanel);

        if (charactersButton != null)
            charactersButton.gameObject.SetActive(false);

        if (instructionText != null)
            instructionText.gameObject.SetActive(_mode == LobbyUiMode.ModeSelect);

        UpdateDisconnectLabel();
        ApplyRightPanelLayout(showSoloPanel);
        UpdateReadyToggleForMode();
        UpdateJoinReadyVisibility();
    }

    private void CaptureDefaultPanelLayout()
    {
        if (_defaultLayoutCaptured)
            return;

        if (statusText != null)
            _defaultStatusAnchoredPos = statusText.rectTransform.anchoredPosition;

        if (readyToggle != null)
            _defaultReadyAnchoredPos = readyToggle.GetComponent<RectTransform>().anchoredPosition;

        if (disconnectButton != null)
            _defaultDisconnectAnchoredPos = disconnectButton.GetComponent<RectTransform>().anchoredPosition;

        _defaultLayoutCaptured = true;
    }

    private void ApplyRightPanelLayout(bool soloPanel)
    {
        CaptureDefaultPanelLayout();

        if (statusText != null)
        {
            statusText.rectTransform.anchoredPosition = soloPanel
                ? soloStatusAnchoredPos
                : _defaultStatusAnchoredPos;
        }

        if (readyToggle != null)
        {
            readyToggle.GetComponent<RectTransform>().anchoredPosition = soloPanel
                ? soloReadyAnchoredPos
                : _defaultReadyAnchoredPos;
        }

        if (disconnectButton != null)
        {
            disconnectButton.GetComponent<RectTransform>().anchoredPosition = soloPanel
                ? soloDisconnectAnchoredPos
                : _defaultDisconnectAnchoredPos;
        }
    }

    private void UpdateDisconnectLabel()
    {
        if (disconnectButton == null)
            return;

        TMP_Text label = disconnectButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        bool connected = IsNetworkConnected();
        label.text = connected
            ? UiLocalization.Get("btn.lobby.disconect", "Desconectar")
            : UiLocalization.Get("btn.back", "Voltar");

        // Mesma cor dos outros botões do Lobby (Host/Entrar).
        label.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
        label.enableAutoSizing = false;
        label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
    }

    private void UpdateReadyToggleForMode()
    {
        if (readyToggle == null)
            return;

        if (_mode == LobbyUiMode.SoloConfirm)
        {
            SetReadyVisible(true);
            SetReadyLabel("btn.play");
            return;
        }

        if (_mode == LobbyUiMode.HostWaiting)
        {
            SetReadyLabel("btn.ready");
            if (!IsHostWithEnoughPlayers())
                SetReadyVisible(false);
            return;
        }

        if (_mode == LobbyUiMode.ClientJoin)
        {
            SetReadyLabel("btn.ready");
            return;
        }

        SetReadyVisible(false);
    }

    private void SetReadyLabel(string localizationKey)
    {
        if (readyToggle == null)
            return;

        TMP_Text label = readyToggle.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = UiLocalization.Get(localizationKey, label.text);
    }

    private void SetReadyVisible(bool visible)
    {
        if (readyToggle != null)
            readyToggle.gameObject.SetActive(visible);
    }

    private bool IsNetworkConnected()
    {
        return NetworkManager.Singleton != null
               && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);
    }

    private bool IsHostWithEnoughPlayers()
    {
        return NetworkManager.Singleton != null
               && NetworkManager.Singleton.IsHost
               && GetConnectedPlayerCount() >= requiredPlayersForLoading;
    }

    private void SetStatusKey(string key, params object[] args)
    {
        SetStatus(UiLocalization.Format(key, key, args));
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ClearStatus()
    {
        if (statusText != null)
            statusText.text = string.Empty;
    }
}
