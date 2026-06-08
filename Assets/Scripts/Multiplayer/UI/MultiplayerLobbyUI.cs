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
        SetStatus("Pronto. Jogue solo, hospede ou entre em uma partida.");
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
            SetError("Erro interno: ConnectionManager ausente.");
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
            SetError("ConnectionManager não encontrado na cena.");
            return;
        }

        GameSessionContext.BeginMultiplayer();
        SetButtonsInteractable(false);
        SetStatus("Inicializando Unity Services...");
        HideError();

        await ConnectionManager.Instance.StartHostAsync();
    }

    private async void OnJoinButtonClicked()
    {
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            SetError("Digite o código de acesso antes de entrar.");
            return;
        }

        if (ConnectionManager.Instance == null)
        {
            SetError("ConnectionManager não encontrado na cena.");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus($"Entrando na sessão com código: {code.ToUpper()}...");
        HideError();

        await ConnectionManager.Instance.StartClientAsync(code);
    }

    private void OnCopyCodeClicked()
    {
        string code = ConnectionManager.Instance?.CurrentJoinCode;
        if (!string.IsNullOrEmpty(code))
        {
            GUIUtility.systemCopyBuffer = code;
            SetStatus($"Código {code} copiado!");
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
            SetError("Apenas o host pode iniciar o jogo.");
            return;
        }

        if (connected && NetworkManager.Singleton.IsHost)
        {
            GameSessionContext.BeginMultiplayer();
            if (LobbySessionManager.Instance != null)
            {
                LobbySessionManager.Instance.RequestStartGameRpc();
                SetStatus("Carregando seleção de personagens...");
                return;
            }
        }
        else
        {
            GameSessionContext.BeginSinglePlayer();
        }

        if (LobbyMatchFlow.TryBeginMatchFromLobby())
        {
            SetStatus("Carregando seleção de personagens...");
            return;
        }

        SetError("Fluxo de telas indisponível. Inicie pelo BootstrapScene.");
    }

    // --- Handlers de Eventos do ConnectionManager ---

    private void HandleJoinCodeObtained(string code)
    {
        if (joinCodeDisplayText != null)
            joinCodeDisplayText.text = $"Código: <b>{code}</b>";

        if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(true);
        SetStatus($"Sessão criada! Código: <b>{code}</b> — Compartilhe com amigos.");
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
        SetStatus("Conectado! Aguardando o host iniciar a partida...");
        HideError();
        UpdatePlayerCount();
    }

    private void HandleClientJoined(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus($"Jogador {clientId + 1} entrou na partida.");
    }

    private void HandleClientLeft(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus($"Jogador {clientId + 1} saiu da partida.");
    }

    private void HandleConnectionFailed(string message)
    {
        SetButtonsInteractable(true);
        SetGameButtonsVisible(false);
        if (hostButton != null) hostButton.gameObject.SetActive(true);
        if (joinButton != null) joinButton.gameObject.SetActive(true);
        if (joinCodeInputField != null) joinCodeInputField.gameObject.SetActive(true);
        SetError(message);
        SetStatus("Falha na conexão. Tente novamente.");
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
        SetStatus("Desconectado. Hospede ou entre em uma partida.");
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
                SetStatus("Vitória! Parabéns!");
                SetGameButtonsVisible(false);
                break;
            case GameState.Defeat:
                ShowLobbyPanel();
                SetStatus("Derrota! Tente novamente.");
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
        playerCountText.text = $"Jogadores: {count}/4";
        Debug.Log($"[MultiplayerLobbyUI] Jogadores conectados: {count}");
    }
}
