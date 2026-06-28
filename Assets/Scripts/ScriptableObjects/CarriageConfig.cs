/// <summary>
/// Parâmetros data-driven da carruagem da Fase 2.
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "CarriageConfig", menuName = "MidnightMeow/Gameplay/Carriage Config")]
public class CarriageConfig : ScriptableObject
{
    [Header("Vitalidade")]
    public float maxHealth = 120f;

    [Header("Visual")]
    [Tooltip("Multiplicador opcional sobre o tamanho alvo do placeholder (~2,4×1,6 uu). Mantenha 1 até ter arte final.")]
    public float visualScale = 1f;

    [Header("Trajeto (Fase 2)")]
    [Tooltip("Posição X inicial da carruagem (waypoint de partida).")]
    public float pathStartX = -42f;

    [Tooltip("Posição X final da carruagem (waypoint de chegada).")]
    public float pathEndX = 18f;

    [Tooltip("Quando desligado, usa o Y do centro do mapa (CameraBounds).")]
    public bool useCustomPathY;

    public float pathY;

    [Header("Movimento")]
    [Tooltip("Unidades por segundo ao longo do trajeto normalizado (0–1).")]
    public float moveSpeed = 1.8f;

    [Header("Chegada")]
    [Tooltip("Raio da zona de chegada no fim do trajeto.")]
    public float arrivalZoneRadius = 2f;

    [Header("Conserto cooperativo")]
    public float repairZoneRadius = 1.2f;
    public float repairMinDistance = 1.4f;
    public float repairMaxDistance = 2.8f;
    public float repairMinZoneSeparation = 1.6f;
    public float repairDuration = 8f;
    [Range(1f, 4f)]
    public float repairDualPlayerSpeedMultiplier = 2f;
    public float repairAbandonTimeout = 2.5f;

    [Header("Visual das áreas de conserto")]
    public Color repairZoneBackgroundColor = new Color(0.95f, 0.7f, 0.25f, 0.22f);
    public Color repairZoneFillColor = new Color(1f, 0.85f, 0.35f, 0.5f);
    public Color repairZoneOutlineColor = new Color(1f, 0.95f, 0.75f, 0.9f);
}
