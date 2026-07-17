/// <summary>
/// Parâmetros data-driven da carruagem da Fase 2.
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "CarriageConfig", menuName = "MidnightMeow/Gameplay/Carriage Config")]
public class CarriageConfig : ScriptableObject
{
    [Header("Vitalidade")]
    public float maxHealth = 120f;

    [Tooltip("Fração da vida máxima restaurada após conserto (0–1).")]
    [Range(0.1f, 1f)]
    public float repairRestoreHealthFraction = 0.5f;

    [Header("Visual")]
    [Tooltip("Se true, usa a arte oficial do prefab (VisualRoot) e não força o placeholder marrom.")]
    public bool useOfficialArt = true;

    [Tooltip("Escala uniforme do VisualRoot quando useOfficialArt.")]
    [Min(0.05f)]
    public float visualRootScale = 0.3f;

    [Tooltip("Multiplicador legado do placeholder (~2,4×1,6 uu). Ignorado com arte oficial.")]
    public float visualScale = 1f;

    [Tooltip("Raio da roda dianteira em unidades de mundo (após escala do VisualRoot).")]
    [Min(0.05f)]
    public float frontWheelRadius = 0.28f;

    [Tooltip("Raio da roda traseira em unidades de mundo (após escala do VisualRoot).")]
    [Min(0.05f)]
    public float backWheelRadius = 0.36f;

    [Header("Collider (arte oficial)")]
    public Vector2 colliderSize = new Vector2(3.0f, 1.8f);
    public Vector2 colliderOffset = new Vector2(0.15f, -0.25f);

    [Header("UI offsets (arte oficial)")]
    [Tooltip("Offset local do label de conserto acima da carruagem.")]
    public Vector3 repairLabelOffset = new Vector3(0f, 1.85f, 0f);

    [Header("Trajeto (Fase 2)")]
    public float pathStartX = -42f;
    public float pathEndX = 18f;
    public bool useCustomPathY;
    public float pathY;

    [Header("Movimento")]
    [Tooltip("Unidades por segundo ao longo do trajeto.")]
    public float moveSpeed = 1.8f;

    [Header("Presença de jogadores (escolta)")]
    [Tooltip("Raio em que pelo menos um jogador vivo deve estar para a carruagem avançar.")]
    [Min(0.5f)]
    public float playerPresenceRadius = 8f;

    [Tooltip("Layers consideradas na detecção de presença. Se 0, usa a layer Player.")]
    public LayerMask playerPresenceLayerMask;

    [Header("Chegada")]
    public float arrivalZoneRadius = 2f;

    [Header("Conserto cooperativo")]
    public float repairPromptRadius = 3.3f;
    public float repairZoneRadius = 1.2f;
    public float repairMinDistance = 1.4f;
    public float repairMaxDistance = 2.8f;
    public float repairMinZoneSeparation = 1.6f;
    public float repairDuration = 8f;
    [Range(1f, 4f)]
    public float repairDualPlayerSpeedMultiplier = 2f;
    public float repairAbandonTimeout = 2.5f;

    [Header("Visual das áreas de conserto")]
    public float repairZoneVisualScaleMultiplier = 1.05f;
    [Range(0.02f, 0.15f)]
    public float repairZoneOutlineThickness = 0.055f;
    public bool repairZoneShowInteriorFill;
    public int repairZoneSortingOrder = 250;
    public Color repairZoneBackgroundColor = new Color(0.95f, 0.7f, 0.25f, 0.22f);
    public Color repairZoneFillColor = new Color(1f, 0.85f, 0.35f, 0.5f);
    public Color repairZoneOutlineColor = new Color(1f, 0.95f, 0.75f, 0.9f);

    [Header("UI")]
    [Tooltip("Prefab world-space do label acima da carruagem (reutiliza DownedRevivePromptUI).")]
    public GameObject repairPromptPrefab;

    [Tooltip("Tamanho da fonte do label world-space da carruagem.")]
    [Min(0.1f)]
    public float worldLabelFontSize = 1.4f;

    public float repairLabelVisibilityRadiusMultiplier = 2f;
    public string approachText = "Aproxime-se para consertar";
    public string pressEText = "Aperte E para consertar";
    public string stayInAreaText = "Fique na área para consertar";
    public string repairProgressTextFormat = "{0}%";

    [Header("UI — escolta (estados de rede)")]
    [Tooltip("Idle: nenhum jogador vivo no raio de presença.")]
    public string escortIdleText = "Se aproximem da Carruagem";

    [Tooltip("Moving: pelo menos um jogador vivo no raio.")]
    public string escortMovingText = "Protejam a Carruagem";

    [Tooltip("Broken: carruagem destruída aguardando conserto (label distante).")]
    public string escortBrokenText = "Consertem a Carruagem";

    public float GetRepairZoneVisualDiameter() =>
        Mathf.Max(0.5f, repairZoneRadius * 2f * Mathf.Max(0.85f, repairZoneVisualScaleMultiplier));

    public float GetRepairLabelVisibilityRadius() =>
        Mathf.Max(repairPromptRadius, repairPromptRadius * Mathf.Max(1f, repairLabelVisibilityRadiusMultiplier));

    public float GetPlayerPresenceRadius() => Mathf.Max(0.5f, playerPresenceRadius);

    public LayerMask ResolvePlayerPresenceLayerMask()
    {
        if (playerPresenceLayerMask.value != 0)
            return playerPresenceLayerMask;

        int playerLayer = LayerMask.NameToLayer("Player");
        return playerLayer >= 0 ? (LayerMask)(1 << playerLayer) : (LayerMask)~0;
    }

    public string GetApproachText() => string.IsNullOrWhiteSpace(approachText)
        ? "Aproxime-se para consertar"
        : approachText;

    public string GetPressEText() => string.IsNullOrWhiteSpace(pressEText)
        ? "Aperte E para consertar"
        : pressEText;

    public string GetStayInAreaText() => string.IsNullOrWhiteSpace(stayInAreaText)
        ? "Fique na área para consertar"
        : stayInAreaText;

    public string GetEscortIdleText() => string.IsNullOrWhiteSpace(escortIdleText)
        ? "Se aproximem da Carruagem"
        : escortIdleText;

    public string GetEscortMovingText() => string.IsNullOrWhiteSpace(escortMovingText)
        ? "Protejam a Carruagem"
        : escortMovingText;

    public string GetEscortBrokenText() => string.IsNullOrWhiteSpace(escortBrokenText)
        ? "Consertem a Carruagem"
        : escortBrokenText;

    public string FormatRepairProgressText(int percent) =>
        string.IsNullOrWhiteSpace(repairProgressTextFormat) ? $"{percent}%" : string.Format(repairProgressTextFormat, percent);

    private void OnValidate()
    {
        if (repairPromptRadius <= 0f) repairPromptRadius = repairZoneRadius * 3f;
        if (repairAbandonTimeout <= 0f) repairAbandonTimeout = 2.5f;
        if (repairLabelVisibilityRadiusMultiplier < 1f) repairLabelVisibilityRadiusMultiplier = 2f;
        if (playerPresenceRadius < 0.5f) playerPresenceRadius = 8f;
    }
}
