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
    [Tooltip("Multiplicador só do desenho (1 = mesmo tamanho da hitbox).")]
    public float zoneVisualScaleMultiplier = 1.05f;

    [Tooltip("Espessura do anel externo (fração do raio, ex. 0.05 = fino).")]
    [Range(0.02f, 0.15f)]
    public float zoneOutlineThickness = 0.055f;

    [Tooltip("Preencher o interior com cor de fundo (além do anel).")]
    public bool zoneShowInteriorFill;

    public Color zoneBackgroundColor = new Color(0.25f, 0.6f, 1f, 0.28f);
    public Color zoneFillColor = new Color(0.2f, 0.95f, 0.5f, 0.75f);
    public Color zoneOutlineColor = new Color(1f, 1f, 1f, 0.95f);
    [Tooltip("Sorting order do sprite (acima do chão, buracos e decoração).")]
    public int zoneSortingOrder = 250;

    public float GetZoneVisualDiameter() =>
        Mathf.Max(0.5f, zoneRadius * 2f * Mathf.Max(0.85f, zoneVisualScaleMultiplier));
}
