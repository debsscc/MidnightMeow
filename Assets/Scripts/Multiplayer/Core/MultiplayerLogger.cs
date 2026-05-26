/// <summary>
/// MultiplayerLogger.cs
/// Logger centralizado e configurável para todos os sistemas multiplayer.
/// Assina eventos dos gerenciadores multiplayer e de GameEvents, exibindo logs
/// categorizados e filtráveis no Console do Unity. Cada categoria pode ser
/// habilitada/desabilitada individualmente no Inspector sem alterar nenhum
/// outro script. Elimina a necessidade de Debug.Log espalhados pelo código.
/// SRP: exclusivamente responsável por logging e diagnóstico em tempo de execução.
/// </summary>

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerLogger : MonoBehaviour
{
    [Header("Habilitar / Desabilitar Categorias")]
    [SerializeField] private bool logConexao    = true;
    [SerializeField] private bool logEstadoJogo = true;
    [SerializeField] private bool logOndas      = true;
    [SerializeField] private bool logJogadores  = true;
    [SerializeField] private bool logRede       = true;

    [Header("Configuração")]
    [Tooltip("Prefixo exibido em todos os logs deste logger.")]
    [SerializeField] private string prefixo = "[MP]";
    [Tooltip("Se desabilitado, nenhum log é emitido independente das categorias.")]
    [SerializeField] private bool ativo = true;

    private bool _inscritoConnectionManager = false;

    private ConnectionManager _cm;

    private void Start()
    {
        // Eventos estáticos — disponíveis imediatamente
        MultiplayerGameManager.OnGameStateChanged += AoMudarEstadoJogo;
        MultiplayerGameManager.OnVictory           += AoVencer;
        MultiplayerGameManager.OnDefeat            += AoPerder;

        GameEvents.OnWaveStatusChanged  += AoMudarStatusOnda;
        GameEvents.OnPlayerJoined       += AoJogadorEntrar;
        GameEvents.OnPlayerLeft         += AoJogadorSair;
        GameEvents.OnAllPlayersDefeated += AoTodosDefeated;
        GameEvents.OnNightEnded         += AoNoiteTerminar;
        GameEvents.OnPlayerDefeated     += AoJogadorLocalDerrotado;

        // Eventos do NetworkManager — disponíveis após conexão
        StartCoroutine(InscricaoNetworkManagerRoutine());

        // Eventos do ConnectionManager — pode não estar pronto ainda
        StartCoroutine(InscricaoConnectionManagerRoutine());
    }

    private void OnDestroy()
    {
        MultiplayerGameManager.OnGameStateChanged -= AoMudarEstadoJogo;
        MultiplayerGameManager.OnVictory           -= AoVencer;
        MultiplayerGameManager.OnDefeat            -= AoPerder;

        GameEvents.OnWaveStatusChanged  -= AoMudarStatusOnda;
        GameEvents.OnPlayerJoined       -= AoJogadorEntrar;
        GameEvents.OnPlayerLeft         -= AoJogadorSair;
        GameEvents.OnAllPlayersDefeated -= AoTodosDefeated;
        GameEvents.OnNightEnded         -= AoNoiteTerminar;
        GameEvents.OnPlayerDefeated     -= AoJogadorLocalDerrotado;

        DesinscreverConnectionManager();
        DesinscreverNetworkManager();
    }

    // ── Coroutines de inscrição ────────────────────────────────────────────────

    private IEnumerator InscricaoConnectionManagerRoutine()
    {
        float timeout = 10f, elapsed = 0f;
        while (ConnectionManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.Instance == null)
        {
            LogInterno(LogType.Warning, logConexao, "ConnectionManager não encontrado — logs de conexão desabilitados.");
            yield break;
        }

        _cm = ConnectionManager.Instance;
        InscreveConnectionManager();
    }

    private IEnumerator InscricaoNetworkManagerRoutine()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        InscreveNetworkManager();
    }

    // ── Inscrição / desinscrição ───────────────────────────────────────────────

    private void InscreveConnectionManager()
    {
        if (_inscritoConnectionManager || _cm == null) return;
        _cm.OnJoinCodeObtained   += AoObterJoinCode;
        _cm.OnHostStarted        += AoHostIniciar;
        _cm.OnClientConnected    += AoClienteConectar;
        _cm.OnClientJoined       += AoClienteEntrar;
        _cm.OnClientLeft         += AoClienteSair;
        _cm.OnConnectionFailed   += AoFalhaConexao;
        _cm.OnDisconnected       += AoDesconectar;
        _cm.OnConnectionProgress += AoProgressoConexao;
        _inscritoConnectionManager = true;
        LogInterno(LogType.Log, logConexao, "Inscrito nos eventos do ConnectionManager.");
    }

    private void DesinscreverConnectionManager()
    {
        if (!_inscritoConnectionManager || _cm == null) return;
        _cm.OnJoinCodeObtained   -= AoObterJoinCode;
        _cm.OnHostStarted        -= AoHostIniciar;
        _cm.OnClientConnected    -= AoClienteConectar;
        _cm.OnClientJoined       -= AoClienteEntrar;
        _cm.OnClientLeft         -= AoClienteSair;
        _cm.OnConnectionFailed   -= AoFalhaConexao;
        _cm.OnDisconnected       -= AoDesconectar;
        _cm.OnConnectionProgress -= AoProgressoConexao;
        _inscritoConnectionManager = false;
    }

    private void InscreveNetworkManager()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback    += AoClienteConectadoNM;
        NetworkManager.Singleton.OnClientDisconnectCallback   += AoClienteDesconectadoNM;
        NetworkManager.Singleton.OnTransportFailure           += AoFalhaTransporte;
        LogInterno(LogType.Log, logRede, "Inscrito nos callbacks do NetworkManager.");
    }

    private void DesinscreverNetworkManager()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback    -= AoClienteConectadoNM;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= AoClienteDesconectadoNM;
        NetworkManager.Singleton.OnTransportFailure           -= AoFalhaTransporte;
    }

    // ── Handlers do ConnectionManager ─────────────────────────────────────────

    private void AoObterJoinCode(string code)    => Log(logConexao, $"Join Code obtido: <b>{code}</b>");
    private void AoHostIniciar()                 => Log(logConexao, "Host iniciado com sucesso.");
    private void AoClienteConectar()             => Log(logConexao, "Cliente local conectado ao host.");
    private void AoClienteEntrar(ulong id)       => Log(logConexao, $"Cliente entrou na sessão. ClientId={id}");
    private void AoClienteSair(ulong id)         => Log(logConexao, $"Cliente saiu da sessão. ClientId={id}");
    private void AoFalhaConexao(string msg)      => Log(logConexao, $"FALHA NA CONEXÃO: {msg}", LogType.Error);
    private void AoDesconectar()                 => Log(logConexao, "Desconectado da sessão.");
    private void AoProgressoConexao(string msg)  => Log(logConexao, $"[Progresso] {msg}");

    // ── Handlers do NetworkManager ─────────────────────────────────────────────

    private void AoClienteConectadoNM(ulong id)
    {
        bool local = NetworkManager.Singleton != null && id == NetworkManager.Singleton.LocalClientId;
        Log(logRede, $"[NM] Cliente conectado: ClientId={id}{(local ? " (local)" : "")}");
    }

    private void AoClienteDesconectadoNM(ulong id)
    {
        Log(logRede, $"[NM] Cliente desconectado: ClientId={id}");
    }

    private void AoFalhaTransporte()
    {
        Log(logRede, "[NM] Falha no transporte de rede!", LogType.Error);
    }

    // ── Handlers do MultiplayerGameManager ────────────────────────────────────

    private void AoMudarEstadoJogo(GameState novoEstado)
        => Log(logEstadoJogo, $"Estado do jogo: <b>{novoEstado}</b>");

    private void AoVencer()  => Log(logEstadoJogo, "VITÓRIA!");
    private void AoPerder()  => Log(logEstadoJogo, "DERROTA!");

    // ── Handlers do GameEvents ────────────────────────────────────────────────

    private void AoMudarStatusOnda(int onda, int total, int inimigosVivos, int mortos)
        => Log(logOndas, $"Onda {onda}/{total} | Inimigos vivos: {inimigosVivos} | Mortos: {mortos}");

    private void AoNoiteTerminar()
        => Log(logOndas, "Todas as ondas concluídas.");

    private void AoJogadorEntrar(ulong id, bool isLocal)
        => Log(logJogadores, $"Jogador entrou. ClientId={id}{(isLocal ? " (você)" : "")}");

    private void AoJogadorSair(ulong id)
        => Log(logJogadores, $"Jogador saiu. ClientId={id}");

    private void AoTodosDefeated()
        => Log(logJogadores, "Todos os jogadores foram derrotados.", LogType.Warning);

    private void AoJogadorLocalDerrotado()
        => Log(logJogadores, "Jogador local foi derrotado.", LogType.Warning);

    // ── Helpers de log ────────────────────────────────────────────────────────

    private void Log(bool categoria, string mensagem, LogType tipo = LogType.Log)
    {
        if (!ativo || !categoria) return;
        LogInterno(tipo, categoria, mensagem);
    }

    private void LogInterno(LogType tipo, bool categoria, string mensagem)
    {
        if (!ativo || !categoria) return;
        string msg = $"{prefixo} {mensagem}";
        switch (tipo)
        {
            case LogType.Error:   Debug.LogError(msg);   break;
            case LogType.Warning: Debug.LogWarning(msg); break;
            default:              Debug.Log(msg);        break;
        }
    }
}
