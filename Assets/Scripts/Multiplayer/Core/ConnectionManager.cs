/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Ciclo de vida da sessão multiplayer (Relay + NetworkManager, host e cliente).
---------------------------------------------------------------- */

using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [SerializeField] private MultiplayerConfig config;

    // ── Eventos Públicos ───────────────────────────────────────────────────────
    public event Action<string>  OnJoinCodeObtained;
    public event Action          OnHostStarted;
    public event Action          OnClientConnected;
    public static event Action   OnHostLeftSession;
    public event Action<ulong>   OnClientJoined;
    public event Action<ulong>   OnClientLeft;
    public event Action<string>  OnConnectionFailed;
    public event Action          OnDisconnected;
    public event Action<string>  OnConnectionProgress; // Progresso de conexão (para UI)

    // ── Propriedades Públicas ──────────────────────────────────────────────────
    public string CurrentJoinCode { get; private set; }
    public bool IsConnectedAsHost   => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    public bool IsConnectedAsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;
    public bool IsConnecting        => _connectionTimeoutCoroutine != null;

    // ── Estado Interno ─────────────────────────────────────────────────────────
    private bool _networkEventsSubscribed = false;
    private bool _lobbyRecoveryScheduled;
    private bool _hostStartInProgress;
    private bool _hostStartCancelled;
    private float _intentionalShutdownUntil;
    private Coroutine _connectionTimeoutCoroutine;

    private const float IntentionalShutdownGraceSeconds = 3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[ConnectionManager] Singleton inicializado.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        StopConnectionTimeout();
        UnsubscribeNetworkManagerEvents();
    }

    // ── Host ───────────────────────────────────────────────────────────────────

    public bool TryStartLocalSoloHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ConnectionManager] TryStartLocalSoloHost: NetworkManager ausente.");
            return false;
        }

        if (NetworkManager.Singleton.IsServer)
            return true;

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[ConnectionManager] TryStartLocalSoloHost: encerrando sessão anterior...");
            NetworkManager.Singleton.Shutdown();
            UnsubscribeNetworkManagerEvents();
        }

        SubscribeNetworkManagerEvents();
        bool started = NetworkManager.Singleton.StartHost();
        if (!started)
        {
            UnsubscribeNetworkManagerEvents();
            Debug.LogError("[ConnectionManager] TryStartLocalSoloHost: StartHost() falhou.");
            return false;
        }

        Debug.Log("[ConnectionManager] Host local (solo) iniciado.");
        SaveProfileStore.Instance?.MarkActiveAsHostSave("SoloHost");
        OnHostStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Cancela um StartHostAsync em andamento (ex.: usuário trocou pra solo enquanto o Relay era
    /// alocado). Se a alocação ainda não virou host, o host não é iniciado — evita o ciclo
    /// start->shutdown que gera os erros "allocation invalid"/"allocation ID not found".
    /// </summary>
    public void CancelPendingHostStart()
    {
        if (_hostStartInProgress)
            _hostStartCancelled = true;
    }

    public async Task StartHostAsync()
    {
        if (_hostStartInProgress)
            return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (!string.IsNullOrEmpty(CurrentJoinCode))
                OnJoinCodeObtained?.Invoke(CurrentJoinCode);
            OnHostStarted?.Invoke();
            return;
        }

        if (!ValidatePrerequisites()) return;

        _hostStartInProgress = true;
        _hostStartCancelled = false;
        try
        {
            int maxConnections = config != null ? config.maxPlayers - 1 : 3;
            NotifyProgress($"Autenticando nos Unity Services...");
            Debug.Log($"[ConnectionManager] StartHostAsync: maxConnections={maxConnections}");

            string joinCode = await RelayManager.Instance.CreateRelayAndGetJoinCodeAsync(maxConnections);

            if (_hostStartCancelled)
            {
                // Usuário abandonou o modo host durante a alocação do Relay. Não inicia o host
                // (a alocação não usada expira sozinha no serviço) — evita transport failure.
                Debug.Log("[ConnectionManager] StartHostAsync cancelado durante alocação do Relay — host não será iniciado.");
                CurrentJoinCode = string.Empty;
                return;
            }

            CurrentJoinCode = joinCode;

            NotifyProgress("Iniciando NetworkManager como host...");
            SubscribeNetworkManagerEvents();

            bool started = NetworkManager.Singleton.StartHost();
            if (started)
            {
                Debug.Log($"[ConnectionManager] Host iniciado. Join Code: {joinCode}");
                OnJoinCodeObtained?.Invoke(joinCode);
                OnHostStarted?.Invoke();
            }
            else
            {
                UnsubscribeNetworkManagerEvents();
                string err = "NetworkManager.StartHost() retornou false. Verifique o Transport.";
                Debug.LogError($"[ConnectionManager] {err}");
                OnConnectionFailed?.Invoke(err);
            }
        }
        catch (Exception e)
        {
            UnsubscribeNetworkManagerEvents();
            string err = $"Erro ao hospedar: {e.Message}";
            Debug.LogError($"[ConnectionManager] StartHostAsync falhou: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            OnConnectionFailed?.Invoke(err);
        }
        finally
        {
            _hostStartInProgress = false;
            _hostStartCancelled = false;
        }
    }

    // ── Cliente ────────────────────────────────────────────────────────────────

    public async Task StartClientAsync(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            OnConnectionFailed?.Invoke("Join code não pode estar vazio.");
            return;
        }

        if (!ValidatePrerequisites()) return;

        try
        {
            string normalizedCode = joinCode.Trim().ToUpper();
            NotifyProgress($"Autenticando e obtendo alocação Relay para '{normalizedCode}'...");
            Debug.Log($"[ConnectionManager] StartClientAsync: código='{normalizedCode}'");

            await RelayManager.Instance.JoinRelayAsync(normalizedCode);

            NotifyProgress("Relay configurado. Conectando ao host...");
            Debug.Log("[ConnectionManager] JoinRelayAsync concluído. Iniciando NetworkManager cliente...");

            SubscribeNetworkManagerEvents();

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                UnsubscribeNetworkManagerEvents();
                string err = "NetworkManager.StartClient() retornou false. Verifique o Transport.";
                Debug.LogError($"[ConnectionManager] {err}");
                OnConnectionFailed?.Invoke(err);
                return;
            }

            Debug.Log("[ConnectionManager] StartClient() retornou true. Aguardando callback de conexão...");
            NotifyProgress("Conectando... aguardando resposta do host.");

            // Inicia monitoramento de timeout
            float timeout = config != null ? config.connectionTimeout : 30f;
            _connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutRoutine(timeout));
        }
        catch (Exception e)
        {
            StopConnectionTimeout();
            UnsubscribeNetworkManagerEvents();
            string err = $"Erro ao entrar na partida: {e.Message}";
            Debug.LogError($"[ConnectionManager] StartClientAsync falhou: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            OnConnectionFailed?.Invoke(err);
        }
    }

    // ── Desconexão ─────────────────────────────────────────────────────────────

    public void Disconnect()
    {
        PerformDisconnect();
    }

    public void DisconnectAsHost()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            Disconnect();
            return;
        }

        MarkIntentionalShutdown();

        if (LobbySessionManager.Instance != null && LobbySessionManager.Instance.IsSpawned)
            LobbySessionManager.Instance.NotifyHostLeavingClients();

        StartCoroutine(DisconnectAsHostRoutine());
    }

    /// <summary>
    /// Marca uma janela curta em que falhas de transporte são tratadas como esperadas (desligamento
    /// que nós provocamos), evitando recovery/erro falso ao jogador.
    /// </summary>
    private void MarkIntentionalShutdown()
    {
        _intentionalShutdownUntil = Time.realtimeSinceStartup + IntentionalShutdownGraceSeconds;
    }

    private IEnumerator DisconnectAsHostRoutine()
    {
        yield return null;
        PerformDisconnect();
    }

    internal static void RaiseHostLeftSession()
    {
        OnHostLeftSession?.Invoke();
    }

    private void PerformDisconnect()
    {
        MarkIntentionalShutdown();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[ConnectionManager] Disconnect chamado mas NetworkManager.Singleton é null.");
            CurrentJoinCode = string.Empty;
            UnsubscribeNetworkManagerEvents();
            OnDisconnected?.Invoke();
            return;
        }

        Debug.Log("[ConnectionManager] Desconectando...");
        StopConnectionTimeout();
        NetworkManager.Singleton.Shutdown();
        CurrentJoinCode = string.Empty;
        UnsubscribeNetworkManagerEvents();
        OnDisconnected?.Invoke();
    }

    // ── Timeout de Conexão ─────────────────────────────────────────────────────

    private IEnumerator ConnectionTimeoutRoutine(float timeout)
    {
        float elapsed = 0f;
        float progressInterval = 5f;
        float nextProgressLog = progressInterval;

        Debug.Log($"[ConnectionManager] Timeout de conexão iniciado: {timeout}s");

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextProgressLog)
            {
                NotifyProgress($"Conectando... ({elapsed:F0}s / {timeout:F0}s)");
                Debug.Log($"[ConnectionManager] Aguardando conexão: {elapsed:F0}s de {timeout:F0}s");
                nextProgressLog += progressInterval;
            }

            yield return null;
        }

        // Timeout atingido sem receber OnClientConnected
        _connectionTimeoutCoroutine = null;
        string errMsg = $"Timeout de conexão após {timeout:F0}s. Verifique o código de acesso e a conexão com a internet.";
        Debug.LogError($"[ConnectionManager] {errMsg}");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            UnsubscribeNetworkManagerEvents();
        }

        OnConnectionFailed?.Invoke(errMsg);
    }

    private void StopConnectionTimeout()
    {
        if (_connectionTimeoutCoroutine != null)
        {
            StopCoroutine(_connectionTimeoutCoroutine);
            _connectionTimeoutCoroutine = null;
        }
    }

    // ── Eventos do NetworkManager ──────────────────────────────────────────────

    private void SubscribeNetworkManagerEvents()
    {
        if (_networkEventsSubscribed || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback    += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   += HandleClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure           += HandleTransportFailure;
        _networkEventsSubscribed = true;

        Debug.Log("[ConnectionManager] Inscrito nos callbacks do NetworkManager (OnClientConnected, OnClientDisconnect, OnTransportFailure).");
    }

    private void UnsubscribeNetworkManagerEvents()
    {
        if (!_networkEventsSubscribed || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback    -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= HandleClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure           -= HandleTransportFailure;
        _networkEventsSubscribed = false;

        Debug.Log("[ConnectionManager] Removido dos callbacks do NetworkManager.");
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        bool isLocalClient = clientId == NetworkManager.Singleton.LocalClientId;
        bool isHost = NetworkManager.Singleton.IsHost;

        Debug.Log($"[ConnectionManager] Cliente conectado: ClientId={clientId} | Local={isLocalClient} | IsHost={isHost}");

        if (isLocalClient && !isHost)
        {
            // Conexão do cliente local confirmada — cancela timeout
            StopConnectionTimeout();
            NotifyProgress("Conectado com sucesso!");
            OnClientConnected?.Invoke();
        }

        OnClientJoined?.Invoke(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[ConnectionManager] Cliente desconectado: ClientId={clientId} | Local={clientId == localId}");

        OnClientLeft?.Invoke(clientId);

        if (clientId == localId)
        {
            StopConnectionTimeout();
            CurrentJoinCode = string.Empty;
            UnsubscribeNetworkManagerEvents();
            OnDisconnected?.Invoke();
        }
    }

    private void HandleTransportFailure()
    {
        StopConnectionTimeout();
        CurrentJoinCode = string.Empty;
        UnsubscribeNetworkManagerEvents();

        if (Time.realtimeSinceStartup <= _intentionalShutdownUntil)
        {
            // Falha de transporte logo após um desligamento que nós provocamos (ex.: troca rápida
            // host->solo). Esperado: não dispara recovery nem mensagem de erro pro jogador.
            Debug.Log("[ConnectionManager] Transport failure após shutdown intencional — ignorado.");
            return;
        }

        string err = UiLocalization.Get("lobby.status.connection_lost", "Conexão perdida. Tente novamente.");
        Debug.LogError("[ConnectionManager] OnTransportFailure disparado! Relay caiu — recuperando para o lobby.");

        if (!_lobbyRecoveryScheduled)
            BeginLobbyRecoveryAfterNetworkFailure(err);
    }

    public void BeginLobbyRecoveryAfterNetworkFailure(string userMessage)
    {
        if (_lobbyRecoveryScheduled)
            return;

        StartCoroutine(RecoverToLobbyAfterNetworkFailureRoutine(userMessage));
    }

    private IEnumerator RecoverToLobbyAfterNetworkFailureRoutine(string userMessage)
    {
        _lobbyRecoveryScheduled = true;

        yield return null;

        NetworkManager net = NetworkManager.Singleton;
        if (net != null && net.IsListening)
        {
            net.Shutdown();
            yield return null;
        }

        UnsubscribeNetworkManagerEvents();
        OnConnectionFailed?.Invoke(userMessage);

        if (ShouldReturnToLobbyAfterNetworkFailure())
        {
            // Vamos sair da cena atual para o lobby: guarda o aviso amigável para o lobby exibir ao abrir.
            GameSessionContext.PendingConnectionMessage = userMessage;
            GameSessionContext.PendingRouteId = string.Empty;
            ScreenFlowController.Instance?.ForceClearTransitionOverlay();

            float timeout = 5f;
            while (timeout > 0f && !TryRequestReturnToLobby())
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        _lobbyRecoveryScheduled = false;
    }

    private static bool ShouldReturnToLobbyAfterNetworkFailure()
    {
        string scene = SceneManager.GetActiveScene().name;
        return scene is not ("Lobby" or "Menu2" or "BootstrapScene");
    }

    private static bool TryRequestReturnToLobby()
    {
        GameFlowOrchestrator orchestrator = GameFlowOrchestrator.Instance;
        if (orchestrator != null && orchestrator.TryRequestRoute(SceneFlowRouteIds.ReturnToLobby))
            return true;

        ScreenFlowController flow = ScreenFlowController.Instance;
        return flow != null && flow.RequestRoute(SceneFlowRouteIds.ReturnToLobby);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private bool ValidatePrerequisites()
    {
        if (RelayManager.Instance == null)
        {
            string err = "RelayManager não encontrado na cena.";
            Debug.LogError($"[ConnectionManager] {err}");
            OnConnectionFailed?.Invoke(err);
            return false;
        }

        if (NetworkManager.Singleton == null)
        {
            string err = "NetworkManager não encontrado. Verifique a configuração da cena.";
            Debug.LogError($"[ConnectionManager] {err}");
            OnConnectionFailed?.Invoke(err);
            return false;
        }

        return true;
    }

    private void NotifyProgress(string message)
    {
        OnConnectionProgress?.Invoke(message);
        Debug.Log($"[ConnectionManager] Progresso: {message}");
    }
}
