/// <summary>
/// Controla a UI da cena de Lobby no fluxo principal.
/// Liga botões do prefab Lobby e dispara transições via <see cref="ScreenFlowStateMachine"/>.
/// </summary>
using System.Collections;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LobbySceneUIController : MonoBehaviour
{
    [Header("Conexao")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button charactersButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button startGameButton;

    [Header("Fluxo")]
    [SerializeField] private int requiredPlayersForLoading = 2;
    [SerializeField] private bool autoResolveMissingRefs = true;

    private readonly StringBuilder _playersBuilder = new StringBuilder(256);
    private bool _matchTransitionStarted;

    private void Awake()
    {
        if (autoResolveMissingRefs)
            TryAutoResolveReferences();

        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (joinButton != null) joinButton.onClick.AddListener(StartClient);
        if (soloButton != null) soloButton.onClick.AddListener(StartSolo);
        if (charactersButton != null) charactersButton.onClick.AddListener(OpenCharacters);
        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(CopyJoinCode);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
    }

    private void OnEnable()
    {
        StartCoroutine(BindManagersRoutine());
    }

    private void Start()
    {
        if (GameSessionContext.AutoHostOnLobbyEnter)
            StartCoroutine(AutoContinueRoutine());

        HandleJoinCodeUpdated(ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty);
        RefreshPlayersView();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        UnbindManagers();
    }

    private void TryAutoResolveReferences()
    {
        if (hostButton == null) hostButton = ScreenFlowUiLookup.FindButton("Host");
        if (joinButton == null) joinButton = ScreenFlowUiLookup.FindButton("Join");
        if (startGameButton == null) startGameButton = ScreenFlowUiLookup.FindButton("StartGame");
        if (disconnectButton == null) disconnectButton = ScreenFlowUiLookup.FindButton("Disconnect");
        if (copyCodeButton == null) copyCodeButton = ScreenFlowUiLookup.FindButton("CopyCode");
        if (charactersButton == null) charactersButton = ScreenFlowUiLookup.FindButton("Back");
        if (joinCodeText == null) joinCodeText = ScreenFlowUiLookup.FindText("JoinCode");
        if (statusText == null) statusText = ScreenFlowUiLookup.FindText("Status") ?? ScreenFlowUiLookup.FindText("ERROCODE");
        if (playersText == null) playersText = ScreenFlowUiLookup.FindText("Texts");
        if (joinCodeInput == null) joinCodeInput = ScreenFlowUiLookup.FindInputField();
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
            cm.OnConnectionProgress += SetStatus;
            cm.OnConnectionFailed += SetStatus;
            cm.OnDisconnected += HandleDisconnected;
            cm.OnClientJoined += HandleClientJoined;
            cm.OnHostStarted += HandleHostStarted;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged += RefreshPlayersView;
            LobbySessionManager.Instance.OnLobbyPlayersChanged += TryAutoStartWhenReady;
            LobbySessionManager.Instance.OnJoinCodeChanged += HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError += SetStatus;
        }
    }

    private void UnbindManagers()
    {
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager cm = ConnectionManager.Instance;
            cm.OnJoinCodeObtained -= HandleJoinCodeUpdated;
            cm.OnConnectionProgress -= SetStatus;
            cm.OnConnectionFailed -= SetStatus;
            cm.OnDisconnected -= HandleDisconnected;
            cm.OnClientJoined -= HandleClientJoined;
            cm.OnHostStarted -= HandleHostStarted;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged -= RefreshPlayersView;
            LobbySessionManager.Instance.OnLobbyPlayersChanged -= TryAutoStartWhenReady;
            LobbySessionManager.Instance.OnJoinCodeChanged -= HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError -= SetStatus;
        }
    }

    private IEnumerator AutoContinueRoutine()
    {
        SetStatus("Retomando sessão como host...");
        float timeout = 8f;
        while (ConnectionManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
        {
            SetStatus("Erro: ConnectionManager ausente.");
            yield break;
        }

        var hostTask = ConnectionManager.Instance.StartHostAsync();
        while (!hostTask.IsCompleted)
            yield return null;
    }

    private async void StartHost()
    {
        if (ConnectionManager.Instance == null) return;
        GameSessionContext.BeginMultiplayer();
        SetStatus("Inicializando host...");
        await ConnectionManager.Instance.StartHostAsync();
        RefreshPlayersView();
    }

    private async void StartClient()
    {
        if (ConnectionManager.Instance == null) return;
        GameSessionContext.BeginMultiplayer();
        string joinCode = joinCodeInput != null ? joinCodeInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Digite um codigo para entrar.");
            return;
        }

        SetStatus("Conectando ao host...");
        await ConnectionManager.Instance.StartClientAsync(joinCode);
    }

    private void StartSolo()
    {
        GameSessionContext.BeginSinglePlayer();
        TryBeginPreparation();
    }

    private void OpenCharacters()
    {
        ScreenFlowStateMachine.OpenCharactersFromLobby();
    }

    private void StartGame()
    {
        bool connected = NetworkManager.Singleton != null
                         && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);

        if (connected && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            SetStatus("Apenas o host pode iniciar o jogo.");
            return;
        }

        if (connected && NetworkManager.Singleton.IsHost)
        {
            if (LobbySessionManager.Instance != null && !LobbySessionManager.Instance.CanStartMatch)
            {
                SetStatus($"Aguardando {requiredPlayersForLoading} jogadores conectados.");
                return;
            }

            GameSessionContext.BeginMultiplayer();
            TryBeginPreparation();
            return;
        }

        StartSolo();
    }

    private void TryBeginPreparation()
    {
        if (_matchTransitionStarted)
            return;

        _matchTransitionStarted = true;
        if (LobbyMatchFlow.TryBeginMatchFromLobby())
            SetStatus("Carregando preparação...");
        else
        {
            _matchTransitionStarted = false;
            SetStatus("Fluxo indisponível. Inicie pelo BootstrapScene.");
        }
    }

    private void HandleHostStarted()
    {
        SetStatus("Aguardando segundo jogador...");
        RefreshPlayersView();
    }

    private void HandleClientJoined(ulong _)
    {
        RefreshPlayersView();
        TryAutoStartWhenReady();
    }

    private void TryAutoStartWhenReady()
    {
        if (_matchTransitionStarted || GameSessionContext.IsSinglePlayer)
            return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (count < requiredPlayersForLoading)
            return;

        SetStatus("Jogadores conectados! Carregando preparação...");
        TryBeginPreparation();
    }

    private void Disconnect()
    {
        ConnectionManager.Instance?.Disconnect();
        _matchTransitionStarted = false;
    }

    private void CopyJoinCode()
    {
        string code = ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty;
        if (string.IsNullOrWhiteSpace(code)) return;
        GUIUtility.systemCopyBuffer = code;
        SetStatus($"Codigo {code} copiado.");
    }

    private void HandleDisconnected()
    {
        _matchTransitionStarted = false;
        SetStatus("Desconectado do lobby.");
        RefreshPlayersView();
    }

    private void HandleJoinCodeUpdated(string code)
    {
        if (joinCodeText == null) return;
        joinCodeText.text = string.IsNullOrWhiteSpace(code) ? "Codigo: --" : $"Codigo: <b>{code}</b>";
    }

    private void RefreshPlayersView()
    {
        bool connected = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);

        if (hostButton != null) hostButton.gameObject.SetActive(!connected);
        if (joinButton != null) joinButton.gameObject.SetActive(!connected);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(!connected);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(connected);
        if (copyCodeButton != null)
            copyCodeButton.gameObject.SetActive(connected && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);

        if (playersText != null)
        {
            _playersBuilder.Clear();
            var lobby = LobbySessionManager.Instance;
            if (lobby == null)
            {
                playersText.text = "Aguardando LobbySessionManager...";
            }
            else
            {
                for (int i = 0; i < lobby.Players.Count; i++)
                {
                    LobbyPlayerState player = lobby.Players[i];
                    bool isLocal = NetworkManager.Singleton != null && player.ClientId == NetworkManager.Singleton.LocalClientId;
                    _playersBuilder.Append("- ").Append(player.DisplayName);
                    if (isLocal) _playersBuilder.Append(" (voce)");
                    _playersBuilder.AppendLine();
                }

                playersText.text = _playersBuilder.ToString();
            }
        }

        if (startGameButton != null)
        {
            TMP_Text label = startGameButton.GetComponentInChildren<TMP_Text>();
            if (!connected)
            {
                startGameButton.gameObject.SetActive(true);
                startGameButton.interactable = true;
                if (label != null) label.text = "Jogar Solo";
            }
            else
            {
                bool host = NetworkManager.Singleton.IsHost;
                bool canStart = LobbySessionManager.Instance != null && LobbySessionManager.Instance.CanStartMatch;
                startGameButton.gameObject.SetActive(host);
                startGameButton.interactable = host && canStart;
                if (label != null) label.text = "Iniciar Partida";
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
