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

    [Header("Zona de reviver")]
    [Tooltip("Raio da área ao redor do jogador caído. Aliados devem permanecer dentro para reanimar.")]
    public float reviveZoneRadius = 2.2f;

    [Tooltip("Tempo em segundos dentro da zona para concluir o reviver.")]
    public float reviveZoneFillDuration = 3f;

    [Tooltip("Queda do progresso por segundo quando ninguém está na zona.")]
    public float reviveZoneProgressDecayPerSecond = 0.75f;

    [Header("Visual da zona")]
    public Color reviveZoneBackgroundColor = new Color(0.2f, 0.85f, 0.45f, 0.2f);
    public Color reviveZoneFillColor = new Color(0.35f, 1f, 0.55f, 0.45f);
    public Color reviveZoneOutlineColor = new Color(0.9f, 1f, 0.95f, 0.9f);

    [Header("Legado (obsoleto)")]
    [Tooltip("Use reviveZoneRadius. Mantido para assets antigos.")]
    public float reviveRange = 2.2f;

    [Tooltip("Use reviveZoneFillDuration.")]
    public float reviveHoldDuration = 3f;

    [Tooltip("Use reviveZoneProgressDecayPerSecond.")]
    public float reviveProgressDecayPerSecond = 0.75f;

    [Tooltip("Intervalo mínimo entre envios de progresso de reviver à rede (segundos).")]
    public float reviveProgressSendInterval = 0.1f;

    private void OnValidate()
    {
        if (reviveZoneRadius <= 0f) reviveZoneRadius = reviveRange > 0f ? reviveRange : 2.2f;
        if (reviveZoneFillDuration <= 0f) reviveZoneFillDuration = reviveHoldDuration > 0f ? reviveHoldDuration : 3f;
        if (reviveZoneProgressDecayPerSecond <= 0f)
            reviveZoneProgressDecayPerSecond = reviveProgressDecayPerSecond > 0f ? reviveProgressDecayPerSecond : 0.75f;
    }
}
