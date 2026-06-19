/// <summary>
/// Parâmetros data-driven do selamento de buracos de spawn de ratos.
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "RatHoleSealConfig", menuName = "MidnightMeow/Gameplay/Rat Hole Seal Config")]
public class RatHoleSealConfig : ScriptableObject
{
    [Header("Proximidade")]
    [Tooltip("Distância para exibir o prompt de selar.")]
    public float promptRadius = 2.4f;

    [Header("Áreas de selamento")]
    [Tooltip("Raio de cada área circular no chão.")]
    public float zoneRadius = 1.1f;

    [Tooltip("Distância mínima entre o centro do buraco e cada área.")]
    public float minDistanceFromHole = 1.6f;

    [Tooltip("Distância máxima entre o centro do buraco e cada área.")]
    public float maxDistanceFromHole = 3.2f;

    [Tooltip("Separação mínima entre duas áreas (evita sobreposição).")]
    public float minZoneSeparation = 1.8f;

    [Header("Progresso")]
    [Tooltip("Tempo em segundos com 1 jogador em 1 área para concluir o selamento.")]
    public float sealDuration = 6f;

    [Tooltip("Multiplicador de velocidade quando 2 jogadores ocupam as 2 áreas.")]
    [Range(1f, 4f)]
    public float dualPlayerSpeedMultiplier = 2f;

    [Tooltip("Segundos sem jogadores nas áreas até cancelar o selamento em andamento.")]
    public float abandonTimeout = 2.5f;

    [Header("Visual")]
    public Color zoneBackgroundColor = new Color(0.45f, 0.65f, 1f, 0.22f);
    public Color zoneFillColor = new Color(0.55f, 0.85f, 1f, 0.5f);
    public Color zoneOutlineColor = new Color(0.9f, 0.95f, 1f, 0.9f);
}
