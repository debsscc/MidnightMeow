/// <summary>
/// RelayManager.cs
/// Responsável exclusivamente pela comunicação com o serviço Unity Relay.
/// Gerencia autenticação anônima, criação de alocações (host), join codes e entrada
/// em alocações existentes (cliente). Usa SetHostRelayData / SetClientRelayData do
/// UnityTransport — API estável do Unity Transport 2.x sem AllocationUtils.
/// Aplica configurações de keep-alive do UnityTransport para manter conexão persistente.
/// SRP: apenas lida com o Relay e transporte, sem conhecer UI ou lógica de jogo.
/// </summary>

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [SerializeField] private MultiplayerConfig config;

    private bool _isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Inicialização ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa os Unity Services e realiza autenticação anônima.
    /// Idempotente: chamadas subsequentes retornam imediatamente.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            Debug.Log("[RelayManager] Inicializando Unity Services...");
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[RelayManager] Realizando autenticação anônima...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayManager] Autenticado. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"[RelayManager] Já autenticado. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }

            _isInitialized = true;
            Debug.Log("[RelayManager] Unity Services inicializados com sucesso.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Falha na inicialização dos Unity Services: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    // ── Host ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria uma alocação Relay para o host, configura o UnityTransport e retorna o join code.
    /// Aplica configurações de keep-alive e protocolo definidas em MultiplayerConfig.
    /// </summary>
    /// <param name="maxConnections">Número máximo de CLIENTES (maxPlayers - 1).</param>
    public async Task<string> CreateRelayAndGetJoinCodeAsync(int maxConnections)
    {
        await InitializeAsync();

        try
        {
            Debug.Log($"[RelayManager] Criando alocação Relay para {maxConnections} conexões...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            Debug.Log($"[RelayManager] Alocação criada. AllocationId: {allocation.AllocationId}");

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[RelayManager] Join Code obtido: {joinCode}");

            bool secure = config != null ? config.useSecureRelay : false;
            ConfigureTransportAsHost(allocation, secure);
            ApplyKeepAliveSettings();

            Debug.Log($"[RelayManager] Host configurado. Relay={allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}, Secure={secure}");
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Falha ao criar alocação Relay: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    // ── Cliente ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Entra em uma alocação Relay existente usando o join code do host.
    /// Configura o UnityTransport como cliente com o mesmo protocolo do host.
    /// </summary>
    /// <param name="joinCode">Código de 6 caracteres fornecido pelo host.</param>
    public async Task JoinRelayAsync(string joinCode)
    {
        await InitializeAsync();

        string normalizedCode = joinCode.Trim().ToUpper();
        Debug.Log($"[RelayManager] Entrando na alocação Relay com código: {normalizedCode}...");

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(normalizedCode);
            Debug.Log($"[RelayManager] JoinAllocation obtido. AllocationId: {joinAllocation.AllocationId}" +
                      $" | Relay: {joinAllocation.RelayServer.IpV4}:{joinAllocation.RelayServer.Port}");

            bool secure = config != null ? config.useSecureRelay : false;
            ConfigureTransportAsClient(joinAllocation, secure);
            ApplyKeepAliveSettings();

            Debug.Log($"[RelayManager] Cliente configurado. Relay={joinAllocation.RelayServer.IpV4}:{joinAllocation.RelayServer.Port}, Secure={secure}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Falha ao entrar na alocação Relay: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    // ── Configuração do UnityTransport ─────────────────────────────────────────

    private void ConfigureTransportAsHost(Allocation allocation, bool isSecure)
    {
        UnityTransport transport = GetTransport();
        if (transport == null) return;

        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            isSecure: isSecure
        );
    }

    private void ConfigureTransportAsClient(JoinAllocation joinAllocation, bool isSecure)
    {
        UnityTransport transport = GetTransport();
        if (transport == null) return;

        transport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            isSecure: isSecure
        );
    }

    /// <summary>
    /// Aplica configurações de keep-alive ao UnityTransport.
    /// Heartbeat frequente + disconnect timeout alto = conexão persistente mesmo sem tráfego de jogo.
    /// </summary>
    private void ApplyKeepAliveSettings()
    {
        UnityTransport transport = GetTransport();
        if (transport == null) return;

        int heartbeat    = config != null ? config.heartbeatTimeoutMS  : 500;
        int connectTO    = config != null ? config.connectTimeoutMS     : 1000;
        int disconnectTO = config != null ? config.disconnectTimeoutMS  : 30000;
        int maxAttempts  = config != null ? config.maxConnectAttempts   : 60;

        transport.HeartbeatTimeoutMS  = heartbeat;
        transport.ConnectTimeoutMS    = connectTO;
        transport.DisconnectTimeoutMS = disconnectTO;
        transport.MaxConnectAttempts  = maxAttempts;

        Debug.Log($"[RelayManager] Keep-alive aplicado: Heartbeat={heartbeat}ms, " +
                  $"ConnectTO={connectTO}ms, DisconnectTO={disconnectTO}ms, MaxAttempts={maxAttempts}");
    }

    private UnityTransport GetTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayManager] NetworkManager.Singleton não encontrado ao configurar transporte!");
            return null;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
            Debug.LogError("[RelayManager] UnityTransport não encontrado no NetworkManager!");

        return transport;
    }

    // ── Propriedades ───────────────────────────────────────────────────────────

    public bool IsInitialized => _isInitialized;
}
