/// <summary>
/// Configuração data-driven para jogadores inconscientes e reviver no multiplayer.
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "DownedPlayerConfig", menuName = "MidnightMeow/Multiplayer/Downed Player Config")]
public class DownedPlayerConfig : ScriptableObject
{
    [Header("Inconsciente")]
    [Tooltip("Tempo em segundos até sangrar (não pode mais ser revivido).")]
    public float unconsciousDuration = 45f;

    [Tooltip("Fração da vida máxima restaurada ao ser revivido (0–1).")]
    [Range(0.1f, 1f)]
    public float reviveHealthFraction = 0.5f;

    [Header("Reviver")]
    [Tooltip("Distância máxima para iniciar reviver outro jogador.")]
    public float reviveRange = 2.5f;

    [Tooltip("Tempo em segundos segurando Interact para concluir o reviver.")]
    public float reviveHoldDuration = 3f;

    [Tooltip("Velocidade de queda do progresso de reviver por segundo ao soltar Interact.")]
    public float reviveProgressDecayPerSecond = 0.75f;

    [Tooltip("Intervalo mínimo entre envios de progresso de reviver à rede (segundos).")]
    public float reviveProgressSendInterval = 0.1f;
}
