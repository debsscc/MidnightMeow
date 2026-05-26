/// <summary>
/// Controla a UI da cena de Lobby no fluxo principal.
/// Reage a eventos de ConnectionManager e LobbySessionManager para manter a tela
/// sincronizada sem acoplamento com regras de dominio.
/// </summary>
using System.Text;
using System.Collections;
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
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button startGameButton;

    [Header("Selecao de personagem")]
    [SerializeField] private Button selectCharacterAButton;
    [SerializeField] private Button selectCharacterBButton;
    [SerializeField] private Toggle readyToggle;

    private readonly StringBuilder _playersBuilder = new StringBuilder(256);

    private void Awake()
    {
        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (joinButton != null) joinButton.onClick.AddListener(StartClient);
        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(CopyJoinCode);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);
        if (startGameButton != null) startGameButton.onClick.AddListener(RequestStartGame);
        if (selectCharacterAButton != null) selectCharacterAButton.onClick.AddListener(() => SelectCharacter(LobbyCharacterType.CharacterA));
        if (selectCharacterBButton != null) selectCharacterBButton.onClick.AddListener(() => SelectCharacter(LobbyCharacterType.CharacterB));
        if (readyToggle != null) readyToggle.onValueChanged.AddListener(SetReady);
    }

    private void OnEnable()
    {
        StartCoroutine(BindManagersRoutine());
    }

    private void Start()
    {
        HandleJoinCodeUpdated(ConnectionManager.Instance != null ? ConnectionManager.Instance.CurrentJoinCode : string.Empty);
        RefreshPlayersView();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnJoinCodeObtained -= HandleJoinCodeUpdated;
            ConnectionManager.Instance.OnConnectionProgress -= SetStatus;
            ConnectionManager.Instance.OnConnectionFailed -= SetStatus;
            ConnectionManager.Instance.OnDisconnected -= HandleDisconnected;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged -= RefreshPlayersView;
            LobbySessionManager.Instance.OnJoinCodeChanged -= HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError -= SetStatus;
        }
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

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnJoinCodeObtained += HandleJoinCodeUpdated;
            ConnectionManager.Instance.OnConnectionProgress += SetStatus;
            ConnectionManager.Instance.OnConnectionFailed += SetStatus;
            ConnectionManager.Instance.OnDisconnected += HandleDisconnected;
        }

        if (LobbySessionManager.Instance != null)
        {
            LobbySessionManager.Instance.OnLobbyPlayersChanged += RefreshPlayersView;
            LobbySessionManager.Instance.OnJoinCodeChanged += HandleJoinCodeUpdated;
            LobbySessionManager.Instance.OnLobbyError += SetStatus;
        }
    }

    private void RequestStartGame()
    {
        if (LobbySessionManager.Instance == null) return;
        LobbySessionManager.Instance.RequestStartGameRpc();
    }

    private async void StartHost()
    {
        if (ConnectionManager.Instance == null) return;
        SetStatus("Inicializando host...");
        await ConnectionManager.Instance.StartHostAsync();
    }

    private async void StartClient()
    {
        if (ConnectionManager.Instance == null) return;
        string joinCode = joinCodeInput != null ? joinCodeInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Digite um codigo para entrar.");
            return;
        }

        SetStatus("Conectando ao host...");
        await ConnectionManager.Instance.StartClientAsync(joinCode);
    }

    private void SelectCharacter(LobbyCharacterType type)
    {
        if (LobbySessionManager.Instance == null) return;
        LobbySessionManager.Instance.RequestSetCharacterRpc((byte)type);
    }

    private void SetReady(bool isReady)
    {
        if (LobbySessionManager.Instance == null) return;
        LobbySessionManager.Instance.RequestSetReadyRpc(isReady);
    }

    private void Disconnect()
    {
        ConnectionManager.Instance?.Disconnect();
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
        SetStatus("Desconectado do lobby.");
    }

    private void HandleJoinCodeUpdated(string code)
    {
        if (joinCodeText == null) return;
        if (string.IsNullOrWhiteSpace(code))
        {
            joinCodeText.text = "Codigo: --";
            return;
        }

        joinCodeText.text = $"Codigo: <b>{code}</b>";
    }

    private void RefreshPlayersView()
    {
        if (playersText == null) return;

        bool connected = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);
        if (hostButton != null) hostButton.gameObject.SetActive(!connected);
        if (joinButton != null) joinButton.gameObject.SetActive(!connected);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(!connected);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(connected);
        if (copyCodeButton != null)
        {
            bool host = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            copyCodeButton.gameObject.SetActive(host);
        }

        _playersBuilder.Clear();
        var lobby = LobbySessionManager.Instance;
        if (lobby == null)
        {
            _playersBuilder.Append("Aguardando LobbySessionManager...");
            playersText.text = _playersBuilder.ToString();
            return;
        }

        for (int i = 0; i < lobby.Players.Count; i++)
        {
            LobbyPlayerState player = lobby.Players[i];
            bool isLocal = NetworkManager.Singleton != null && player.ClientId == NetworkManager.Singleton.LocalClientId;
            _playersBuilder.Append("- ")
                .Append(player.DisplayName)
                .Append(" | ")
                .Append(player.CharacterType)
                .Append(" | ")
                .Append(player.IsReady ? "Pronto" : "Nao pronto");

            if (isLocal) _playersBuilder.Append(" (voce)");
            _playersBuilder.AppendLine();
        }

        playersText.text = _playersBuilder.ToString();

        if (startGameButton != null)
        {
            bool host = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            startGameButton.gameObject.SetActive(host);
            startGameButton.interactable = host && lobby.CanStartMatch;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
