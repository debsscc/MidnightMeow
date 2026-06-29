/// <summary>
/// MultiplayerLobbyUI.cs
/// Controlador de UI para o lobby multiplayer da cena Sandbox.
/// Gerencia os painéis de pré-jogo (lobby) e in-game, respondendo a eventos do
/// ConnectionManager para atualizar o estado visual (código de acesso, status,
/// lista de jogadores, botões de ação). Usa padrão Observer via eventos para
/// desacoplamento total da lógica de conexão.
/// Correção: eventos do ConnectionManager são assinados em Start() + retry coroutine
/// para evitar race condition com a ordem de inicialização dos singletons.
/// SRP: exclusivamente responsável pela interface de lobby e conexão.
/// </summary>

using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerLobbyUI : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject inGameHUDPanel;

    [Header("Lobby - Botões")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button startGameButton;

    [Header("Lobby - Inputs e Textos")]
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeDisplayText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text errorText;

    private bool _subscribedToConnectionManager = false;

    private void Awake()
    {
        if (hostButton != null)       hostButton.onClick.AddListener(OnHostButtonClicked);
        if (joinButton != null)       joinButton.onClick.AddListener(OnJoinButtonClicked);
        if (copyCodeButton != null)   copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
        if (startGameButton != null)  startGameButton.onClick.AddListener(OnStartGameClicked);

        // Estado inicial correto — botões secundários ocultos até serem relevantes
        SetGameButtonsVisible(false);
        HideError();
    }

    private void Start()
    {
        MultiplayerGameManager.OnGameStateChanged += HandleGameStateChanged;
        ShowLobbyPanel();
        SetStatus(LocaleText.IsPortuguese()
            ? "Pronto. Jogue solo, hospede ou entre em uma partida."
            : "Ready. Play solo, host, or join a match.");
        RefreshSoloStartButton();
        StartCoroutine(SubscribeToConnectionManagerRoutine());
    }

    private void RefreshSoloStartButton()
    {
        bool connected = NetworkManager.Singleton != null
                         && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);

        if (startGameButton == null)
            return;

        if (!connected)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        MultiplayerGameManager.OnGameStateChanged -= HandleGameStateChanged;
        UnsubscribeFromConnectionManager();
    }

    /// <summary>
    /// Aguarda o ConnectionManager.Instance ficar disponível (resolve race condition
    /// de ordem de inicialização de singletons) e então assina seus eventos.
    /// </summary>
    private IEnumerator SubscribeToConnectionManagerRoutine()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ConnectionManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] ConnectionManager não encontrado na cena após 5 segundos!");
            SetError(LocaleText.IsPortuguese()
                ? "Erro interno: ConnectionManager ausente."
                : "Internal error: ConnectionManager missing.");
            yield break;
        }

        SubscribeToConnectionManager();
    }

    private void SubscribeToConnectionManager()
    {
        if (_subscribedToConnectionManager) return;
        var cm = ConnectionManager.Instance;
        cm.OnJoinCodeObtained  += HandleJoinCodeObtained;
        cm.OnHostStarted       += HandleHostStarted;
        cm.OnClientConnected   += HandleClientConnected;
        cm.OnClientJoined      += HandleClientJoined;
        cm.OnClientLeft        += HandleClientLeft;
        cm.OnConnectionFailed  += HandleConnectionFailed;
        cm.OnDisconnected      += HandleDisconnected;
        cm.OnConnectionProgress += SetStatus;
        _subscribedToConnectionManager = true;
        Debug.Log("[MultiplayerLobbyUI] Inscrito nos eventos do ConnectionManager.");
    }

    private void UnsubscribeFromConnectionManager()
    {
        if (!_subscribedToConnectionManager || ConnectionManager.Instance == null) return;
        var cm = ConnectionManager.Instance;
        cm.OnJoinCodeObtained  -= HandleJoinCodeObtained;
        cm.OnHostStarted       -= HandleHostStarted;
        cm.OnClientConnected   -= HandleClientConnected;
        cm.OnClientJoined      -= HandleClientJoined;
        cm.OnClientLeft        -= HandleClientLeft;
        cm.OnConnectionFailed  -= HandleConnectionFailed;
        cm.OnDisconnected      -= HandleDisconnected;
        cm.OnConnectionProgress -= SetStatus;
        _subscribedToConnectionManager = false;
    }

    // --- Handlers de Botões ---

    private async void OnHostButtonClicked()
    {
        if (ConnectionManager.Instance == null)
        {
            SetError(LocaleText.IsPortuguese()
                ? "ConnectionManager não encontrado na cena."
                : "ConnectionManager not found in scene.");
            return;
        }

        GameSessionContext.BeginMultiplayer();
        SetButtonsInteractable(false);
        SetStatus(LocaleText.IsPortuguese()
            ? "Inicializando Unity Services..."
            : "Initializing Unity Services...");
        HideError();

        await ConnectionManager.Instance.StartHostAsync();
    }

    private async void OnJoinButtonClicked()
    {
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            SetError(LocaleText.IsPortuguese()
                ? "Digite o código de acesso antes de entrar."
                : "Enter the access code before joining.");
            return;
        }

        if (ConnectionManager.Instance == null)
        {
            SetError(LocaleText.IsPortuguese()
                ? "ConnectionManager não encontrado na cena."
                : "ConnectionManager not found in scene.");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus(LocaleText.IsPortuguese()
            ? $"Entrando na sessão com código: {code.ToUpper()}..."
            : $"Joining session with code: {code.ToUpper()}...");
        HideError();

        await ConnectionManager.Instance.StartClientAsync(code);
    }

    private void OnCopyCodeClicked()
    {
        string code = ConnectionManager.Instance?.CurrentJoinCode;
        if (!string.IsNullOrEmpty(code))
        {
            GUIUtility.systemCopyBuffer = code;
            SetStatus(LocaleText.IsPortuguese()
                ? $"Código {code} copiado!"
                : $"Code {code} copied!");
        }
    }

    private void OnDisconnectClicked()
    {
        ConnectionManager.Instance?.Disconnect();
    }

    private void OnStartGameClicked()
    {
        bool connected = NetworkManager.Singleton != null
                         && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);

        if (connected && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            SetError(LocaleText.IsPortuguese()
                ? "Apenas o host pode iniciar o jogo."
                : "Only the host can start the game.");
            return;
        }

        if (connected && NetworkManager.Singleton.IsHost)
        {
            GameSessionContext.BeginMultiplayer();
            if (LobbySessionManager.Instance != null)
            {
                LobbySessionManager.Instance.RequestStartGameRpc();
                SetStatus(LocaleText.IsPortuguese()
                    ? "Carregando seleção de personagens..."
                    : "Loading character selection...");
                return;
            }
        }
        else
        {
            GameSessionContext.BeginSinglePlayer();
        }

        if (LobbyMatchFlow.TryBeginMatchFromLobby())
        {
            SetStatus(LocaleText.IsPortuguese()
            ? "Carregando seleção de personagens..."
            : "Loading character selection...");
            return;
        }

        SetError(LocaleText.IsPortuguese()
            ? "Fluxo de telas indisponível. Inicie pelo BootstrapScene."
            : "Screen flow unavailable. Start from BootstrapScene.");
    }

    // --- Handlers de Eventos do ConnectionManager ---

    private void HandleJoinCodeObtained(string code)
    {
        if (joinCodeDisplayText != null)
            joinCodeDisplayText.text = LocaleText.IsPortuguese()
                ? $"Código: <b>{code}</b>"
                : $"Code: <b>{code}</b>";

        if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(true);
        SetStatus(LocaleText.IsPortuguese()
            ? $"Sessão criada! Código: <b>{code}</b> — Compartilhe com amigos."
            : $"Session created! Code: <b>{code}</b> — Share it with friends.");
        HideError();
    }

    private void HandleHostStarted()
    {
        SetButtonsInteractable(true);
        if (startGameButton != null) startGameButton.gameObject.SetActive(true);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(true);
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (joinButton != null) joinButton.gameObject.SetActive(false);
        if (joinCodeInputField != null) joinCodeInputField.gameObject.SetActive(false);
        UpdatePlayerCount();
    }

    private void HandleClientConnected()
    {
        SetButtonsInteractable(true);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(true);
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (joinButton != null) joinButton.gameObject.SetActive(false);
        if (joinCodeInputField != null) joinCodeInputField.gameObject.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        SetStatus(LocaleText.IsPortuguese()
            ? "Conectado! Aguardando o host iniciar a partida..."
            : "Connected! Waiting for the host to start the match...");
        HideError();
        UpdatePlayerCount();
    }

    private void HandleClientJoined(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus(LocaleText.IsPortuguese()
            ? $"Jogador {clientId + 1} entrou na partida."
            : $"Player {clientId + 1} joined the match.");
    }

    private void HandleClientLeft(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus(LocaleText.IsPortuguese()
            ? $"Jogador {clientId + 1} saiu da partida."
            : $"Player {clientId + 1} left the match.");
    }

    private void HandleConnectionFailed(string message)
    {
        SetButtonsInteractable(true);
        SetGameButtonsVisible(false);
        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (joinButton != null) joinButton.gameObject.SetActive(true);
        if (joinCodeInputField != null) joinCodeInputField.gameObject.SetActive(true);
        SetError(message);
        SetStatus(LocaleText.IsPortuguese()
            ? "Falha na conexão. Tente novamente."
            : "Connection failed. Try again.");
    }

    private void HandleDisconnected()
    {
        ShowLobbyPanel();
        SetGameButtonsVisible(false);
        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (joinButton != null) joinButton.gameObject.SetActive(true);
        if (joinCodeInputField != null) joinCodeInputField.gameObject.SetActive(true);
        if (joinCodeDisplayText != null) joinCodeDisplayText.text = "";
        SetButtonsInteractable(true);
        SetStatus(LocaleText.IsPortuguese()
            ? "Desconectado. Hospede ou entre em uma partida."
            : "Disconnected. Host or join a match.");
        HideError();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                ShowInGamePanel();
                break;
            case GameState.Victory:
                ShowLobbyPanel();
                SetStatus(LocaleText.IsPortuguese()
                    ? "Vitória! Parabéns!"
                    : "Victory! Congratulations!");
                SetGameButtonsVisible(false);
                break;
            case GameState.Defeat:
                ShowLobbyPanel();
                SetStatus(LocaleText.IsPortuguese()
                    ? "Derrota! Tente novamente."
                    : "Defeat! Try again.");
                SetGameButtonsVisible(false);
                break;
        }
    }

    // --- Helpers de UI ---

    private void ShowLobbyPanel()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (inGameHUDPanel != null) inGameHUDPanel.SetActive(false);
    }

    private void ShowInGamePanel()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (inGameHUDPanel != null) inGameHUDPanel.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log($"[MultiplayerLobbyUI] Status: {message}");
    }

    private void SetError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        Debug.LogWarning($"[MultiplayerLobbyUI] Erro: {message}");
    }

    private void HideError()
    {
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null) hostButton.interactable = interactable;
        if (joinButton != null) joinButton.interactable = interactable;
        if (joinCodeInputField != null) joinCodeInputField.interactable = interactable;
    }

    private void SetGameButtonsVisible(bool visible)
    {
        if (copyCodeButton != null)   copyCodeButton.gameObject.SetActive(visible);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(visible);
        if (startGameButton != null)  startGameButton.gameObject.SetActive(visible);
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText == null || NetworkManager.Singleton == null) return;
        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        playerCountText.text = LocaleText.IsPortuguese()
            ? $"Jogadores: {count}/4"
            : $"Players: {count}/4";
        Debug.Log($"[MultiplayerLobbyUI] Jogadores conectados: {count}");
    }
}
