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
    [Tooltip("Distância para exibir o prompt de reviver (E).")]
    public float revivePromptRadius = 3.3f;

    [Tooltip("Raio de cada área circular no chão.")]
    public float reviveZoneRadius = 1.1f;

    [Tooltip("Distância mínima entre o jogador caído e cada área.")]
    public float reviveZonePlacementMinDistance = 1.6f;

    [Tooltip("Distância máxima entre o jogador caído e cada área.")]
    public float reviveZonePlacementMaxDistance = 3.2f;

    [Tooltip("Separação mínima entre duas áreas.")]
    public float reviveZoneMinSeparation = 1.8f;

    [Tooltip("Tempo em segundos com 1 jogador em 1 área para concluir o reviver.")]
    public float reviveZoneFillDuration = 6f;

    [Tooltip("Multiplicador quando 2 jogadores ocupam as 2 áreas.")]
    [Range(1f, 4f)]
    public float reviveDualPlayerSpeedMultiplier = 2f;

    [Tooltip("Segundos sem jogadores nas áreas até cancelar o reviver em andamento.")]
    public float reviveAbandonTimeout = 2.5f;

    [Header("Visual da zona")]
    [Tooltip("Multiplicador só do desenho (1 = mesmo tamanho da hitbox).")]
    public float reviveZoneVisualScaleMultiplier = 1.05f;

    [Range(0.02f, 0.15f)]
    public float reviveZoneOutlineThickness = 0.055f;

    public bool reviveZoneShowInteriorFill;

    public int reviveZoneSortingOrder = 250;
    public Color reviveZoneBackgroundColor = new Color(0.2f, 0.85f, 0.45f, 0.2f);
    public Color reviveZoneFillColor = new Color(0.35f, 1f, 0.55f, 0.45f);
    public Color reviveZoneOutlineColor = new Color(0.9f, 1f, 0.95f, 0.9f);

    [Header("UI")]
    [Tooltip("Prefab world-space do prompt \"Aperte E para reviver\" (mesmo estilo do selamento).")]
    public GameObject revivePromptPrefab;

    [Header("Legado (obsoleto)")]
    [Tooltip("Use reviveZoneRadius. Mantido para assets antigos.")]
    public float reviveRange = 2.2f;

    [Tooltip("Use reviveZoneFillDuration.")]
    public float reviveHoldDuration = 6f;

    [Tooltip("Obsoleto — use reviveAbandonTimeout.")]
    public float reviveZoneProgressDecayPerSecond = 0.75f;

    public float GetReviveZoneVisualDiameter() =>
        Mathf.Max(0.5f, reviveZoneRadius * 2f * Mathf.Max(0.85f, reviveZoneVisualScaleMultiplier));

    private void OnValidate()
    {
        if (reviveZoneRadius <= 0f) reviveZoneRadius = reviveRange > 0f ? reviveRange : 1.1f;
        if (reviveZoneFillDuration <= 0f) reviveZoneFillDuration = reviveHoldDuration > 0f ? reviveHoldDuration : 6f;
        if (revivePromptRadius <= 0f) revivePromptRadius = reviveZoneRadius * 3f;
        if (reviveAbandonTimeout <= 0f) reviveAbandonTimeout = 2.5f;
    }
}
