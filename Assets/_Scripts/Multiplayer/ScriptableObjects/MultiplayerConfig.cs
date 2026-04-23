/// <summary>
/// MultiplayerConfig.cs
/// ScriptableObject centralizado com todas as configurações ajustáveis do multiplayer.
/// Permite que game designers configurem parâmetros de sessão, sincronização, respawn
/// e comportamento de câmera sem alterar código. Segue o padrão Data-Driven.
/// Caminho de criação: Assets > Create > Scriptable Objects > Multiplayer > MultiplayerConfig
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "MultiplayerConfig", menuName = "Scriptable Objects/Multiplayer/MultiplayerConfig")]
public class MultiplayerConfig : ScriptableObject
{
    [Header("Sessão")]
    [Tooltip("Número máximo de jogadores simultâneos (1 host + N-1 clientes).")]
    [Range(2, 4)]
    public int maxPlayers = 4;

    [Tooltip("Região preferida do Relay. Use 'any' para menor latência automática.")]
    public string relayRegion = "any";

    [Tooltip("Timeout em segundos para tentativa de conexão.")]
    public float connectionTimeout = 30f;

    [Header("Jogador - Morte e Respawn")]
    [Tooltip("Se verdadeiro, jogadores mortos podem re-spawnar automaticamente.")]
    public bool allowRespawn = false;

    [Tooltip("Tempo em segundos até o respawn automático (requer allowRespawn = true).")]
    public float respawnDelay = 10f;

    [Tooltip("Tempo de invulnerabilidade em segundos após o respawn.")]
    public float respawnInvulnerabilityDuration = 2f;

    [Header("Spectator")]
    [Tooltip("Velocidade de interpolação da câmera do espectador ao trocar de alvo.")]
    public float spectatorCameraLerpSpeed = 5f;

    [Tooltip("Tempo em segundos entre a troca automática de alvo do espectador.")]
    public float spectatorAutoSwitchInterval = 8f;

    [Header("Sincronização")]
    [Tooltip("Intervalo mínimo em segundos entre atualizações de posição de rede (0 = a cada frame).")]
    [Range(0f, 0.1f)]
    public float positionSyncInterval = 0f;

    [Tooltip("Threshold de distância para enviar atualização de posição (economiza bandwidth).")]
    public float positionSyncThreshold = 0.01f;

    [Header("HUD Multiplayer")]
    [Tooltip("Cor do card de HUD do jogador local.")]
    public Color localPlayerCardColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [Tooltip("Cor do card de HUD dos jogadores remotos.")]
    public Color remotePlayerCardColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Tooltip("Cor do card de HUD de um jogador morto.")]
    public Color deadPlayerCardColor = new Color(0.4f, 0.1f, 0.1f, 0.6f);

    [Header("Ciência (Collectibles)")]
    [Tooltip("Se verdadeiro, toda ciência coletada é compartilhada entre todos os jogadores.")]
    public bool sharedSciencePool = true;

    [Header("Persistência de Conexão (UnityTransport)")]
    [Tooltip("Intervalo em ms entre heartbeats. Mantém a conexão ativa sem tráfego de jogo. Padrão: 500ms.")]
    public int heartbeatTimeoutMS = 500;

    [Tooltip("Intervalo em ms entre tentativas de reconexão. Padrão: 1000ms.")]
    public int connectTimeoutMS = 1000;

    [Tooltip("Ms sem resposta antes de declarar o cliente desconectado. Padrão: 30000ms (30s).")]
    public int disconnectTimeoutMS = 30000;

    [Tooltip("Número máximo de tentativas de conectar antes de desistir. Padrão: 60.")]
    public int maxConnectAttempts = 60;

    [Header("Relay")]
    [Tooltip("Se verdadeiro usa DTLS (criptografado). Se falso usa UDP puro (mais compatível para testes).")]
    public bool useSecureRelay = false;
}
