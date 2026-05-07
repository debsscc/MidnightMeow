/// <summary>
/// ConnectionManager.cs
/// Orquestra o ciclo de vida completo de uma sessão multiplayer.
/// Coordena RelayManager + NetworkManager para hospedar ou entrar em partidas.
/// Expõe eventos para que a UI e outros sistemas reajam a mudanças de conexão
/// sem acoplamento direto.
///
/// MELHORIAS:
///   - Logging detalhado em cada etapa da conexão para diagnóstico
///   - Timeout de conexão configurável via MultiplayerConfig
///   - Monitoramento de falha de transporte (OnTransportFailure)
///   - Coroutine de progresso de conexão no cliente
///   - Proteção contra NPE em NetworkManager.Singleton durante callbacks
///
/// SRP: apenas gerencia o ciclo de conexão, não lógica de jogo ou UI.
/// </summary>

using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [SerializeField] private MultiplayerConfig config;

    // ── Eventos Públicos ───────────────────────────────────────────────────────
    public event Action<string>  OnJoinCodeObtained;
    public event Action          OnHostStarted;
    public event Action          OnClientConnected;
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
    private Coroutine _connectionTimeoutCoroutine;

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

    /// <summary>
    /// Inicia uma sessão como host. Cria alocação Relay, configura o transporte e
    /// inicia o NetworkManager como host.
    /// </summary>
    public async Task StartHostAsync()
    {
        if (!ValidatePrerequisites()) return;

        try
        {
            int maxConnections = config != null ? config.maxPlayers - 1 : 3;
            NotifyProgress($"Autenticando nos Unity Services...");
            Debug.Log($"[ConnectionManager] StartHostAsync: maxConnections={maxConnections}");

            string joinCode = await RelayManager.Instance.CreateRelayAndGetJoinCodeAsync(maxConnections);
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
    }

    // ── Cliente ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Entra em uma sessão existente usando o join code do host.
    /// Inicia um timeout configurável para detectar falha de conexão silenciosa.
    /// </summary>
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

    /// <summary>Desconecta da sessão atual. Funciona para host e cliente.</summary>
    public void Disconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[ConnectionManager] Disconnect chamado mas NetworkManager.Singleton é null.");
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
        string err = "Falha no transporte de rede. Verifique sua conexão e tente novamente.";
        Debug.LogError($"[ConnectionManager] OnTransportFailure disparado! {err}");

        StopConnectionTimeout();
        UnsubscribeNetworkManagerEvents();
        CurrentJoinCode = string.Empty;
        OnConnectionFailed?.Invoke(err);
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
