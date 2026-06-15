using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orquestra os painéis do lobby: seleção de modo, host aguardando e client inserindo código.
/// A transição para Characters ocorre apenas quando o host clica em Start Game.
/// </summary>
[DisallowMultipleComponent]
public class LobbyFlowController : MonoBehaviour
{
    public const string PanelModeSelect = "mode_select";
    public const string PanelHostWaiting = "host_waiting";
    public const string PanelClientJoin = "client_join";

    [Header("Navegação")]
    [SerializeField] private ScreenPanelNavigator navigator;

    [Header("Modo — Seleção")]
    [SerializeField] private Button soloButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button charactersButton;

    [Header("Host aguardando")]
    [SerializeField] private TMP_Text hostJoinCodeText;
    [SerializeField] private TMP_Text hostPlayersText;
    [SerializeField] private TMP_Text hostFeedbackText;

    [Header("Client")]
    [SerializeField] private TMP_InputField clientJoinCodeInput;
    [SerializeField] private Button clientConfirmButton;
    [SerializeField] private TMP_Text clientStatusText;

    [Header("Fluxo")]
    [SerializeField] private int requiredPlayersForLoading = 2;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private bool _matchTransitionStarted;

    private void Awake()
    {
        if (navigator == null)
            navigator = GetComponent<ScreenPanelNavigator>();

        if (buildPlaceholderIfMissing && navigator == null)
            BuildPlaceholderUI();

        WireButtons();
    }

    private void OnEnable()
    {
        StartCoroutine(BindConnectionRoutine());
    }

    private void Start()
    {
        if (GameSessionContext.AutoHostOnLobbyEnter)
            StartCoroutine(AutoContinueRoutine());
        else
            navigator?.ShowPanel(PanelModeSelect);

        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
        ScreenFlowSceneReadiness.MarkReadyIfPending("Lobby");
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        UnbindConnection();
    }

    private void WireButtons()
    {
        if (soloButton != null) soloButton.onClick.AddListener(OnSoloClicked);
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(() => navigator?.ShowPanel(PanelClientJoin));
        if (charactersButton != null) charactersButton.onClick.AddListener(OnCharactersClicked);
        if (clientConfirmButton != null) clientConfirmButton.onClick.AddListener(OnClientConfirm);
    }

    private void OnSoloClicked()
    {
        GameSessionContext.BeginSinglePlayer();
        StartCoroutine(BeginPreparationWhenReadyRoutine());
    }

    private IEnumerator BeginPreparationWhenReadyRoutine()
    {
        yield return ScreenFlowTransitionGate.WaitUntilReady();

        if (_matchTransitionStarted)
            yield break;

        TryBeginPreparation();
    }

    private void OnCharactersClicked()
    {
        ScreenFlowStateMachine.OpenCharactersFromLobby();
    }

    private void TryBeginPreparation()
    {
        if (_matchTransitionStarted)
            return;

        _matchTransitionStarted = true;
        if (!LobbyMatchFlow.TryBeginMatchFromLobby())
            _matchTransitionStarted = false;
    }

    private IEnumerator AutoContinueRoutine()
    {
        navigator?.ShowPanel(PanelHostWaiting);
        SetHostFeedback("Retomando sessão como host...");

        float timeout = 8f;
        while (ConnectionManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
        {
            SetHostFeedback("Erro: ConnectionManager ausente.");
            yield break;
        }

        var hostTask = ConnectionManager.Instance.StartHostAsync();
        while (!hostTask.IsCompleted)
            yield return null;
    }

    private IEnumerator BindConnectionRoutine()
    {
        float timeout = 10f;
        while (ConnectionManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
            yield break;

        ConnectionManager cm = ConnectionManager.Instance;
        cm.OnJoinCodeObtained += HandleJoinCode;
        cm.OnHostStarted += HandleHostStarted;
        cm.OnClientConnected += HandleClientConnected;
        cm.OnClientJoined += HandleClientJoined;
        cm.OnConnectionFailed += HandleConnectionFailed;
        cm.OnConnectionProgress += SetClientStatus;
    }

    private void UnbindConnection()
    {
        if (ConnectionManager.Instance == null)
            return;

        ConnectionManager cm = ConnectionManager.Instance;
        cm.OnJoinCodeObtained -= HandleJoinCode;
        cm.OnHostStarted -= HandleHostStarted;
        cm.OnClientConnected -= HandleClientConnected;
        cm.OnClientJoined -= HandleClientJoined;
        cm.OnConnectionFailed -= HandleConnectionFailed;
        cm.OnConnectionProgress -= SetClientStatus;
    }

    private async void OnHostClicked()
    {
        if (ConnectionManager.Instance == null)
            return;

        GameSessionContext.BeginMultiplayer();
        navigator?.ShowPanel(PanelHostWaiting);
        SetHostFeedback("Inicializando host...");
        await ConnectionManager.Instance.StartHostAsync();
    }

    private async void OnClientConfirm()
    {
        if (ConnectionManager.Instance == null)
            return;

        string code = clientJoinCodeInput != null ? clientJoinCodeInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            SetClientStatus("Digite o código da sala.");
            return;
        }

        SetClientStatus("Conectando...");
        await ConnectionManager.Instance.StartClientAsync(code);
    }

    private void HandleJoinCode(string code)
    {
        if (hostJoinCodeText != null)
            hostJoinCodeText.text = $"Código da Sala: {code}";
    }

    private void HandleHostStarted()
    {
        navigator?.ShowPanel(PanelHostWaiting);
        SetHostFeedback("Aguardando segundo jogador...");
        UpdatePlayerCount();
        SaveProfileStore save = SaveProfileStore.Instance;
        save?.Active?.Touch(host: true, ConnectionManager.Instance?.CurrentJoinCode, "Lobby");
        save?.SaveActive();
    }

    private void HandleClientConnected()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            return;

        SetClientStatus("Conectado! Aguardando o host iniciar a partida...");
    }

    private void HandleClientJoined(ulong _)
    {
        UpdatePlayerCount();
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

        SetHostFeedback("Jogadores conectados! Carregando preparação...");
        TryBeginPreparation();
    }

    private void HandleConnectionFailed(string message)
    {
        SetClientStatus(message);
        SetHostFeedback(message);
    }

    private void UpdatePlayerCount()
    {
        if (NetworkManager.Singleton == null)
            return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (hostPlayersText != null)
            hostPlayersText.text = $"Jogadores {count}/{requiredPlayersForLoading}";

        if (hostFeedbackText != null && count < requiredPlayersForLoading)
            hostFeedbackText.text = "Aguardando segundo jogador...";
    }

    private void SetHostFeedback(string message)
    {
        if (hostFeedbackText != null)
            hostFeedbackText.text = message;
    }

    private void SetClientStatus(string message)
    {
        if (clientStatusText != null)
            clientStatusText.text = message;
    }

    private void BuildPlaceholderUI()
    {
        navigator = gameObject.AddComponent<ScreenPanelNavigator>();
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);

        GameObject modePanel = BuildModeSelectPanel(canvas.transform);
        GameObject hostPanel = BuildHostWaitingPanel(canvas.transform);
        GameObject clientPanel = BuildClientJoinPanel(canvas.transform);

        var nav = navigator;
        nav.RegisterPanel(PanelModeSelect, modePanel);
        nav.RegisterPanel(PanelHostWaiting, hostPanel);
        nav.RegisterPanel(PanelClientJoin, clientPanel);
    }

    private GameObject BuildModeSelectPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelModeSelect, new Color(0.07f, 0.07f, 0.1f, 0.94f));
        ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Lobby — Seleção de Modo", 48, TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-500f, -100f), new Vector2(500f, -10f));

        soloButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Jogar Solo",
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(-240f, -40f), new Vector2(240f, 40f));
        hostButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Hostear",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f, -40f), new Vector2(240f, 40f));
        joinButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Entrar",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(-240f, -40f), new Vector2(240f, 40f));
        charactersButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Personagens",
            new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(-240f, -40f), new Vector2(240f, 40f));
        return panel;
    }

    private GameObject BuildHostWaitingPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelHostWaiting, new Color(0.05f, 0.08f, 0.12f, 0.94f));
        hostJoinCodeText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Código da Sala: ----", 36, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(-400f, -30f), new Vector2(400f, 30f));
        hostPlayersText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Jogadores 0/2", 28, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -25f), new Vector2(300f, 25f));
        hostFeedbackText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Aguardando segundo jogador...", 24, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f),
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(-400f, -25f), new Vector2(400f, 25f));
        return panel;
    }

    private GameObject BuildClientJoinPanel(Transform parent)
    {
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(parent, PanelClientJoin, new Color(0.08f, 0.05f, 0.1f, 0.94f));
        clientJoinCodeInput = ScreenFlowPlaceholderFactory.CreateInputField(panel.transform, "Insira o código",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(-280f, -35f), new Vector2(280f, 35f));
        clientConfirmButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Entrar",
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(-200f, -40f), new Vector2(200f, 40f));
        clientStatusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 22, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(-400f, -30f), new Vector2(400f, 30f));
        return panel;
    }
}
